using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.HostedGit;
using Microsoft.Extensions.Options;

namespace GodForge.IntegrationTests.HostedGit;

public sealed class RealForgejoFactAttribute : FactAttribute
{
    public RealForgejoFactAttribute()
    {
        if (new[] { "GODFORGE_TEST_FORGEJO_URL", "GODFORGE_TEST_FORGEJO_SERVICE_TOKEN", "GODFORGE_TEST_FORGEJO_DEVELOPER_TOKEN", "GODFORGE_TEST_FORGEJO_VIEWER_TOKEN" }
            .Any(name => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))))
            Skip = "Requires isolated real Forgejo and disposable non-admin service/developer/viewer accounts. This is not M2 acceptance evidence.";
    }
}

// Adapter integration only: this does not substitute for API/outbox/Worker membership acceptance.
public sealed class RealForgejoTests
{
    [RealForgejoFact]
    public async Task AdapterCreatesClonesPushesBrowsesAndRevokesRealGitAccess()
    {
        var endpoint = Environment.GetEnvironmentVariable("GODFORGE_TEST_FORGEJO_URL")!;
        var serviceToken = Environment.GetEnvironmentVariable("GODFORGE_TEST_FORGEJO_SERVICE_TOKEN")!;
        var developerToken = Environment.GetEnvironmentVariable("GODFORGE_TEST_FORGEJO_DEVELOPER_TOKEN")!;
        var viewerToken = Environment.GetEnvironmentVariable("GODFORGE_TEST_FORGEJO_VIEWER_TOKEN")!;
        using var http = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false });
        var provider = new ForgejoHostedGitService(http, Options.Create(new ForgejoSettings
        { Enabled = true, BaseUrl = endpoint, PublicUrl = endpoint, ApiToken = serviceToken, AllowLocalHttp = true }));
        var developer = await provider.VerifyIdentityAsync(developerToken, default);
        var viewer = await provider.VerifyIdentityAsync(viewerToken, default);
        Assert.NotEqual(developer.Id, viewer.Id);
        var identity = Guid.NewGuid().ToString("N");
        var input = new HostedRepositoryProvisionRequest("", "godforge-test-" + identity, true, "main", identity);
        var repository = await provider.ProvisionAsync(input);
        var workspace = Directory.CreateTempSubdirectory("godforge-provider-test-").FullName;
        try
        {
            Assert.Equal(repository.ProviderRepositoryId, (await provider.ProvisionAsync(input)).ProviderRepositoryId);
            await provider.SetCollaboratorAsync(repository.ProviderRepositoryId, developer.Id, developer.Login, "write", default);
            await provider.SetCollaboratorAsync(repository.ProviderRepositoryId, viewer.Id, viewer.Login, "read", default);
            var developerPath = Path.Combine(workspace, "developer");
            Assert.Equal(0, await Git(workspace, developer.Login, developerToken, "clone", repository.CloneUrl, developerPath));
            await File.WriteAllTextAsync(Path.Combine(developerPath, "project.godot"), "config_version=5\n[application]\nconfig/name=\"M2 fixture\"\n");
            await File.WriteAllTextAsync(Path.Combine(developerPath, "player.gd"), "extends Node\nvar health = 100\n");
            Assert.Equal(0, await Git(developerPath, developer.Login, developerToken, "add", "project.godot", "player.gd"));
            Assert.Equal(0, await Git(developerPath, developer.Login, developerToken, "-c", "user.name=M2 Test", "-c", "user.email=m2@example.test", "commit", "-m", "Godot fixture"));
            Assert.Equal(0, await Git(developerPath, developer.Login, developerToken, "push", "origin", "HEAD:main"));
            IReadOnlyList<GodForge.Application.Common.Models.RepositoryBranchDto> branches = [];
            using (var ready = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                do
                {
                    branches = await provider.BranchesAsync(repository.ProviderRepositoryId, 1, 20, ready.Token);
                    if (branches.Any(b => b.Name == "main")) break;
                    await Task.Delay(200, ready.Token);
                } while (!ready.IsCancellationRequested);
            }
            var main = Assert.Single(branches, b => b.Name == "main");
            var commits = await provider.CommitsAsync(repository.ProviderRepositoryId, "main", 1, 20, default);
            Assert.Equal(main.CommitSha, commits[0].Sha);
            var tree = await provider.TreeAsync(repository.ProviderRepositoryId, main.CommitSha, "", default);
            Assert.Contains(tree.Entries, e => e.Name == "project.godot");
            Assert.Contains("config_version=5", (await provider.BlobAsync(repository.ProviderRepositoryId, main.CommitSha, "project.godot", default)).Content);
            Assert.Contains("extends Node", (await provider.BlobAsync(repository.ProviderRepositoryId, main.CommitSha, "player.gd", default)).Content);
            var viewerPath = Path.Combine(workspace, "viewer");
            Assert.Equal(0, await Git(workspace, viewer.Login, viewerToken, "clone", repository.CloneUrl, viewerPath));
            await File.WriteAllTextAsync(Path.Combine(viewerPath, "forbidden.txt"), "viewer push must fail");
            Assert.Equal(0, await Git(viewerPath, viewer.Login, viewerToken, "add", "forbidden.txt"));
            Assert.Equal(0, await Git(viewerPath, viewer.Login, viewerToken, "-c", "user.name=M2 Viewer", "-c", "user.email=viewer@example.test", "commit", "-m", "Viewer change"));
            Assert.NotEqual(0, await Git(viewerPath, viewer.Login, viewerToken, "push", "origin", "HEAD:main"));
            await provider.SetCollaboratorAsync(repository.ProviderRepositoryId, developer.Id, developer.Login, null, default);
            Assert.DoesNotContain(await provider.CollaboratorsAsync(repository.ProviderRepositoryId, default), c => c.Id == developer.Id);
            Assert.NotEqual(0, await Git(workspace, developer.Login, developerToken, "ls-remote", repository.CloneUrl));
        }
        finally
        {
            using var cleanup = new HttpRequestMessage(HttpMethod.Delete, new Uri(new Uri(endpoint.TrimEnd('/') + "/"), "api/v1/repos/" + new Uri(repository.WebUrl).AbsolutePath.Trim('/')));
            cleanup.Headers.Authorization = new AuthenticationHeaderValue("token", serviceToken);
            using var response = await http.SendAsync(cleanup);
            Assert.True(response.IsSuccessStatusCode, "Disposable provider repository cleanup failed.");
            var full = Path.GetFullPath(workspace);
            if (!full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(full).StartsWith("godforge-provider-test-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing cleanup outside the owned test directory.");
            foreach (var file in Directory.EnumerateFiles(full, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(full, true);
        }
    }

    private static async Task<int> Git(string directory, string login, string token, params string[] args)
    {
        var info = new ProcessStartInfo("git") { WorkingDirectory = directory, RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in args) info.ArgumentList.Add(argument);
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        info.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        info.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(directory, "nonexistent-global-config");
        info.Environment["GIT_CONFIG_COUNT"] = "3";
        info.Environment["GIT_CONFIG_KEY_0"] = "http.extraHeader";
        info.Environment["GIT_CONFIG_VALUE_0"] = "Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(login + ":" + token));
        info.Environment["GIT_CONFIG_KEY_1"] = "http.followRedirects"; info.Environment["GIT_CONFIG_VALUE_1"] = "false";
        info.Environment["GIT_CONFIG_KEY_2"] = "credential.helper"; info.Environment["GIT_CONFIG_VALUE_2"] = "";
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        await Task.WhenAll(output, errors); // Never log raw Git output or credentials.
        return process.ExitCode;
    }
}
