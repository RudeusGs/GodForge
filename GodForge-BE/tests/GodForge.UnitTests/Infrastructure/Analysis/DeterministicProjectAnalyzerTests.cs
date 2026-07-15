using GodForge.Infrastructure.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace GodForge.UnitTests.Infrastructure.Analysis;

public class DeterministicProjectAnalyzerTests : IDisposable
{
    private readonly RepositoryProcessingSettings _settings;
    private readonly DeterministicProjectAnalyzer _analyzer;
    private readonly string _tempRoot;

    public DeterministicProjectAnalyzerTests()
    {
        _settings = new RepositoryProcessingSettings
        {
            WorkspaceRoot = Path.GetTempPath(),
            MaxFiles = 10,
            MaxRepositoryBytes = 10 * 1024 * 1024,
            MaxTextFileBytes = 1024 * 1024,
            GitCommandTimeoutSeconds = 30
        };

        var optionsMock = new Mock<IOptions<RepositoryProcessingSettings>>();
        optionsMock.Setup(o => o.Value).Returns(_settings);

        _analyzer = new DeterministicProjectAnalyzer(optionsMock.Object);
        _tempRoot = Path.Combine(Path.GetTempPath(), "GodForgeTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); } catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var invalidPath = Path.Combine(_tempRoot, "nonexistent");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _analyzer.AnalyzeAsync(invalidPath));
    }

    [Fact]
    public async Task AnalyzeAsync_WithMissingProjectGodot_ReturnsCriticalFinding()
    {
        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.False(result.HasProjectFile);
        Assert.Contains(result.Findings, f => f.Code == "GODOT_PROJECT_FILE_MISSING");
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidProject_ReturnsCorrectStats()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempRoot, "project.godot"), "; Godot");
        File.WriteAllText(Path.Combine(_tempRoot, "main.tscn"), "[gd_scene format=3]");
        File.WriteAllText(Path.Combine(_tempRoot, "player.gd"), "extends Node");
        File.WriteAllText(Path.Combine(_tempRoot, "data.tres"), "[gd_resource]");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.True(result.HasProjectFile);
        Assert.Equal(4, result.TotalFiles);
        Assert.Equal(1, result.SceneFiles);
        Assert.Equal(1, result.ResourceFiles);
        Assert.Equal(1, result.ScriptFiles);
        Assert.Equal(4, result.TextFiles);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task AnalyzeAsync_WithIgnoredDirectories_SkipsThem()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempRoot, "project.godot"), "; Godot");
        File.WriteAllText(Path.Combine(_tempRoot, "main.tscn"), "[gd_scene format=3]");
        
        var gitDir = Path.Combine(_tempRoot, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(gitDir, "config"), "[core]");

        var godotDir = Path.Combine(_tempRoot, ".godot");
        Directory.CreateDirectory(godotDir);
        File.WriteAllText(Path.Combine(godotDir, "editor_layout.cfg"), "...");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.True(result.HasProjectFile);
        Assert.Equal(2, result.TotalFiles); // project.godot and main.tscn
        Assert.Empty(result.Findings);
    }

    [Fact]
    public async Task AnalyzeAsync_ExceedingMaxFiles_TruncatesAndReturnsWarning()
    {
        // Arrange
        _settings.MaxFiles = 2; // Very small limit
        File.WriteAllText(Path.Combine(_tempRoot, "project.godot"), "; Godot");
        File.WriteAllText(Path.Combine(_tempRoot, "file1.txt"), "data");
        File.WriteAllText(Path.Combine(_tempRoot, "file2.txt"), "data");
        File.WriteAllText(Path.Combine(_tempRoot, "file3.txt"), "data");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Contains(result.Findings, f => f.Code == "REPOSITORY_FILE_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task AnalyzeAsync_WithExternalResource_Missing_ReturnsCriticalFinding()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempRoot, "project.godot"), "; Godot");
        File.WriteAllText(Path.Combine(_tempRoot, "main.tscn"), 
            @"[ext_resource type=""Script"" path=""res://missing.gd"" id=""1_abc""]");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.Contains(result.Findings, f => f.Code == "GODOT_RESOURCE_MISSING");
    }

    [Fact]
    public async Task AnalyzeAsync_WithExternalResource_Valid_ReturnsNoFinding()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempRoot, "project.godot"), "; Godot");
        File.WriteAllText(Path.Combine(_tempRoot, "existing.gd"), "extends Node");
        File.WriteAllText(Path.Combine(_tempRoot, "main.tscn"), 
            @"[ext_resource type=""Script"" path=""res://existing.gd"" id=""1_abc""]");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.DoesNotContain(result.Findings, f => f.Code == "GODOT_RESOURCE_MISSING");
        Assert.DoesNotContain(result.Findings, f => f.Code == "GODOT_RESOURCE_PATH_INVALID");
    }

    [Fact]
    public async Task AnalyzeAsync_WithExternalResource_PathTraversal_ReturnsCriticalFinding()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempRoot, "project.godot"), "; Godot");
        // Using an escaped res:// path that tries to go outside the repo
        File.WriteAllText(Path.Combine(_tempRoot, "main.tscn"), 
            @"[ext_resource type=""Script"" path=""res://../outside.gd"" id=""1_abc""]");

        // Act
        var result = await _analyzer.AnalyzeAsync(_tempRoot);

        // Assert
        Assert.Contains(result.Findings, f => f.Code == "GODOT_RESOURCE_PATH_INVALID");
    }
}
