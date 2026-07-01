# Script to scaffold the Atlas Backend Solution (Atlas.sln)
$SolutionName = "Atlas.Backend"
$SrcDir = "src"
$TestsDir = "tests"

Write-Host "Creating Solution: $SolutionName"
dotnet new sln -n $SolutionName

Write-Host "Creating Domain Project"
dotnet new classlib -n Atlas.Domain -o $SrcDir/Atlas.Domain
dotnet sln add $SrcDir/Atlas.Domain/Atlas.Domain.csproj

Write-Host "Creating Application Project"
dotnet new classlib -n Atlas.Application -o $SrcDir/Atlas.Application
dotnet add $SrcDir/Atlas.Application/Atlas.Application.csproj reference $SrcDir/Atlas.Domain/Atlas.Domain.csproj
dotnet sln add $SrcDir/Atlas.Application/Atlas.Application.csproj

Write-Host "Creating Infrastructure Project"
dotnet new classlib -n Atlas.Infrastructure -o $SrcDir/Atlas.Infrastructure
dotnet add $SrcDir/Atlas.Infrastructure/Atlas.Infrastructure.csproj reference $SrcDir/Atlas.Application/Atlas.Application.csproj
dotnet sln add $SrcDir/Atlas.Infrastructure/Atlas.Infrastructure.csproj

Write-Host "Creating API Project"
dotnet new webapi -n Atlas.Api -o $SrcDir/Atlas.Api
dotnet add $SrcDir/Atlas.Api/Atlas.Api.csproj reference $SrcDir/Atlas.Application/Atlas.Application.csproj
dotnet add $SrcDir/Atlas.Api/Atlas.Api.csproj reference $SrcDir/Atlas.Infrastructure/Atlas.Infrastructure.csproj
dotnet sln add $SrcDir/Atlas.Api/Atlas.Api.csproj

Write-Host "Creating Worker Project"
dotnet new worker -n Atlas.Worker -o $SrcDir/Atlas.Worker
dotnet add $SrcDir/Atlas.Worker/Atlas.Worker.csproj reference $SrcDir/Atlas.Application/Atlas.Application.csproj
dotnet add $SrcDir/Atlas.Worker/Atlas.Worker.csproj reference $SrcDir/Atlas.Infrastructure/Atlas.Infrastructure.csproj
dotnet sln add $SrcDir/Atlas.Worker/Atlas.Worker.csproj

Write-Host "Creating Unit Tests Project"
dotnet new xunit -n Atlas.UnitTests -o $TestsDir/Atlas.UnitTests
dotnet sln add $TestsDir/Atlas.UnitTests/Atlas.UnitTests.csproj

Write-Host "Scaffolding completed successfully."
