# Build context esperado: backend/src/ (ver docker-compose.build no deploy/docker-compose.yml
# e o passo de build em .github/workflows/ci.yml) — os caminhos abaixo são relativos a essa raiz.

# Estágio "base": o Visual Studio (F5 com docker-compose) o usa no modo rápido de depuração.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura só com os .csproj primeiro, pra cachear a camada de restore entre builds
# (o restore é a parte mais lenta e não muda a cada alteração de código).
COPY Sbar.Domain/*.csproj Sbar.Domain/
COPY Sbar.Application/*.csproj Sbar.Application/
COPY Sbar.Infrastructure/*.csproj Sbar.Infrastructure/
COPY Sbar.Api/*.csproj Sbar.Api/
RUN dotnet restore Sbar.Api/Sbar.Api.csproj

COPY Sbar.Domain/ Sbar.Domain/
COPY Sbar.Application/ Sbar.Application/
COPY Sbar.Infrastructure/ Sbar.Infrastructure/
COPY Sbar.Api/ Sbar.Api/
RUN dotnet publish Sbar.Api/Sbar.Api.csproj --no-restore --configuration Release --output /app/publish

FROM base AS runtime
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Sbar.Api.dll"]
