# E2E Pack Validation Checklist

## Preflight
- Repo reachable
- Correct ref exists
- Required runtimes present (node/python/java/etc)
- Secrets/env vars available (if required)

## Build + Pack
- Setup command exits 0
- Pack command exits 0
- Logs captured to file

## Artifact Checks
- Every expected artifact exists
- Artifact size > 0
- sha256 generated for each artifact

## Functional Validation
- Verify command exits 0
- Smoke test on generated pack succeeds

## Failure Triage
1. Setup fails: dependency/toolchain missing
2. Pack fails: config or source issue
3. Artifacts missing: output path mismatch
4. Verify fails: functional regression or bad fixture

## Output Contract
- PASS/FAIL
- Exact command transcript
- Artifact table
- Recommended next action
