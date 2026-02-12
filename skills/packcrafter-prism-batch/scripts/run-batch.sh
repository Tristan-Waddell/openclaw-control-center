#!/usr/bin/env bash
set -u

BATCH_SIZE="${1:-${BATCH_SIZE:-25}}"
MC_VERSION="${MC_VERSION:-1.20.1}"
LOADER="${LOADER:-fabric}"
LAUNCH_TIMEOUT_SEC="${LAUNCH_TIMEOUT_SEC:-180}"

PACKCRAFTER_REPO="/root/.openclaw/workspace/packcrafter-ai"
EXPORT_DIR="/home/tristanwaddell/Downloads/PackCrafter-Exports"
PRISM_ROOT="/home/tristanwaddell/.var/app/org.prismlauncher.PrismLauncher/data/PrismLauncher"
SKILL_DIR="/root/.openclaw/workspace/skills/packcrafter-prism-batch"

TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BATCH_DIR="$PACKCRAFTER_REPO/output/batch-25/$TIMESTAMP"
RUN_INDEX="$BATCH_DIR/run-index.jsonl"
mkdir -p "$BATCH_DIR" "$EXPORT_DIR"

# Preflight: ensure generator script does not depend on external dotenv module.
# We intentionally parse /root/.openclaw/secrets/packcrafter.env inline to avoid runtime module drift.
if rg -n "require\(['\"]dotenv['\"]\)" "$SKILL_DIR/scripts/generate-pack.cjs" >/dev/null 2>&1; then
  echo "Preflight failed: generate-pack.cjs still requires dotenv. Remove that dependency before running batch." >&2
  exit 3
fi

THEMES=(
  "sky islands survival" "hardcore winter wasteland" "steampunk automation city"
  "cozy farming village" "dark magic academy" "underwater ocean colonization"
  "jungle ruins exploration" "nether industrial challenge" "space colonization progression"
  "medieval kingdom building" "cave dweller survival" "desert nomad trading"
  "post-apocalyptic scavenger" "skyblock tech progression" "pirate archipelago adventure"
  "volcanic biome survival" "arctic expedition research" "vampire gothic rpg"
  "pokemon creature collecting" "factory megabase optimization" "dungeon crawler loot hunt"
  "ecological restoration" "railway empire logistics" "wizard tower progression"
  "prehistoric dinosaur world" "haunted horror survival" "astral dimension travel"
  "city life roleplay" "bunker survival" "mythological beasts quest"
)

if (( BATCH_SIZE > ${#THEMES[@]} )); then
  echo "Requested $BATCH_SIZE runs but only ${#THEMES[@]} unique themes available" >&2
  exit 2
fi

# Unique randomized theme order
mapfile -t SELECTED_THEMES < <(printf '%s\n' "${THEMES[@]}" | shuf | head -n "$BATCH_SIZE")

cleanup_processes() {
  sudo -u tristanwaddell bash -lc "pkill -f 'prismlauncher' || true; pkill -f 'java.*minecraft' || true"
  sleep 2
}

copy_if_exists() {
  local src="$1"
  local dst="$2"
  if [[ -e "$src" ]]; then
    cp -a "$src" "$dst"
  fi
}

for ((i=1; i<=BATCH_SIZE; i++)); do
  run_id="$(printf 'run-%02d' "$i")"
  theme="${SELECTED_THEMES[$((i-1))]}"
  run_slug="$(echo "$theme" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-//;s/-$//')"
  run_dir="$BATCH_DIR/${run_id}-${run_slug}"
  mkdir -p "$run_dir"

  start_epoch="$(date +%s)"
  started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

  echo "[$run_id] Theme: $theme"

  gen_json_file="$run_dir/generation-meta.json"
  gen_log="$run_dir/generation.log"

  # Generation step
  if node "$SKILL_DIR/scripts/generate-pack.cjs" "$theme" "$MC_VERSION" "$LOADER" "$run_dir" >"$gen_json_file" 2>"$gen_log"; then
    gen_ok=true
  else
    gen_ok=false
  fi

  mrpack_path=""
  pack_name=""
  used_fallback="false"
  candidates_found="0"
  mods_found="0"
  conflicts_found="0"

  if [[ "$gen_ok" == true ]]; then
    mrpack_path="$(python3 - <<'PY' "$gen_json_file"
import json,sys
j=json.load(open(sys.argv[1]))
print(j.get('mrpackPath',''))
PY
)"
    pack_name="$(python3 - <<'PY' "$gen_json_file"
import json,sys
j=json.load(open(sys.argv[1]))
print(j.get('packName',''))
PY
)"
    used_fallback="$(python3 - <<'PY' "$gen_json_file"
import json,sys
j=json.load(open(sys.argv[1]))
print(str(j.get('usedFallback', False)).lower())
PY
)"
    candidates_found="$(python3 - <<'PY' "$gen_json_file"
import json,sys
j=json.load(open(sys.argv[1]))
print(j.get('candidatesFound',0))
PY
)"
    mods_found="$(python3 - <<'PY' "$gen_json_file"
import json,sys
j=json.load(open(sys.argv[1]))
print(j.get('modsInReport',0))
PY
)"
    conflicts_found="$(python3 - <<'PY' "$gen_json_file"
import json,sys
j=json.load(open(sys.argv[1]))
print(j.get('conflictsDetected',0))
PY
)"
  fi

  import_ok=false
  launch_ok=false
  instance_id=""
  import_log="$run_dir/prism-import.log"
  launch_log="$run_dir/prism-launch.log"
  pre_instances="$run_dir/pre-instances.txt"
  post_instances="$run_dir/post-instances.txt"

  if [[ "$gen_ok" == true && -n "$mrpack_path" && -f "$mrpack_path" ]]; then
    export_copy="$EXPORT_DIR/$(basename "$mrpack_path")"
    cp -f "$mrpack_path" "$export_copy"

    sudo -u tristanwaddell bash -lc "find '$PRISM_ROOT/instances' -mindepth 1 -maxdepth 1 -type d -printf '%f\n' | sort" > "$pre_instances" 2>/dev/null || true

    cleanup_processes
    if sudo -u tristanwaddell bash -lc "QT_QPA_PLATFORM=offscreen prismlauncher --dir '$PRISM_ROOT' --import '$export_copy'" >"$import_log" 2>&1; then
      import_ok=true
    else
      import_ok=false
    fi

    sudo -u tristanwaddell bash -lc "find '$PRISM_ROOT/instances' -mindepth 1 -maxdepth 1 -type d -printf '%f\n' | sort" > "$post_instances" 2>/dev/null || true

    instance_id="$(python3 - <<'PY' "$pre_instances" "$post_instances"
import sys
pre=set(open(sys.argv[1]).read().split()) if __import__('os').path.exists(sys.argv[1]) else set()
post=set(open(sys.argv[2]).read().split()) if __import__('os').path.exists(sys.argv[2]) else set()
new=sorted(post-pre)
print(new[-1] if new else '')
PY
)"

    if [[ -z "$instance_id" && -n "$pack_name" ]]; then
      instance_id="$(python3 - <<'PY' "$post_instances" "$pack_name"
import re,sys,os
if not os.path.exists(sys.argv[1]):
    print(''); raise SystemExit
names=open(sys.argv[1]).read().splitlines()
needle=re.sub(r'[^a-z0-9]+','',sys.argv[2].lower())
best=''
for n in names:
    comp=re.sub(r'[^a-z0-9]+','',n.lower())
    if needle and needle in comp:
        best=n
print(best)
PY
)"
    fi

    if [[ -n "$instance_id" ]]; then
      cleanup_processes
      if timeout "$LAUNCH_TIMEOUT_SEC" sudo -u tristanwaddell bash -lc "QT_QPA_PLATFORM=offscreen prismlauncher --dir '$PRISM_ROOT' --launch '$instance_id' --offline PackCrafterBot" >"$launch_log" 2>&1; then
        launch_ok=true
      else
        launch_ok=false
      fi
      cleanup_processes
    else
      echo "No imported instance detected" > "$launch_log"
    fi
  else
    echo "Generation failed; skipping Prism import/launch" > "$import_log"
    echo "Generation failed; skipping Prism import/launch" > "$launch_log"
  fi

  # Gather Prism logs
  mkdir -p "$run_dir/prism-logs"
  copy_if_exists "$PRISM_ROOT/logs" "$run_dir/prism-logs/prismlauncher-logs"
  if [[ -n "$instance_id" ]]; then
    copy_if_exists "$PRISM_ROOT/instances/$instance_id/.minecraft/logs/latest.log" "$run_dir/prism-logs/latest.log"
    copy_if_exists "$PRISM_ROOT/instances/$instance_id/.minecraft/crash-reports" "$run_dir/prism-logs/crash-reports"
  fi

  ended_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  end_epoch="$(date +%s)"
  duration_sec=$((end_epoch - start_epoch))

  status="fail"
  if [[ "$gen_ok" == true && "$import_ok" == true && "$launch_ok" == true ]]; then
    status="pass"
  fi

  python3 - <<'PY' "$RUN_INDEX" "$run_id" "$theme" "$status" "$started_at" "$ended_at" "$duration_sec" "$gen_ok" "$import_ok" "$launch_ok" "$mrpack_path" "$instance_id" "$run_dir" "$used_fallback" "$candidates_found" "$mods_found" "$conflicts_found"
import json,sys
(record_path, run_id, theme, status, started_at, ended_at, duration_sec, gen_ok, import_ok, launch_ok,
 mrpack_path, instance_id, run_dir, used_fallback, candidates_found, mods_found, conflicts_found)=sys.argv[1:]
obj={
  'runId': run_id,
  'theme': theme,
  'status': status,
  'startedAt': started_at,
  'endedAt': ended_at,
  'durationSec': int(duration_sec),
  'generationOk': gen_ok=='true',
  'importOk': import_ok=='true',
  'launchOk': launch_ok=='true',
  'mrpackPath': mrpack_path,
  'instanceId': instance_id,
  'runDir': run_dir,
  'usedFallbackSelection': used_fallback=='true',
  'candidatesFound': int(candidates_found),
  'modsFound': int(mods_found),
  'conflictsFound': int(conflicts_found)
}
with open(record_path,'a',encoding='utf-8') as f:
  f.write(json.dumps(obj)+"\n")
PY

  echo "[$run_id] status=$status gen=$gen_ok import=$import_ok launch=$launch_ok instance=${instance_id:-none}"

done

python3 - <<'PY' "$BATCH_DIR"
import json,glob,os,re,collections,sys
batch_dir=sys.argv[1]
index_path=os.path.join(batch_dir,'run-index.jsonl')
rows=[]
if os.path.exists(index_path):
  with open(index_path,'r',encoding='utf-8') as f:
    for line in f:
      line=line.strip()
      if line:
        rows.append(json.loads(line))

total=len(rows)
passed=sum(1 for r in rows if r.get('status')=='pass')
failed=total-passed

err_counter=collections.Counter()
for r in rows:
  run_dir=r['runDir']
  for fname in ('generation.log','prism-import.log','prism-launch.log'):
    p=os.path.join(run_dir,fname)
    if not os.path.exists(p):
      continue
    txt=open(p,'r',encoding='utf-8',errors='ignore').read()
    patterns=[
      r'No compatible candidates found',
      r'LLM selection failed',
      r'Failed to resolve',
      r'conflict',
      r'QDBusError\([^\n]+\)',
      r'qt\.qpa\.[^\n]+',
      r'Could not connect to display',
      r'No imported instance detected',
      r'timeout',
      r'Exception[^\n]*',
      r'Error:[^\n]*'
    ]
    for pat in patterns:
      for m in re.findall(pat,txt,re.IGNORECASE):
        err_counter[m[:160]]+=1

top=err_counter.most_common(12)

report=os.path.join(batch_dir,'final-report.md')
with open(report,'w',encoding='utf-8') as f:
  f.write('# PackCrafter Prism Batch Report\n\n')
  f.write(f'- Total runs attempted: **{total}**\n')
  f.write(f'- Pass: **{passed}**\n')
  f.write(f'- Fail: **{failed}**\n\n')
  f.write('## Top recurring errors\n\n')
  if top:
    for k,v in top:
      f.write(f'- `{k}` — {v} occurrences\n')
  else:
    f.write('- No recurring error strings detected.\n')
  f.write('\n## Probable root causes\n\n')
  f.write('- Headless VPS constraints (no normal desktop session / D-Bus / display) can prevent reliable Prism import/launch behavior.\n')
  f.write('- Some generated packs include unresolved/pruned dependencies or mod incompatibilities that surface during launch/import.\n')
  f.write('- Theme-driven candidate selection can produce edge-case mod combinations with version/loader mismatches.\n\n')
  f.write('## Concrete fix proposals\n\n')
  f.write('1. Tighten generation constraints: enforce stricter compatibility filters (Minecraft version + loader + known conflict denylist) before final selection.\n')
  f.write('2. Add post-processing validator: parse generated manifest and preflight-check dependency closure against Modrinth before export.\n')
  f.write('3. Add Prism runtime environment guard: detect headless mode and run CLI import/launch through a stable virtual display session (e.g., xvfb-run) with per-run timeout and cleanup.\n\n')
  f.write('## Quick wins\n\n')
  f.write('- Cache and reuse compatibility lookups to reduce transient API/selection failures.\n')
  f.write('- Auto-retry LLM selection once, then fallback deterministically.\n')
  f.write('- Persist structured error categories per run for easier trend analysis.\n\n')
  f.write('## Longer-term fixes\n\n')
  f.write('- Build a historical conflict knowledge base from failed runs and feed it into selection scoring.\n')
  f.write('- Add launch smoke-test automation in CI with known-good fixtures and Prism/Minecraft telemetry parsing.\n')
  f.write('- Introduce a constraint solver step that minimizes conflict risk while preserving theme intent.\n\n')
  f.write('## Run summary\n\n')
  for r in rows:
    f.write(f"- {r['runId']} | {r['theme']} | status={r['status']} | gen={r['generationOk']} import={r['importOk']} launch={r['launchOk']} | instance={r.get('instanceId') or 'n/a'}\n")

print(report)
PY

echo "Batch completed: $BATCH_DIR"
