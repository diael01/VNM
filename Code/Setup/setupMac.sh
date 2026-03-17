#!/bin/bash
# VNM Setup for macOS/Linux
set -e

# Default values
MODE="local"
CONTAINER_NAME="vnm-sqlserver"
SA_PASSWORD=""
SQL_HOST="localhost"
SQL_PORT=1433
SQL_USER="sa"
DATABASE_DIR="../Database"
TIMEOUT_SECS=120
FORCE_RECREATE=false

# Parse arguments (simple)
while [[ $# -gt 0 ]]; do
  case $1 in
    -Mode)
      MODE="$2"; shift 2;;
    -ContainerName)
      CONTAINER_NAME="$2"; shift 2;;
    -SaPassword)
      SA_PASSWORD="$2"; shift 2;;
    -SqlHost)
      SQL_HOST="$2"; shift 2;;
    -SqlPort)
      SQL_PORT="$2"; shift 2;;
    -SqlUser)
      SQL_USER="$2"; shift 2;;
    -DatabaseDir)
      DATABASE_DIR="$2"; shift 2;;
    -TimeoutSecs)
      TIMEOUT_SECS="$2"; shift 2;;
    -ForceRecreate)
      FORCE_RECREATE=true; shift;;
    *)
      shift;;
  esac
done

# Wait for SQL Server readiness
echo "Waiting for SQL Server container ($CONTAINER_NAME) to be ready..."
for ((i=0;i<TIMEOUT_SECS;i++)); do
  if docker exec "$CONTAINER_NAME" /opt/mssql-tools/bin/sqlcmd -S "$SQL_HOST,$SQL_PORT" -U "$SQL_USER" -P "$SA_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; then
    echo "SQL Server is ready."
    break
  fi
  sleep 1
done

if [[ $i -eq $TIMEOUT_SECS ]]; then
  echo "SQL Server did not become ready in $TIMEOUT_SECS seconds."
  exit 1
fi

# Drop and recreate databases
for db in VNM VNM_TEST; do
  echo "Dropping and recreating $db..."
  docker exec "$CONTAINER_NAME" /opt/mssql-tools/bin/sqlcmd -S "$SQL_HOST,$SQL_PORT" -U "$SQL_USER" -P "$SA_PASSWORD" -Q "DROP DATABASE IF EXISTS [$db]; CREATE DATABASE [$db];"
  echo "Seeding $db..."
  docker exec "$CONTAINER_NAME" /opt/mssql-tools/bin/sqlcmd -S "$SQL_HOST,$SQL_PORT" -U "$SQL_USER" -P "$SA_PASSWORD" -d "$db" -i "$DATABASE_DIR/${db}.sql"
  docker exec "$CONTAINER_NAME" /opt/mssql-tools/bin/sqlcmd -S "$SQL_HOST,$SQL_PORT" -U "$SQL_USER" -P "$SA_PASSWORD" -d "$db" -i "$DATABASE_DIR/Seed.sql"
done

echo "Setup complete."
