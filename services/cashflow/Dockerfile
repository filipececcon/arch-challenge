FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/cashflow/ ./services/cashflow/
WORKDIR /src/services/cashflow
RUN dotnet restore ArchChallenge.CashFlow.sln \
    && dotnet publish src/Api/Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "ArchChallenge.CashFlow.Api.dll"]
