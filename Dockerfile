FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UrlShortener.slnx ./
COPY src/UrlShortener.Api/UrlShortener.Api.csproj src/UrlShortener.Api/
COPY src/UrlShortener.Application/UrlShortener.Application.csproj src/UrlShortener.Application/
COPY src/UrlShortener.Domain/UrlShortener.Domain.csproj src/UrlShortener.Domain/
COPY src/UrlShortener.Infrastructure/UrlShortener.Infrastructure.csproj src/UrlShortener.Infrastructure/
RUN dotnet restore src/UrlShortener.Api/UrlShortener.Api.csproj

COPY src/ src/
RUN dotnet publish src/UrlShortener.Api/UrlShortener.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Silences Npgsql's non-fatal "Cannot load library libgssapi_krb5.so.2" log noise on startup.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]
