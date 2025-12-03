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

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "HouseianaApi.dll"]
