#!/bin/bash
# VNM Prerequisite Check for macOS/Linux
set -e

# Check for Docker
if ! command -v docker >/dev/null 2>&1; then
  echo "Error: Docker is not installed or not in PATH."
  exit 1
fi

echo "Checking Docker daemon..."
if ! docker info >/dev/null 2>&1; then
  echo "Error: Docker daemon is not running. Please start Docker Desktop."
  exit 1
fi

# Ensure containers
CONTAINERS=("vnm-sqlserver" "vnm-rabbitmq")
for cname in "${CONTAINERS[@]}"; do
  if ! docker ps --format '{{.Names}}' | grep -q "^$cname$"; then
    echo "Container $cname not running. Attempting to start..."
    docker start "$cname" || echo "Container $cname could not be started."
  else
    echo "Container $cname is running."
  fi

docker inspect "$cname" >/dev/null 2>&1 && echo "Container $cname details:" && docker inspect "$cname" | grep "Status" || echo "Container $cname not found."
docker logs "$cname" | tail -n 10

done

echo "Prerequisite check complete."
