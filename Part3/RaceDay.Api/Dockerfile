# Build stage: restore and publish with the full SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY RaceDay.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage: only the ASP.NET runtime, running as a non-root user
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
