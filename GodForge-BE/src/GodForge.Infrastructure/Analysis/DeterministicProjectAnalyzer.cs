using System.Text.RegularExpressions;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Analysis;

public sealed class DeterministicProjectAnalyzer : IDeterministicProjectAnalyzer
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".godot", ".import", "node_modules", "bin", "obj", "dist", "build", "vendor", ".cache"
    };

    private static readonly Regex ExternalResourceRegex = new(
        @"\[ext_resource\s+[^\]]*path=""(?<path>res://[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private readonly RepositoryProcessingSettings _settings;

    public DeterministicProjectAnalyzer(IOptions<RepositoryProcessingSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<DeterministicProjectSummary> AnalyzeAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(workspacePath);
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        var allFiles = Directory.EnumerateFiles(root, "*", options)
            .Where(path => !IsIgnored(root, path))
            .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
            .Take(_settings.MaxFiles + 1)
            .ToList();

        var findings = new List<DeterministicFinding>();
        var findingKeys = new HashSet<string>(StringComparer.Ordinal);
        if (allFiles.Count > _settings.MaxFiles)
        {
            AddFinding(findings, findingKeys, new(
                "REPOSITORY_FILE_LIMIT_EXCEEDED",
                "warning",
                "Repository contains more files than the configured analysis limit.",
                null));
            allFiles = allFiles.Take(_settings.MaxFiles).ToList();
        }

        var projectFile = Path.Combine(root, "project.godot");
        if (!File.Exists(projectFile))
        {
            AddFinding(findings, findingKeys, new(
                "GODOT_PROJECT_FILE_MISSING",
                "critical",
                "project.godot was not found at the repository root.",
                "project.godot"));
        }

        var totalBytes = allFiles.Sum(path => new FileInfo(path).Length);
        if (totalBytes > _settings.MaxRepositoryBytes)
        {
            AddFinding(findings, findingKeys, new(
                "REPOSITORY_SIZE_LIMIT_EXCEEDED",
                "critical",
                "Repository exceeds the configured processing size limit.",
                null));
        }

        var sceneFiles = allFiles
            .Where(path => string.Equals(Path.GetExtension(path), ".tscn", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var resourceFiles = allFiles
            .Where(path => string.Equals(Path.GetExtension(path), ".tres", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var scriptCount = allFiles.Count(path => string.Equals(Path.GetExtension(path), ".gd", StringComparison.OrdinalIgnoreCase));
        var textCount = allFiles.Count(IsKnownTextFile);

        if (sceneFiles.Count == 0)
        {
            AddFinding(findings, findingKeys, new(
                "GODOT_SCENE_NOT_FOUND",
                "warning",
                "No .tscn scenes were found in the repository.",
                null));
        }

        foreach (var file in sceneFiles.Concat(resourceFiles))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            var relativeSource = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (info.Length > _settings.MaxTextFileBytes)
            {
                AddFinding(findings, findingKeys, new(
                    "GODOT_TEXT_FILE_TOO_LARGE",
                    "warning",
                    "Godot text resource exceeded the parser size limit and was skipped.",
                    relativeSource));
                continue;
            }

            if (string.Equals(info.Extension, ".tscn", StringComparison.OrdinalIgnoreCase) && info.Length > 1024 * 1024)
            {
                AddFinding(findings, findingKeys, new(
                    "GODOT_SCENE_OVERSIZED",
                    "warning",
                    "Scene file is larger than 1 MiB and may be difficult to review or maintain.",
                    relativeSource));
            }

            string content;
            try
            {
                content = await File.ReadAllTextAsync(file, cancellationToken);
            }
            catch (IOException)
            {
                AddFinding(findings, findingKeys, new(
                    "PARSER_FILE_READ_FAILED",
                    "warning",
                    "The parser could not read this Godot text resource.",
                    relativeSource));
                continue;
            }

            foreach (Match match in ExternalResourceRegex.Matches(content))
            {
                var resourcePath = match.Groups["path"].Value;
                var repositoryRelative = resourcePath["res://".Length..].Replace('/', Path.DirectorySeparatorChar);
                var target = Path.GetFullPath(Path.Combine(root, repositoryRelative));
                if (!target.StartsWith(rootPrefix, StringComparison.Ordinal))
                {
                    AddFinding(findings, findingKeys, new(
                        "GODOT_RESOURCE_PATH_INVALID",
                        "critical",
                        $"External resource path escapes the repository root: {resourcePath}",
                        relativeSource));
                    continue;
                }

                if (!File.Exists(target))
                {
                    AddFinding(findings, findingKeys, new(
                        "GODOT_RESOURCE_MISSING",
                        "critical",
                        $"Referenced resource does not exist: {resourcePath}",
                        relativeSource));
                }
            }
        }

        return new DeterministicProjectSummary(
            File.Exists(projectFile),
            allFiles.Count,
            sceneFiles.Count,
            resourceFiles.Count,
            scriptCount,
            textCount,
            totalBytes,
            findings);
    }

    private static bool IsIgnored(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(IgnoredDirectories.Contains);
    }

    private static bool IsKnownTextFile(string path)
    {
        if (string.Equals(Path.GetFileName(path), "project.godot", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return new[] { ".gd", ".tscn", ".tres", ".cs", ".ts", ".js", ".vue", ".json", ".md" }
            .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddFinding(
        ICollection<DeterministicFinding> findings,
        ISet<string> keys,
        DeterministicFinding finding)
    {
        var key = $"{finding.Code}|{finding.FilePath}|{finding.Message}";
        if (keys.Add(key))
        {
            findings.Add(finding);
        }
    }
}
