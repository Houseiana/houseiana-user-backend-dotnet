# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/HouseianaApi/HouseianaApi.csproj ./HouseianaApi/
RUN dotnet restore ./HouseianaApi/HouseianaApi.csproj

# Copy the rest of the source code
COPY src/HouseianaApi/ ./HouseianaApi/

# Build and publish
WORKDIR /src/HouseianaApi
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080

# Run the application
ENTRYPOINT ["dotnet", "HouseianaApi.dll"]
