#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  run_pack_e2e.sh --repo <url> --ref <ref> --setup <cmd> --pack <cmd> --verify <cmd> --artifacts <a,b,c>
EOF
}

REPO=""; REF=""; SETUP=""; PACK=""; VERIFY=""; ARTIFACTS=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo) REPO="$2"; shift 2;;
    --ref) REF="$2"; shift 2;;
    --setup) SETUP="$2"; shift 2;;
    --pack) PACK="$2"; shift 2;;
    --verify) VERIFY="$2"; shift 2;;
    --artifacts) ARTIFACTS="$2"; shift 2;;
    -h|--help) usage; exit 0;;
    *) echo "Unknown arg: $1"; usage; exit 1;;
  esac
done

[[ -n "$REPO" && -n "$REF" && -n "$SETUP" && -n "$PACK" && -n "$VERIFY" && -n "$ARTIFACTS" ]] || { usage; exit 1; }

ROOT="/root/.openclaw/workspace"
RUN_ID="pack-e2e-$(date +%Y%m%d-%H%M%S)"
WORKDIR="$ROOT/tmp/$RUN_ID"
LOGDIR="$WORKDIR/logs"
REPODIR="$WORKDIR/repo"
mkdir -p "$LOGDIR"

echo "[INFO] RUN_ID=$RUN_ID"
echo "[INFO] Cloning $REPO"
git clone "$REPO" "$REPODIR" >"$LOGDIR/clone.log" 2>&1
cd "$REPODIR"
git fetch --all >"$LOGDIR/fetch.log" 2>&1 || true
git checkout "$REF" >"$LOGDIR/checkout.log" 2>&1

run_step() {
  local name="$1"
  local cmd="$2"
  echo "[STEP] $name"
  bash -lc "$cmd" >"$LOGDIR/${name}.log" 2>&1
}

run_step setup "$SETUP"
run_step pack "$PACK"
run_step verify "$VERIFY"

IFS=',' read -r -a arr <<< "$ARTIFACTS"
ART_TABLE="$WORKDIR/artifacts.txt"
: > "$ART_TABLE"
for rel in "${arr[@]}"; do
  rel_trimmed="$(echo "$rel" | xargs)"
  p="$REPODIR/$rel_trimmed"
  if [[ ! -f "$p" ]]; then
    echo "MISSING|$rel_trimmed" | tee -a "$ART_TABLE"
    exit 2
  fi
  size=$(stat -c%s "$p")
  sha=$(sha256sum "$p" | awk '{print $1}')
  echo "OK|$rel_trimmed|$size|$sha" | tee -a "$ART_TABLE"
done

echo "PASS"
echo "RUN_DIR=$WORKDIR"
echo "ARTIFACT_TABLE=$ART_TABLE"
