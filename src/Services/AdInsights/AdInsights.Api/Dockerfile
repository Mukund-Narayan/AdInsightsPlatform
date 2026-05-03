# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only csproj first (for better caching)
COPY ["src/Services/AdInsights/AdInsights.Api/AdInsights.Api.csproj", "AdInsights.Api/"]

# Restore dependencies
RUN dotnet restore "AdInsights.Api/AdInsights.Api.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/src/Services/AdInsights/AdInsights.Api"

# Publish the app
RUN dotnet publish "AdInsights.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose port (matches your docker-compose)
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "AdInsights.Api.dll"]