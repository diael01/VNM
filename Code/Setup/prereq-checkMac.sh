#!/bin/bash
# VNM Prerequisite Check for macOS/Linux (migrated from Windows PowerShell)
set -e

# Default values
REQUIRE_DOCKER=false
ENSURE_CONTAINERS=("vnm-sqlserver" "vnm-rabbitmq")
SQL_CONTAINER_IMAGE="mcr.microsoft.com/mssql/server:2022-latest"
RABBITMQ_CONTAINER_IMAGE="rabbitmq:3-management"
SQL_SA_PASSWORD=""
RABBITMQ_PASSWORD=""
TIMEOUT=180
INTERVAL=4

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --require-docker)
      REQUIRE_DOCKER=true; shift;;
    --ensure-container)
      IFS=' ' read -r -a ENSURE_CONTAINERS <<< "$2"; shift 2;;
    --sql-container-image)
      SQL_CONTAINER_IMAGE="$2"; shift 2;;
    --rabbitmq-container-image)
      RABBITMQ_CONTAINER_IMAGE="$2"; shift 2;;
    --sql-sa-password)
      MASKED_SQL_SA_PASSWORD="********"
      echo "Received --sql-sa-password argument in prereq-checkMac.sh: '$MASKED_SQL_SA_PASSWORD'"
      SQL_SA_PASSWORD="$2"; shift 2;;
    --rabbitmq-password)
      RABBITMQ_PASSWORD="$2"; shift 2;;
    *)
      shift;;
  esac
done

# Check for Docker
if ! command -v docker >/dev/null 2>&1; then
  echo "Error: Docker is not installed or not in PATH."
  exit 1
fi

# Wait for Docker daemon
for ((i=0;i<TIMEOUT;i+=INTERVAL)); do
  if docker info >/dev/null 2>&1; then
    echo "Docker daemon is ready."
    break
  fi
  sleep $INTERVAL
  echo "Waiting for Docker daemon..."
done
if ! docker info >/dev/null 2>&1; then
  echo "Error: Docker daemon is not running after waiting. Please start Docker Desktop."
  exit 1
fi




for cname in "${ENSURE_CONTAINERS[@]}"; do
  echo "\n--- Checking container: $cname ---"
  CREATE_CONTAINER=false
  if docker ps -a --format '{{.Names}}' | grep -q "^$cname$"; then
    echo "Container $cname exists. Checking status..."
    STATUS=$(docker inspect --format='{{.State.Status}}' "$cname")
    if [ "$STATUS" = "running" ]; then
      echo "Container $cname is running. No action needed."
    else
      echo "Container $cname exists but is not running (status: $STATUS). Removing for recreation..."
      docker rm -f "$cname" && echo "Removed $cname successfully." || echo "Failed to remove $cname."
      CREATE_CONTAINER=true
    fi
  else
    echo "Container $cname does not exist. Will create."
    CREATE_CONTAINER=true
  fi

  if [ "$CREATE_CONTAINER" = true ]; then
    echo "Creating container $cname..."
    if [[ "$cname" == "vnm-sqlserver" ]]; then
      # Default port
      SQL_PORT=14333
      APPSETTINGS_PATH="../Aspire/AppHost/appsettings.json"
      if [ -f "$APPSETTINGS_PATH" ]; then
        echo "Reading SQL port from $APPSETTINGS_PATH..."
        SQL_PORT=$(grep '"SqlPort"' "$APPSETTINGS_PATH" | head -1 | grep -o '[0-9]\+')
        if [ -z "$SQL_PORT" ]; then
          echo "SqlPort not found in appsettings.json, using default 14333."
          SQL_PORT=14333
        else
          echo "Found SqlPort in appsettings.json: $SQL_PORT"
        fi
      else
        echo "appsettings.json not found, using default SQL port 14333."
      fi
      echo "Using SQL port: $SQL_PORT"
        MASKED_SQL_SA_PASSWORD="********"
        echo "SQL password being used (masked): $MASKED_SQL_SA_PASSWORD"
        echo "Running: docker run -d --name $cname --platform linux/amd64 -e ACCEPT_EULA=Y -e SA_PASSWORD=$MASKED_SQL_SA_PASSWORD -p $SQL_PORT:1433 $SQL_CONTAINER_IMAGE"
      docker run -d --name "$cname" --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=$SQL_SA_PASSWORD" -p "$SQL_PORT:1433" "$SQL_CONTAINER_IMAGE" && echo "Created SQL container $cname." || echo "Failed to create SQL container."
      unset SQL_SA_PASSWORD
      # Check if the container is running after creation
      if docker ps --format '{{.Names}}' | grep -q "^$cname$"; then
        echo "$cname is running after creation."
      else
        echo "Warning: $cname was created but is not running!"
        echo "Last 20 logs for $cname:"
        docker logs "$cname" 2>&1 | tail -n 20
        echo "Container inspect status:"
        docker inspect "$cname" 2>&1 | grep -i status
      fi
    elif [[ "$cname" == "vnm-rabbitmq" ]]; then
      echo "Running: docker run -d --name $cname -e RABBITMQ_DEFAULT_PASS=**** -p 5672:5672 -p 15672:15672 $RABBITMQ_CONTAINER_IMAGE"
      docker run -d --name "$cname" -e "RABBITMQ_DEFAULT_PASS=$RABBITMQ_PASSWORD" -p 5672:5672 -p 15672:15672 "$RABBITMQ_CONTAINER_IMAGE" && echo "Created RabbitMQ container $cname." || echo "Failed to create RabbitMQ container."
    else
      echo "Unknown container $cname. Skipping creation."
    fi
    CREATE_CONTAINER=false
  fi

  docker inspect "$cname" >/dev/null 2>&1 && echo "Container $cname details:" && docker inspect "$cname" | grep "Status" || echo "Container $cname not found."
  docker logs "$cname" | tail -n 10
done

echo "Prerequisite check complete."
