# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj first (for caching)
COPY ["src/Services/EventCollector/EventCollector.Api/EventCollector.Api.csproj", "EventCollector.Api/"]

# Restore dependencies
RUN dotnet restore "EventCollector.Api/EventCollector.Api.csproj"

# Copy full source
COPY . .

WORKDIR "/src/src/Services/EventCollector/EventCollector.Api"

# Publish
RUN dotnet publish "EventCollector.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose port (choose one)
EXPOSE 8080

# Run app
ENTRYPOINT ["dotnet", "EventCollector.Api.dll"]