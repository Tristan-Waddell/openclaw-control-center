# PackCrafter Prism Batch Skill

Automates repeated PackCrafter generation + PrismLauncher import/launch validation runs.

## Purpose
Run N themed modpack generations (default 25), import each `.mrpack` into PrismLauncher, attempt a headless launch, and collect logs/errors into a timestamped batch folder.

## Paths
- PackCrafter repo: `/root/.openclaw/workspace/packcrafter-ai`
- Env file: `/root/.openclaw/secrets/packcrafter.env`
- Prism import drop folder: `/home/tristanwaddell/Downloads/PackCrafter-Exports`
- Prism data root: `/home/tristanwaddell/.var/app/org.prismlauncher.PrismLauncher/data/PrismLauncher`

## Usage
```bash
cd /root/.openclaw/workspace/skills/packcrafter-prism-batch
./scripts/run-batch.sh 25
```

Optional env overrides:
- `BATCH_SIZE` (defaults to CLI arg or 25)
- `MC_VERSION` (default `1.20.1`)
- `LOADER` (default `fabric`)
- `IMPORT_TIMEOUT_SEC` (default `120`)
- `LAUNCH_TIMEOUT_SEC` (default `180`)

## Outputs
Creates:
`/root/.openclaw/workspace/packcrafter-ai/output/batch-25/<timestamp>/`
with:
- `run-index.jsonl`
- `run-XX-*/` folders containing generation/import/launch logs
- `final-report.md`

## Notes
- Prism operations run as user `tristanwaddell`.
- Script uses `QT_QPA_PLATFORM=offscreen` for headless launcher calls.
- Sensitive env values are never echoed.
