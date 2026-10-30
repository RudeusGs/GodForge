FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY GodForge-BE/ GodForge-BE/
RUN dotnet restore GodForge-BE/GodForge.sln
RUN dotnet publish GodForge-BE/src/GodForge.Api/GodForge.Api.csproj -c Release --no-restore -p:UseAppHost=false -o /publish/api
RUN dotnet publish GodForge-BE/src/GodForge.Worker/GodForge.Worker.csproj -c Release --no-restore -p:UseAppHost=false -o /publish/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /publish/api/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "GodForge.Api.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS worker
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir /workspaces && chown $APP_UID:$APP_UID /workspaces
WORKDIR /app
COPY --from=build /publish/worker/ ./
ENV RepositoryProcessing__WorkspaceRoot=/workspaces
USER $APP_UID
ENTRYPOINT ["dotnet", "GodForge.Worker.dll"]
