# Release Packaging & Signing (MSIX)

This project publishes Windows releases from Git tags using GitHub Actions.

## Trigger model

Workflow: `.github/workflows/release.yml`

Release workflow runs on:

- `push` tags matching `control-center-v*`
- manual `workflow_dispatch`

Examples:

- Stable: `control-center-v0.4.0`
- Beta (prerelease): `control-center-v0.5.0-beta.1`

Tags containing `-beta` are published as GitHub prereleases.

## Build + package

The workflow builds and tests `ControlCenter.sln`, then packages `ControlCenter.UI` as an MSIX using:

- `WindowsPackageType=MSIX`
- `GenerateAppxPackageOnBuild=true`
- `AppxPackageSigningEnabled=false` during package generation
- `AppxBundle=Never`

## Signing behavior (secrets-gated)

Signing executes only when **all** signing secrets are present:

- `WINDOWS_SIGNING_CERT_BASE64`
- `WINDOWS_SIGNING_CERT_PASSWORD`
- `WINDOWS_TIMESTAMP_URL`

When all are configured, the workflow:

1. Decodes `WINDOWS_SIGNING_CERT_BASE64` into a temporary `.pfx`
2. Signs the produced `.msix` with `signtool`
3. Verifies the signature using `control-center/scripts/validate-signed-artifacts.ps1`

When any secret is missing, signing is skipped and the workflow still publishes the unsigned package artifact (draft/validation use).

## Signature verification script

`control-center/scripts/validate-signed-artifacts.ps1` checks Authenticode signature status:

```powershell
./scripts/validate-signed-artifacts.ps1 -Path <path-to-msix>
```

The script fails if the signature status is not `Valid`.

## Required repository setup

In GitHub repository settings for `Tristan-Waddell/openclaw-control-center`, configure Actions secrets:

- `WINDOWS_SIGNING_CERT_BASE64` (base64-encoded PFX bytes)
- `WINDOWS_SIGNING_CERT_PASSWORD`
- `WINDOWS_TIMESTAMP_URL` (RFC3161 timestamp endpoint)

Recommended: protect tag creation and release permissions so only maintainers can publish release tags.
