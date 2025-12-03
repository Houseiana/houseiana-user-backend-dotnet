# Houseiana API - .NET 9 Backend

A .NET 9 Web API backend for the Houseiana holiday homes booking platform. This is a migration from the original NestJS backend.

## Features

- **Booking Management** - Create, confirm, approve, reject, and cancel bookings
- **Availability Locking** - Calendar-based soft-hold and confirmed locking to prevent double bookings
- **User Management** - Basic user operations
- **Background Services** - Automatic cleanup of expired calendar holds

## Technology Stack

- **.NET 9** - Latest .NET framework
- **Entity Framework Core 9** - ORM for database operations
- **PostgreSQL** (Neon) - Database
- **Swagger/OpenAPI** - API documentation

## API Endpoints

### Booking Manager (`/booking-manager`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/booking-manager` | Create a new booking with availability locking |
| POST | `/booking-manager/{id}/confirm` | Confirm booking after payment |
| POST | `/booking-manager/{id}/approve` | Host approves a booking |
| POST | `/booking-manager/{id}/reject` | Host rejects a booking |
| POST | `/booking-manager/{id}/cancel` | Cancel a booking |
| GET | `/booking-manager/{id}` | Get booking by ID |
| GET | `/booking-manager/user/{userId}` | Get user bookings (query: role=guest|host) |

### Users (`/users`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/users/{id}` | Get user by ID |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | API status check |
| GET | `/health` | Health check |

## Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL database

### Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-host;Port=5432;Database=your-db;Username=your-user;Password=your-password;SSL Mode=Require"
  }
}
```

Or set the `DATABASE_URL` environment variable.

### Running the API

```bash
cd src/HouseianaApi
dotnet run
```

The API will be available at `http://localhost:5000` (or as configured).

### Swagger UI

Access the API documentation at: `http://localhost:5000/swagger`

## Project Structure

```
houseiana-dotnet/
├── src/
│   └── HouseianaApi/
│       ├── Controllers/       # API Controllers
│       ├── Data/              # DbContext
│       ├── DTOs/              # Data Transfer Objects
│       ├── Enums/             # Enumerations
│       ├── Models/            # Entity Models
│       ├── Services/          # Business Logic
│       └── Program.cs         # Application entry point
└── HouseianaApi.sln           # Solution file
```

## Migration from NestJS

This project is a direct migration from the NestJS backend (`houseiana-nest`), maintaining the same:
- API endpoints and routes
- Business logic
- Database schema compatibility
- Calendar locking mechanism

## License

Proprietary - Houseiana
