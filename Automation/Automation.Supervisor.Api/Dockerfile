#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Create .nuget directory with proper permissions BEFORE switching to app user
USER root
RUN mkdir -p /home/app/.nuget/NuGet && \
    chown -R app:app /home/app/.nuget && \
    chmod -R 755 /home/app/.nuget

USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Automation.Supervisor.Api/Automation.Supervisor.Api.csproj", "Automation.Supervisor.Api/"]
RUN dotnet restore "./Automation.Supervisor.Api/Automation.Supervisor.Api.csproj"
COPY . .
WORKDIR "/src/Automation.Supervisor.Api"
RUN dotnet build "./Automation.Supervisor.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Automation.Supervisor.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Automation.Supervisor.Api.dll"]