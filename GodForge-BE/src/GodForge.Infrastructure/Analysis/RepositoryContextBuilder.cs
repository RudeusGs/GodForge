using System.Security.Cryptography;
using System.Text;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Analysis;

public sealed class RepositoryContextBuilder : IRepositoryContextBuilder
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".godot", ".import", "node_modules", "bin", "obj", "dist", "build", "vendor", ".cache"
    };

    private static readonly HashSet<string> IgnoredFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", ".env.local", ".env.production", "id_rsa", "id_ed25519"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gd", ".tscn", ".tres", ".godot", ".cs", ".ts", ".js", ".vue", ".json", ".yml", ".yaml",
        ".md", ".txt", ".xml", ".props", ".targets", ".sln", ".csproj", ".shader", ".gdshader", ".cfg", ".ini"
    };

    private readonly RepositoryProcessingSettings _settings;
    private readonly ISecretRedactor _redactor;

    public RepositoryContextBuilder(
        IOptions<RepositoryProcessingSettings> settings,
        ISecretRedactor redactor)
    {
        _settings = settings.Value;
        _redactor = redactor;
    }

    public async Task<RepositoryContextArtifact> BuildAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("Repository workspace does not exist.");
        }

        var builder = new StringBuilder(Math.Min(_settings.MaxContextCharacters, 64 * 1024));
        var includedPaths = new List<string>();
        var warnings = new List<string>();
        var skipped = 0;
        var truncated = false;

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnored(root, path))
            .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
            .Take(_settings.MaxFiles + 1)
            .ToList();

        if (files.Count > _settings.MaxFiles)
        {
            files = files.Take(_settings.MaxFiles).ToList();
            warnings.Add("Repository file count exceeded the configured context inventory limit.");
            truncated = true;
        }

        builder.AppendLine("# Repository tree");
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AppendLine(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');

            if (!IsTextFile(file) || info.Length > _settings.MaxTextFileBytes)
            {
                skipped++;
                continue;
            }

            string content;
            try
            {
                content = await File.ReadAllTextAsync(file, cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                skipped++;
                continue;
            }
            catch (IOException)
            {
                skipped++;
                continue;
            }

            content = _redactor.Redact(content);
            var section = $"\n\n===== FILE: {relativePath} =====\n{content}";
            if (builder.Length + section.Length > _settings.MaxContextCharacters)
            {
                warnings.Add("AI context reached the configured character limit.");
                truncated = true;
                break;
            }

            builder.Append(section);
            includedPaths.Add(relativePath);
        }

        var normalized = builder.ToString();
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var inputHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new RepositoryContextArtifact(
            normalized,
            inputHash,
            includedPaths.Count,
            skipped,
            truncated,
            includedPaths,
            warnings);
    }

    private static bool IsIgnored(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(IgnoredDirectories.Contains))
        {
            return true;
        }

        var fileName = Path.GetFileName(path);
        return IgnoredFileNames.Contains(fileName) ||
               fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Path.GetExtension(fileName), ".pem", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Path.GetExtension(fileName), ".key", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextFile(string path)
    {
        var name = Path.GetFileName(path);
        if (string.Equals(name, "project.godot", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return TextExtensions.Contains(Path.GetExtension(path));
    }
}
