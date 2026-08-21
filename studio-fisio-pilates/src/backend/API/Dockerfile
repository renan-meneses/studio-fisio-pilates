# =============================
# Clinica API — .NET 8 build
# =============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Core/Domain/Clinica.Domain.csproj Core/Domain/
COPY Core/Application/Clinica.Application.csproj Core/Application/
COPY Infrastructure/Persistence/Clinica.Persistence.csproj Infrastructure/Persistence/
COPY Infrastructure/CrossCutting/Clinica.CrossCutting.csproj Infrastructure/CrossCutting/
COPY API/Clinica.API.csproj API/

RUN dotnet restore API/Clinica.API.csproj

COPY Core/ Core/
COPY Infrastructure/ Infrastructure/
COPY API/ API/

RUN dotnet publish API/Clinica.API.csproj -c Release -o /app/publish

# =============================
# Runtime
# =============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# Processo sem privilegios (usuario nao-root da imagem .NET 8)
USER app
ENTRYPOINT ["dotnet", "Clinica.API.dll"]