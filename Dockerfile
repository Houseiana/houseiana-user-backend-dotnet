# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files first for restore
COPY Houseiana.Enums/Houseiana.Enums.csproj ./Houseiana.Enums/
COPY Houseiana.DAL/Houseiana.DAL.csproj ./Houseiana.DAL/
COPY Houseiana.DTOs/Houseiana.DTOs.csproj ./Houseiana.DTOs/
COPY Houseiana.Repositories/Houseiana.Repositories.csproj ./Houseiana.Repositories/
COPY Houseiana.Business/Houseiana.Business.csproj ./Houseiana.Business/
COPY src/HouseianaApi/HouseianaApi.csproj ./src/HouseianaApi/

# Restore dependencies
RUN dotnet restore ./src/HouseianaApi/HouseianaApi.csproj

# Copy the rest of the source code
COPY Houseiana.Enums/ ./Houseiana.Enums/
COPY Houseiana.DAL/ ./Houseiana.DAL/
COPY Houseiana.DTOs/ ./Houseiana.DTOs/
COPY Houseiana.Repositories/ ./Houseiana.Repositories/
COPY Houseiana.Business/ ./Houseiana.Business/
COPY src/HouseianaApi/ ./src/HouseianaApi/

# Build and publish
WORKDIR /src/src/HouseianaApi
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
