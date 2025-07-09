# ConnectCore - API Integration Hub

A professional microservices-based API Integration Hub built with .NET 8.

## Quick Start

### Prerequisites
- .NET 8.0 SDK

### Local Development
```bash
git clone <repository-url>
cd ConnectCore
dotnet restore
dotnet build
```

### Docker Deployment
```bash
docker build -t connectcore .
docker run -p 8080:8080 connectcore
```

### Access
- Application: http://localhost:8080
- API Documentation: http://localhost:8080/swagger
- Health Check: http://localhost:8080/health

## Services
- API Gateway (Port 8080)
- Service Discovery
- User Service
- Product Service  
- Order Service
- Notification Service

## Technology Stack
- ASP.NET Core 8.0
- Entity Framework Core
- YARP Reverse Proxy
- Serilog Logging
- FluentValidation
- Swagger/OpenAPI

## Deployment

### Render (Recommended)
1. Fork this repository
2. Connect to Render
3. Create a new Web Service
4. Select this repository
5. Use the `render.yaml` configuration
6. Set environment variables in Render dashboard

### Alternative Cloud Platforms
- **Docker**: Use the included `Dockerfile` for containerized deployment
- **CI/CD**: GitHub Actions workflow included for automated deployment

## License
MIT License
