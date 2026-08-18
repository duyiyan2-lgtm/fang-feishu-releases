#!/usr/bin/env bash
set -euo pipefail

REMOVE_VOLUMES=false
if [[ "${1:-}" == "--remove-volumes" ]]; then
  REMOVE_VOLUMES=true
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$BACKEND_ROOT/.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/deploy/docker/docker-compose.prod.yml"
ENV_FILE="$REPO_ROOT/deploy/docker/.env.prod"

if ! docker info >/dev/null 2>&1; then
  echo "Docker Engine is not running. Please start Docker and run this script again." >&2
  exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE. Nothing to stop for the production stack." >&2
  exit 1
fi

if [[ "$REMOVE_VOLUMES" == "true" ]]; then
  echo "Stopping FangFeishu production backend stack and removing volumes..."
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" down -v
else
  echo "Stopping FangFeishu production backend stack..."
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" down
fi
