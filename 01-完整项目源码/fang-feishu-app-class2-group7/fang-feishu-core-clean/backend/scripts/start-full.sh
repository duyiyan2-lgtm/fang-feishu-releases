#!/usr/bin/env bash
set -euo pipefail

RESET=false
if [[ "${1:-}" == "--reset" ]]; then
  RESET=true
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$BACKEND_ROOT/.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/deploy/docker/docker-compose.prod.yml"
ENV_FILE="$REPO_ROOT/deploy/docker/.env.prod"
ENV_EXAMPLE="$REPO_ROOT/deploy/docker/.env.prod.example"

if ! docker info >/dev/null 2>&1; then
  echo "Docker Engine is not running. Please start Docker and run this script again." >&2
  exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
  cp "$ENV_EXAMPLE" "$ENV_FILE"
  echo "Created $ENV_FILE from .env.prod.example." >&2
  echo "Please edit passwords, domain and CORS values, then run this script again." >&2
  exit 1
fi

api_port="$(grep -E '^API_PORT=' "$ENV_FILE" | tail -n 1 | cut -d '=' -f 2-)"
api_port="${api_port:-5080}"
health_url="http://127.0.0.1:${api_port}/health"

if [[ "$RESET" == "true" ]]; then
  echo "Resetting production stack containers and volumes..."
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" down -v
fi

echo "Starting FangFeishu production backend stack..."
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --build

echo "Waiting for API health check: $health_url"
deadline=$((SECONDS + 120))
while [[ $SECONDS -lt $deadline ]]; do
  if curl -fsS "$health_url" >/dev/null 2>&1; then
    echo "FangFeishu backend stack is ready."
    echo "API:     http://127.0.0.1:${api_port}"
    echo "Swagger: http://127.0.0.1:${api_port}/swagger"
    exit 0
  fi
  sleep 2
done

echo "API did not become healthy in time. Showing container status:" >&2
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps
exit 1
