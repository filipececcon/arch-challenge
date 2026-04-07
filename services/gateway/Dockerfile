FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ArchChallenge.Gateway.csproj ./
RUN dotnet restore ArchChallenge.Gateway.csproj

COPY . ./
RUN dotnet publish ArchChallenge.Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ArchChallenge.Gateway.dll"]
