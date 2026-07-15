using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Analysis;

public sealed class DependencyGraphBuilder : IDependencyGraphBuilder
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".godot", ".import", "node_modules", "bin", "obj", "dist", "build", "vendor", ".cache"
    };

    private static readonly Regex ExtResourceRegex = new(
        @"\[ext_resource\s+type=""(?<type>[^""]+)""[^\]]*\bpath=""(?<path>res://[^""]+)""[^\]]*\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex ScriptDependenciesRegex = new(
        @"(?:extends|preload|load)\s*\(?\s*""(?<path>res://[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private readonly RepositoryProcessingSettings _settings;
    private readonly IClock _clock;

    public DependencyGraphBuilder(IOptions<RepositoryProcessingSettings> settings, IClock clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public async Task<(DependencyGraphSnapshot Snapshot, IReadOnlyList<DependencyGraphNode> Nodes, IReadOnlyList<DependencyGraphEdge> Edges)> BuildAsync(
        Guid projectId,
        Guid repositoryId,
        Guid snapshotId,
        Guid analysisRunId,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("Repository workspace does not exist.");
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        var files = Directory.EnumerateFiles(root, "*", options)
            .Where(path => !IsIgnored(root, path) && IsTrackedFile(path))
            .Take(_settings.MaxFiles + 1) // Just to prevent unbounded iteration, but analyzer already checks limits
            .ToList();

        var nodes = new Dictionary<string, DependencyGraphNode>(StringComparer.OrdinalIgnoreCase);
        var edges = new List<DependencyGraphEdge>();
        var cycleCount = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var nodeKey = $"res://{relativePath}";
            var nodeType = GetNodeType(file);

            if (!nodes.ContainsKey(nodeKey))
            {
                nodes[nodeKey] = DependencyGraphNode.Create(
                    snapshotId,
                    nodeKey,
                    nodeType,
                    relativePath,
                    Path.GetFileName(file),
                    null);
            }

            if (new FileInfo(file).Length > _settings.MaxTextFileBytes)
            {
                continue;
            }

            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);

                if (nodeType == "scene" || nodeType == "resource")
                {
                    foreach (Match match in ExtResourceRegex.Matches(content))
                    {
                        var targetPath = match.Groups["path"].Value;
                        if (!nodes.ContainsKey(targetPath))
                        {
                            var targetRelative = targetPath["res://".Length..];
                            nodes[targetPath] = DependencyGraphNode.Create(
                                snapshotId,
                                targetPath,
                                GetNodeType(targetRelative),
                                targetRelative,
                                Path.GetFileName(targetRelative),
                                null);
                        }

                        var relation = GetRelation(nodeType, nodes[targetPath].NodeType);
                        edges.Add(DependencyGraphEdge.Create(
                            snapshotId,
                            nodeKey,
                            targetPath,
                            relation,
                            1.0m));
                    }
                }
                else if (nodeType == "script")
                {
                    foreach (Match match in ScriptDependenciesRegex.Matches(content))
                    {
                        var targetPath = match.Groups["path"].Value;
                        if (!nodes.ContainsKey(targetPath))
                        {
                            var targetRelative = targetPath["res://".Length..];
                            nodes[targetPath] = DependencyGraphNode.Create(
                                snapshotId,
                                targetPath,
                                GetNodeType(targetRelative),
                                targetRelative,
                                Path.GetFileName(targetRelative),
                                null);
                        }

                        var relation = GetRelation(nodeType, nodes[targetPath].NodeType);
                        edges.Add(DependencyGraphEdge.Create(
                            snapshotId,
                            nodeKey,
                            targetPath,
                            relation,
                            1.0m));
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DecoderFallbackException)
            {
                // Ignore read errors for dependency extraction
            }
        }

        // Deduplicate edges
        var uniqueEdges = new List<DependencyGraphEdge>();
        var edgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            var key = $"{edge.SourceNodeKey}|{edge.TargetNodeKey}|{edge.Relation}";
            if (edgeKeys.Add(key))
            {
                uniqueEdges.Add(edge);
            }
        }

        // Simple cycle detection could go here if needed, setting cycleCount.
        // For MVP, we defer full cycle detection to the client or a background job.

        var graphHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(",", edgeKeys.OrderBy(k => k))));
        var graphHash = Convert.ToHexString(graphHashBytes).ToLowerInvariant();

        var snapshot = DependencyGraphSnapshot.Create(
            projectId,
            repositoryId,
            snapshotId,
            analysisRunId,
            graphHash,
            nodes.Count,
            uniqueEdges.Count,
            cycleCount,
            _clock.UtcNow);

        return (snapshot, nodes.Values.ToList(), uniqueEdges);
    }

    private static bool IsIgnored(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(IgnoredDirectories.Contains);
    }

    private static bool IsTrackedFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ext is ".tscn" or ".gd" or ".tres" or ".png" or ".jpg" or ".wav" or ".ogg" or ".json";
    }

    private static string GetNodeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".tscn" => "scene",
            ".gd" or ".cs" => "script",
            ".tres" => "resource",
            _ => "asset"
        };
    }

    private static string GetRelation(string sourceType, string targetType)
    {
        if (sourceType == "scene" && targetType == "scene") return "instances";
        if (sourceType == "scene" && targetType == "script") return "attaches";
        if (sourceType == "scene" && targetType == "resource") return "uses";
        if (sourceType == "scene" && targetType == "asset") return "references";
        if (sourceType == "script" && targetType == "script") return "extends"; // or load/preload
        if (sourceType == "script") return "load"; // preload/load scene or asset
        if (sourceType == "resource") return "references";
        return "references";
    }
}
