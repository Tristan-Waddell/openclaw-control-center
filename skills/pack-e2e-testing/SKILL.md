---
name: pack-e2e-testing
description: Clone a user Git repository, run pack generation, execute end-to-end validation checks, and produce a reproducible test report. Use when a user asks to pull code from git, run a build/pack pipeline, verify output artifacts, and troubleshoot failures step-by-step.
---

# Pack E2E Testing

## Overview

Run a repeatable git-to-artifact E2E workflow for pack generation projects. Collect logs, verify expected output files, and return a pass/fail report with concrete fixes.

## Workflow

1. Confirm inputs from user:
   - repo URL
   - branch/tag/commit
   - setup command(s)
   - pack generation command
   - validation command(s)
   - expected output file(s)
2. Run `scripts/run_pack_e2e.sh` with those inputs.
3. If failures occur, classify as one of:
   - dependency/setup
   - build/pack command
   - artifact mismatch
   - runtime/test assertion
4. Apply minimal fixes in workspace copy, rerun, and report diffs.
5. Return final status with:
   - exact commands used
   - artifact paths/sizes/checksums
   - failing step (if any)
   - next action for user

## Required Inputs Template

Ask user to provide this block before running:

```text
REPO_URL=
REF=
SETUP_CMD=
PACK_CMD=
VERIFY_CMD=
EXPECTED_ARTIFACTS=
```

`EXPECTED_ARTIFACTS` accepts comma-separated relative paths from repo root.

## Run Command

```bash
bash skills/pack-e2e-testing/scripts/run_pack_e2e.sh \
  --repo "$REPO_URL" \
  --ref "$REF" \
  --setup "$SETUP_CMD" \
  --pack "$PACK_CMD" \
  --verify "$VERIFY_CMD" \
  --artifacts "$EXPECTED_ARTIFACTS"
```

## Reporting Format

- `Status:` PASS | FAIL
- `Repo/Ref:`
- `Pack command:`
- `Verify command:`
- `Artifacts found:` path | size | sha256
- `Failure point:` step + last 20 lines
- `Fix applied:` yes/no + short patch summary
- `Next step:`

## References

- Load `references/e2e-checklist.md` for detailed validation checklist.
