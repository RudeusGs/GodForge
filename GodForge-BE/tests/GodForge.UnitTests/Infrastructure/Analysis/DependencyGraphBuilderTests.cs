using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Infrastructure.Analysis;

public sealed class DependencyGraphBuilderTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly RepositoryProcessingSettings _settings;
    private readonly DependencyGraphBuilder _builder;

    public DependencyGraphBuilderTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        _settings = new RepositoryProcessingSettings
        {
            MaxFiles = 1000,
            MaxTextFileBytes = 1024 * 1024
        };

        var optionsMock = new Mock<IOptions<RepositoryProcessingSettings>>();
        optionsMock.Setup(o => o.Value).Returns(_settings);

        _builder = new DependencyGraphBuilder(optionsMock.Object, _clockMock.Object);
    }

    [Fact]
    public async Task BuildAsync_EmptyDirectory_ReturnsEmptyGraph()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var (snapshot, nodes, edges) = await _builder.BuildAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), tempDir);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(0, snapshot.NodeCount);
            Assert.Empty(nodes);
            Assert.Empty(edges);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task BuildAsync_WithTscnAndGdFiles_ExtractsDependencies()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mainTscn = Path.Combine(tempDir, "main.tscn");
            var playerTscn = Path.Combine(tempDir, "player.tscn");
            var playerGd = Path.Combine(tempDir, "player.gd");
            var spritePng = Path.Combine(tempDir, "sprite.png");

            await File.WriteAllTextAsync(mainTscn, @"
[gd_scene load_steps=2 format=3 uid=""uid://xxx""]
[ext_resource type=""PackedScene"" uid=""uid://yyy"" path=""res://player.tscn"" id=""1_abc""]
[ext_resource type=""Texture2D"" uid=""uid://zzz"" path=""res://sprite.png"" id=""2_def""]
");

            await File.WriteAllTextAsync(playerTscn, @"
[gd_scene load_steps=2 format=3 uid=""uid://yyy""]
[ext_resource type=""Script"" uid=""uid://www"" path=""res://player.gd"" id=""1_xyz""]
");

            await File.WriteAllTextAsync(playerGd, @"
extends Node
var preload_scene = preload(""res://main.tscn"")
");

            await File.WriteAllTextAsync(spritePng, "fake binary data");

            // Act
            var (snapshot, nodes, edges) = await _builder.BuildAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), tempDir);

            // Assert
            Assert.Equal(4, nodes.Count);
            Assert.All(nodes, node => Assert.Equal(snapshot.Id, node.GraphSnapshotId));
            Assert.All(edges, edge => Assert.Equal(snapshot.Id, edge.GraphSnapshotId));
            Assert.Contains(nodes, n => n.NodeKey == "res://main.tscn" && n.NodeType == "scene");
            Assert.Contains(nodes, n => n.NodeKey == "res://player.tscn" && n.NodeType == "scene");
            Assert.Contains(nodes, n => n.NodeKey == "res://player.gd" && n.NodeType == "script");
            Assert.Contains(nodes, n => n.NodeKey == "res://sprite.png" && n.NodeType == "asset");

            Assert.Equal(4, edges.Count);
            // main.tscn -> player.tscn
            Assert.Contains(edges, e => e.SourceNodeKey == "res://main.tscn" && e.TargetNodeKey == "res://player.tscn" && e.Relation == "instances");
            // main.tscn -> sprite.png
            Assert.Contains(edges, e => e.SourceNodeKey == "res://main.tscn" && e.TargetNodeKey == "res://sprite.png" && e.Relation == "references");
            // player.tscn -> player.gd
            Assert.Contains(edges, e => e.SourceNodeKey == "res://player.tscn" && e.TargetNodeKey == "res://player.gd" && e.Relation == "attaches");
            // player.gd -> main.tscn
            Assert.Contains(edges, e => e.SourceNodeKey == "res://player.gd" && e.TargetNodeKey == "res://main.tscn" && e.Relation == "load"); // Since it uses preload
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task BuildAsync_IgnoresExcludedDirectories()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var gitDir = Path.Combine(tempDir, ".git");
            Directory.CreateDirectory(gitDir);
            await File.WriteAllTextAsync(Path.Combine(gitDir, "config.tscn"), "test");

            var godotDir = Path.Combine(tempDir, ".godot");
            Directory.CreateDirectory(godotDir);
            await File.WriteAllTextAsync(Path.Combine(godotDir, "editor.tscn"), "test");

            var srcDir = Path.Combine(tempDir, "src");
            Directory.CreateDirectory(srcDir);
            await File.WriteAllTextAsync(Path.Combine(srcDir, "valid.tscn"), "test");

            // Act
            var (snapshot, nodes, edges) = await _builder.BuildAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), tempDir);

            // Assert
            Assert.Single(nodes);
            Assert.Contains(nodes, n => n.NodeKey == "res://src/valid.tscn");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
