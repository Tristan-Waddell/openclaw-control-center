#!/usr/bin/env bash
set -euo pipefail

# Wrapper: use canonical batch runner in repo to avoid drift.
exec /root/.openclaw/workspace/packcrafter-ai/scripts/batch/run-batch.sh "$@"
