FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/LevelMovement.Domain/LevelMovement.Domain.csproj src/LevelMovement.Domain/
COPY src/LevelMovement.Application/LevelMovement.Application.csproj src/LevelMovement.Application/
COPY src/LevelMovement.Infrastructure/LevelMovement.Infrastructure.csproj src/LevelMovement.Infrastructure/
COPY src/LevelMovement.Api/LevelMovement.Api.csproj src/LevelMovement.Api/
RUN dotnet restore src/LevelMovement.Api/LevelMovement.Api.csproj
COPY src/ src/
RUN dotnet publish src/LevelMovement.Api/LevelMovement.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTPS_PORTS=8443
EXPOSE 8443
USER $APP_UID
ENTRYPOINT ["dotnet", "LevelMovement.Api.dll"]
