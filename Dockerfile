# Multi-stage build for production deployment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8001 8002 8003 8004 8005

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ConnectCore.sln", "."]
COPY ["src/ConnectCore.Gateway/ConnectCore.Gateway.csproj", "src/ConnectCore.Gateway/"]
COPY ["src/ConnectCore.ServiceDiscovery/ConnectCore.ServiceDiscovery.csproj", "src/ConnectCore.ServiceDiscovery/"]
COPY ["src/ConnectCore.UserService/ConnectCore.UserService.csproj", "src/ConnectCore.UserService/"]
COPY ["src/ConnectCore.ProductService/ConnectCore.ProductService.csproj", "src/ConnectCore.ProductService/"]
COPY ["src/ConnectCore.OrderService/ConnectCore.OrderService.csproj", "src/ConnectCore.OrderService/"]
COPY ["src/ConnectCore.NotificationService/ConnectCore.NotificationService.csproj", "src/ConnectCore.NotificationService/"]
COPY ["src/ConnectCore.Shared/ConnectCore.Shared.csproj", "src/ConnectCore.Shared/"]

# Restore dependencies
RUN dotnet restore "ConnectCore.sln"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish "src/ConnectCore.Gateway/ConnectCore.Gateway.csproj" -c Release -o /app/gateway --no-restore
RUN dotnet publish "src/ConnectCore.ServiceDiscovery/ConnectCore.ServiceDiscovery.csproj" -c Release -o /app/discovery --no-restore
RUN dotnet publish "src/ConnectCore.UserService/ConnectCore.UserService.csproj" -c Release -o /app/users --no-restore
RUN dotnet publish "src/ConnectCore.ProductService/ConnectCore.ProductService.csproj" -c Release -o /app/products --no-restore
RUN dotnet publish "src/ConnectCore.OrderService/ConnectCore.OrderService.csproj" -c Release -o /app/orders --no-restore
RUN dotnet publish "src/ConnectCore.NotificationService/ConnectCore.NotificationService.csproj" -c Release -o /app/notifications --no-restore

# Final stage
FROM base AS final
WORKDIR /app

# Copy published applications
COPY --from=build /app/gateway ./gateway
COPY --from=build /app/discovery ./discovery
COPY --from=build /app/users ./users
COPY --from=build /app/products ./products
COPY --from=build /app/orders ./orders
COPY --from=build /app/notifications ./notifications

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create startup script
RUN echo '#!/bin/bash\n\
# Start all services in background\n\
cd /app/discovery && dotnet ConnectCore.ServiceDiscovery.dll --urls=http://0.0.0.0:8001 &\n\
sleep 5\n\
cd /app/users && dotnet ConnectCore.UserService.dll --urls=http://0.0.0.0:8002 &\n\
cd /app/products && dotnet ConnectCore.ProductService.dll --urls=http://0.0.0.0:8003 &\n\
cd /app/orders && dotnet ConnectCore.OrderService.dll --urls=http://0.0.0.0:8004 &\n\
cd /app/notifications && dotnet ConnectCore.NotificationService.dll --urls=http://0.0.0.0:8005 &\n\
sleep 10\n\
cd /app/gateway && dotnet ConnectCore.Gateway.dll --urls=http://0.0.0.0:8080\n\
' > /app/start.sh && chmod +x /app/start.sh

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["/app/start.sh"]
