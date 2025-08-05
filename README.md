# ConnectCore - Microservices Platform

Enterprise-grade microservices API platform built with .NET 8.

## Overview

ConnectCore is a scalable microservices architecture featuring a centralized API gateway, service discovery, and multiple business services. Built with modern .NET technologies and containerized for production deployment.

## Architecture

- **API Gateway**: YARP reverse proxy with Swagger documentation
- **Service Discovery**: In-memory service registry for dynamic service location
- **User Service**: User management and authentication
- **Product Service**: Product catalog and inventory management
- **Order Service**: Order processing and management
- **Notification Service**: Multi-channel notification system
- **Shared Library**: Common DTOs, middleware, and validation logic

## Technology Stack

- ASP.NET Core 8.0
- Entity Framework Core (In-Memory)
- YARP Reverse Proxy
- Serilog Structured Logging
- FluentValidation
- Docker & Docker Compose

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Docker Desktop

### Local Development

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Run with Docker Compose
docker-compose up -d
```

### Production Deployment

```bash
# Build for production (single container)
docker build -f Dockerfile.render -t connectcore-api .

# Run container
docker run -d -p 8080:8080 connectcore-api

# Build full microservices stack
docker build -t connectcore-full .
```

## API Endpoints

Once running, the following endpoints are available:

- **API Gateway**: http://localhost:8080
- **Swagger Documentation**: http://localhost:8080/swagger
- **Health Checks**: http://localhost:8080/health

### Service URLs (Development)

- Service Discovery: http://localhost:8001
- User Service: http://localhost:8002
- Product Service: http://localhost:8003
- Order Service: http://localhost:8004
- Notification Service: http://localhost:8005

## Security Features

- Input validation with FluentValidation
- Structured logging with correlation IDs
- Non-root Docker containers
- Environment-based configuration
- CORS policies configured
- Health check endpoints

## Configuration

The application uses environment-based configuration:

- **Development**: Individual service configurations
- **Production**: Centralized configuration via `appsettings.Production.json`

## Monitoring & Health Checks

Health checks are available at `/health` for all services:

- Automated health monitoring
- Structured logging with correlation IDs
- Docker container health checks
- Service discovery integration

## Docker Support

### Multi-Service Deployment (Dockerfile)
- Runs all services in a single container
- Suitable for development and testing
- Uses startup script for service orchestration

### Single Service Deployment (Dockerfile.render)
- Optimized for production deployment
- API Gateway only with service discovery
- Smaller footprint and faster startup

## Development

### Project Structure
```
src/
├── ConnectCore.Gateway/          # API Gateway & Reverse Proxy
├── ConnectCore.ServiceDiscovery/ # Service Registry
├── ConnectCore.UserService/      # User Management
├── ConnectCore.ProductService/   # Product Catalog
├── ConnectCore.OrderService/     # Order Processing
├── ConnectCore.NotificationService/ # Notifications
└── ConnectCore.Shared/           # Shared Components
```

### Building the Solution

```bash
# Clean build
dotnet clean
dotnet restore
dotnet build --configuration Release

# Run tests (if available)
dotnet test

# Publish for deployment
dotnet publish --configuration Release
```

## Deployment

### Container Registry

```bash
# Tag for registry
docker tag connectcore-api your-registry.com/connectcore:latest

# Push to registry
docker push your-registry.com/connectcore:latest
```

### Cloud Deployment

The application is configured for deployment to:
- Docker-compatible platforms
- Kubernetes clusters
- Cloud container services

## License

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Support

For questions, issues, or contributions, please use the GitHub repository's issue tracker.
