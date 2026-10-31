FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore from the project files alone, so this layer stays cached until a package changes.
COPY FitTrack.Domain/FitTrack.Domain.csproj FitTrack.Domain/
COPY FitTrack.Application/FitTrack.Application.csproj FitTrack.Application/
COPY FitTrack.Infrastructure/FitTrack.Infrastructure.csproj FitTrack.Infrastructure/
COPY FitTrack.Api/FitTrack.Api.csproj FitTrack.Api/
RUN dotnet restore FitTrack.Api/FitTrack.Api.csproj

COPY FitTrack.Domain/ FitTrack.Domain/
COPY FitTrack.Application/ FitTrack.Application/
COPY FitTrack.Infrastructure/ FitTrack.Infrastructure/
COPY FitTrack.Api/ FitTrack.Api/
RUN dotnet publish FitTrack.Api/FitTrack.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./

# DbSeeder reads ContentRootPath/../seed-data.
COPY seed-data /seed-data

# Stays on the image's default root user: Vercel routes to port 80, which a non-root
# user may not be able to bind.
#
# The base image sets ASPNETCORE_HTTP_PORTS=8080; the entrypoint binds $PORT instead.
# FORWARDEDHEADERS_ENABLED takes the client IP from X-Forwarded-For and trusts whichever
# proxy connects. That is safe only behind a proxy that overwrites the header, as Vercel
# does, so it is set here and not in the app's settings.
ENV ASPNETCORE_HTTP_PORTS= \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_EnableDiagnostics=0

# exec lets the app receive SIGTERM directly when Vercel scales an instance down.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-80} exec dotnet FitTrack.Api.dll"]
