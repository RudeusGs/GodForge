# Script to scaffold the GodForge Backend solution (GodForge.Backend.sln).
$ErrorActionPreference = "Stop"

Set-Location -LiteralPath $PSScriptRoot

$SolutionName = "GodForge.Backend"
$SolutionFile = "$SolutionName.sln"
$SrcDir = "src"
$TestsDir = "tests"

$DomainProject = "GodForge.Domain"
$ApplicationProject = "GodForge.Application"
$InfrastructureProject = "GodForge.Infrastructure"
$ApiProject = "GodForge.Api"
$WorkerProject = "GodForge.Worker"
$UnitTestsProject = "GodForge.UnitTests"

function Invoke-DotNet {
    & dotnet @args

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Use-CentralTargetFramework {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProjectPath
    )

    $content = Get-Content -LiteralPath $ProjectPath
    $updated = $content | Where-Object { $_ -notmatch "^\s*<TargetFramework>.*?</TargetFramework>\s*$" }

    if ($updated.Count -ne $content.Count) {
        Set-Content -LiteralPath $ProjectPath -Value $updated
    }
}

if (Test-Path -LiteralPath $SolutionFile) {
    throw "$SolutionFile already exists. Remove the generated solution/projects before scaffolding again."
}

New-Item -ItemType Directory -Force -Path $SrcDir, $TestsDir | Out-Null

Write-Host "Creating solution: $SolutionFile"
$solutionTemplateHelp = & dotnet new sln -h
if ($solutionTemplateHelp -match "--format") {
    Invoke-DotNet new sln -n $SolutionName --format sln
}
else {
    Invoke-DotNet new sln -n $SolutionName
}

Write-Host "Creating Domain project"
Invoke-DotNet new classlib -n $DomainProject -o "$SrcDir/$DomainProject" --no-restore
Use-CentralTargetFramework "$SrcDir/$DomainProject/$DomainProject.csproj"
Invoke-DotNet sln $SolutionFile add "$SrcDir/$DomainProject/$DomainProject.csproj"

Write-Host "Creating Application project"
Invoke-DotNet new classlib -n $ApplicationProject -o "$SrcDir/$ApplicationProject" --no-restore
Use-CentralTargetFramework "$SrcDir/$ApplicationProject/$ApplicationProject.csproj"
Invoke-DotNet add "$SrcDir/$ApplicationProject/$ApplicationProject.csproj" reference "$SrcDir/$DomainProject/$DomainProject.csproj"
Invoke-DotNet sln $SolutionFile add "$SrcDir/$ApplicationProject/$ApplicationProject.csproj"

Write-Host "Creating Infrastructure project"
Invoke-DotNet new classlib -n $InfrastructureProject -o "$SrcDir/$InfrastructureProject" --no-restore
Use-CentralTargetFramework "$SrcDir/$InfrastructureProject/$InfrastructureProject.csproj"
Invoke-DotNet add "$SrcDir/$InfrastructureProject/$InfrastructureProject.csproj" reference "$SrcDir/$ApplicationProject/$ApplicationProject.csproj"
Invoke-DotNet sln $SolutionFile add "$SrcDir/$InfrastructureProject/$InfrastructureProject.csproj"

Write-Host "Creating API project"
Invoke-DotNet new webapi -n $ApiProject -o "$SrcDir/$ApiProject" --no-openapi --no-https --no-restore
Use-CentralTargetFramework "$SrcDir/$ApiProject/$ApiProject.csproj"
Invoke-DotNet add "$SrcDir/$ApiProject/$ApiProject.csproj" reference "$SrcDir/$ApplicationProject/$ApplicationProject.csproj"
Invoke-DotNet add "$SrcDir/$ApiProject/$ApiProject.csproj" reference "$SrcDir/$InfrastructureProject/$InfrastructureProject.csproj"
Invoke-DotNet sln $SolutionFile add "$SrcDir/$ApiProject/$ApiProject.csproj"

$apiProgramPath = "$SrcDir/$ApiProject/Program.cs"
@"
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
"@ | Set-Content -LiteralPath $apiProgramPath

Write-Host "Creating Worker project"
Invoke-DotNet new worker -n $WorkerProject -o "$SrcDir/$WorkerProject" --no-restore
Use-CentralTargetFramework "$SrcDir/$WorkerProject/$WorkerProject.csproj"
Invoke-DotNet add "$SrcDir/$WorkerProject/$WorkerProject.csproj" reference "$SrcDir/$ApplicationProject/$ApplicationProject.csproj"
Invoke-DotNet add "$SrcDir/$WorkerProject/$WorkerProject.csproj" reference "$SrcDir/$InfrastructureProject/$InfrastructureProject.csproj"
Invoke-DotNet sln $SolutionFile add "$SrcDir/$WorkerProject/$WorkerProject.csproj"

Write-Host "Creating Unit Tests project"
Invoke-DotNet new xunit -n $UnitTestsProject -o "$TestsDir/$UnitTestsProject" --no-restore
Use-CentralTargetFramework "$TestsDir/$UnitTestsProject/$UnitTestsProject.csproj"
Invoke-DotNet add "$TestsDir/$UnitTestsProject/$UnitTestsProject.csproj" reference "$SrcDir/$ApplicationProject/$ApplicationProject.csproj"
Invoke-DotNet sln $SolutionFile add "$TestsDir/$UnitTestsProject/$UnitTestsProject.csproj"

Write-Host "Scaffolding completed successfully."
Write-Host "Next steps:"
Write-Host "  dotnet restore"
Write-Host "  dotnet build --no-restore"
Write-Host "  dotnet test --no-build"
