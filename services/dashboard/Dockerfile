FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ArchChallenge.Contracts/ ./shared/ArchChallenge.Contracts/
COPY services/dashboard/ ./services/dashboard/
WORKDIR /src/services/dashboard
RUN dotnet restore ArchChallenge.Dashboard.sln \
    && dotnet publish src/Api/Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health/liveness || exit 1
ENTRYPOINT ["dotnet", "ArchChallenge.Dashboard.Api.dll"]
