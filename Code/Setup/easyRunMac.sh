#!/bin/bash
# VNM EasyRun for macOS/Linux
set -e

APPHOST_PROJ="../Aspire/AppHost/AppHost.csproj"

# Check for .NET SDK
dotnet --version >/dev/null 2>&1 || { echo "Error: .NET SDK is not installed."; exit 1; }

echo "== VNM EasyRun (macOS/Linux) =="

# Function to check if a secret exists
function secret_exists() {
    dotnet user-secrets list --project "$APPHOST_PROJ" 2>/dev/null | grep -q "^$1\s*="
}


# Function to validate SQL Server password rules
function validate_sql_password() {
    local pwd="$1"
    # At least 8 chars, upper, lower, digit, special
    [[ ${#pwd} -ge 8 ]] || return 1
    [[ "$pwd" =~ [A-Z] ]] || return 1
    [[ "$pwd" =~ [a-z] ]] || return 1
    [[ "$pwd" =~ [0-9] ]] || return 1
    [[ "$pwd" =~ [^A-Za-z0-9] ]] || return 1
    return 0
}

# Function to set a secret interactively, with optional validation
function ensure_secret() {
    local key="$1"
    local prompt="$2"
    local validate_func="$3"
    if secret_exists "$key"; then
        if [[ "$key" == "Parameters:sql-password" || "$key" == "Parameters:res08-rabbitmq-password" ]]; then
            echo "Secret '$key' is already configured as ********."
        else
            echo "Secret '$key' is already configured."
        fi
    else
        while true; do
            set +e
            read -s -p "$prompt: " value
            echo
            if [ -z "$value" ]; then
                echo "Secret '$key' cannot be empty."; continue
            fi
            if [ -n "$validate_func" ]; then
                $validate_func "$value"
                if [ $? -ne 0 ]; then
                    echo
                    echo "ERROR: Password does not meet SQL Server requirements."
                    echo "Rules:"
                    echo "  - At least 8 characters"
                    echo "  - At least one uppercase letter (A-Z)"
                    echo "  - At least one lowercase letter (a-z)"
                    echo "  - At least one number (0-9)"
                    echo "  - At least one special character (!@# etc)"
                    echo
                    continue
                fi
            fi
            set -e
            break
        done
        dotnet user-secrets set "$key" "$value" --project "$APPHOST_PROJ"
        echo "Secret '$key' saved."
    fi
}

# Migrate legacy RabbitMQ secret if present
LEGACY_KEY="Parameters:rabbitmq-password"
CANONICAL_KEY="Parameters:res08-rabbitmq-password"
if secret_exists "$LEGACY_KEY" && ! secret_exists "$CANONICAL_KEY"; then
    legacy_value=$(dotnet user-secrets list --project "$APPHOST_PROJ" | grep "^$LEGACY_KEY" | sed "s/^$LEGACY_KEY\s*=\s*//")
    dotnet user-secrets set "$CANONICAL_KEY" "$legacy_value" --project "$APPHOST_PROJ"
    dotnet user-secrets remove "$LEGACY_KEY" --project "$APPHOST_PROJ"
    echo "Migrated legacy RabbitMQ secret."
fi

# Ensure required secrets
ensure_secret "Parameters:sql-password" "Enter SQL password" validate_sql_password
ensure_secret "Parameters:res08-rabbitmq-password" "Enter RabbitMQ password"



echo "Extracting SQL password from user-secrets..."
echo "Project path: $APPHOST_PROJ"
 dotnet user-secrets list --project "$APPHOST_PROJ" | sed -E 's/(Parameters:sql-password[[:space:]]*=[[:space:]]*).*/\1********/; s/(Parameters:res08-rabbitmq-password[[:space:]]*=[[:space:]]*).*/\1********/'
SQL_SA_PASSWORD=$(dotnet user-secrets list --project "$APPHOST_PROJ" | grep "Parameters:sql-password" | awk -F'=' '{print $2}' | xargs)
RABBITMQ_PASSWORD=$(dotnet user-secrets list --project "$APPHOST_PROJ" | grep "Parameters:res08-rabbitmq-password" | awk -F'=' '{print $2}' | xargs)
MASKED_SQL_SA_PASSWORD="********"
MASKED_RABBITMQ_PASSWORD="********"
echo "Extracted SQL password in easyRunMac.sh: '$MASKED_SQL_SA_PASSWORD'"
echo "Extracted RabbitMQ password in easyRunMac.sh: '$MASKED_RABBITMQ_PASSWORD'"
echo "Calling prereq-checkMac.sh with --sql-sa-password argument: '$MASKED_SQL_SA_PASSWORD'"
echo "Calling prereq-checkMac.sh with --rabbitmq-password argument: '$MASKED_RABBITMQ_PASSWORD'"

if [ -f "./prereq-checkMac.sh" ]; then
    echo "Running prerequisite checks..."
    bash ./prereq-checkMac.sh --sql-sa-password "$SQL_SA_PASSWORD" --rabbitmq-password "$RABBITMQ_PASSWORD"
else
    echo "(Skipping prereq-check: ./prereq-checkMac.sh not found)"
fi

echo "Starting AppHost..."
dotnet run --project "$APPHOST_PROJ"
