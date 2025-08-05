# Multi-stage build for full microservices deployment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080 8001 8002 8003 8004 8005

# Install curl and bash for health checks and startup script
RUN apt-get update && apt-get install -y curl bash && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for better layer caching
COPY ["ConnectCore.sln", "."]
COPY ["src/ConnectCore.Gateway/ConnectCore.Gateway.csproj", "src/ConnectCore.Gateway/"]
COPY ["src/ConnectCore.ServiceDiscovery/ConnectCore.ServiceDiscovery.csproj", "src/ConnectCore.ServiceDiscovery/"]
COPY ["src/ConnectCore.UserService/ConnectCore.UserService.csproj", "src/ConnectCore.UserService/"]
COPY ["src/ConnectCore.ProductService/ConnectCore.ProductService.csproj", "src/ConnectCore.ProductService/"]
COPY ["src/ConnectCore.OrderService/ConnectCore.OrderService.csproj", "src/ConnectCore.OrderService/"]
COPY ["src/ConnectCore.NotificationService/ConnectCore.NotificationService.csproj", "src/ConnectCore.NotificationService/"]
COPY ["src/ConnectCore.Shared/ConnectCore.Shared.csproj", "src/ConnectCore.Shared/"]

# Restore dependencies with explicit runtime
RUN dotnet restore "ConnectCore.sln" --runtime linux-x64

# Copy all source code
COPY . .

# Build and publish all services with optimizations
RUN dotnet publish "src/ConnectCore.Gateway/ConnectCore.Gateway.csproj" \
    -c Release -o /app/gateway --no-restore --runtime linux-x64 --self-contained false && \
    dotnet publish "src/ConnectCore.ServiceDiscovery/ConnectCore.ServiceDiscovery.csproj" \
    -c Release -o /app/discovery --no-restore --runtime linux-x64 --self-contained false && \
    dotnet publish "src/ConnectCore.UserService/ConnectCore.UserService.csproj" \
    -c Release -o /app/users --no-restore --runtime linux-x64 --self-contained false && \
    dotnet publish "src/ConnectCore.ProductService/ConnectCore.ProductService.csproj" \
    -c Release -o /app/products --no-restore --runtime linux-x64 --self-contained false && \
    dotnet publish "src/ConnectCore.OrderService/ConnectCore.OrderService.csproj" \
    -c Release -o /app/orders --no-restore --runtime linux-x64 --self-contained false && \
    dotnet publish "src/ConnectCore.NotificationService/ConnectCore.NotificationService.csproj" \
    -c Release -o /app/notifications --no-restore --runtime linux-x64 --self-contained false

# Final production stage
FROM base AS final
WORKDIR /app

# Copy published applications
COPY --from=build /app/gateway ./gateway
COPY --from=build /app/discovery ./discovery
COPY --from=build /app/users ./users
COPY --from=build /app/products ./products
COPY --from=build /app/orders ./orders
COPY --from=build /app/notifications ./notifications

# Create startup script
RUN echo '#!/bin/bash\n\
set -e\n\
echo "Starting ConnectCore Microservices..."\n\
\n\
start_service() {\n\
    local service_name=$1\n\
    local service_path=$2\n\
    local service_dll=$3\n\
    local service_port=$4\n\
    \n\
    echo "Starting $service_name on port $service_port..."\n\
    cd /app/$service_path\n\
    nohup dotnet $service_dll --urls=http://0.0.0.0:$service_port > /app/logs/$service_name.log 2>&1 &\n\
    echo $! > /app/pids/$service_name.pid\n\
    cd /app\n\
}\n\
\n\
mkdir -p /app/logs /app/pids\n\
\n\
start_service "ServiceDiscovery" "discovery" "ConnectCore.ServiceDiscovery.dll" "8001"\n\
sleep 5\n\
\n\
start_service "UserService" "users" "ConnectCore.UserService.dll" "8002"\n\
start_service "ProductService" "products" "ConnectCore.ProductService.dll" "8003"\n\
start_service "OrderService" "orders" "ConnectCore.OrderService.dll" "8004"\n\
start_service "NotificationService" "notifications" "ConnectCore.NotificationService.dll" "8005"\n\
sleep 10\n\
\n\
echo "Starting API Gateway on port 8080..."\n\
cd /app/gateway\n\
exec dotnet ConnectCore.Gateway.dll --urls=http://0.0.0.0:8080\n\
' > /app/start.sh && chmod +x /app/start.sh

# Set ownership and permissions
RUN chown -R appuser:appgroup /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Production environment
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["/app/start.sh"]
