# MEMORY.md

## Tristan (user)
- Name: Tristan
- Preferred style: precise, direct, no filler/jargon.
- Timezone preference: America/New_York (DST-aware) for all times.
- Shortcut preference: when Tristan says "Usage", return `/usage cost` output.

## Operational setup
- Telegram pairing active for Tristan (user id 8405076005).
- Gmail SMTP is configured on host for outbound email (secrets in `/root/.openclaw/secrets/gmail_smtp.env`).
- Google Calendar API is configured on host (secrets in `/root/.openclaw/secrets/google_calendar_oauth.env`, calendar `primary`).

## Calendar defaults
- New Google Calendar events should default to 30-minute prior reminder.
- Auto-normalize capitalization/formatting and enrich event details when appropriate.

## Current project context
- PackCrafter repo cloned locally at `/root/.openclaw/workspace/packcrafter-ai` (branch `dev`).
- High-level architecture understood: Modrinth candidate retrieval + scoring, Claude-based slot selection, dependency resolution with repair/pruning, `.mrpack` packaging, and premium-gated generation/download APIs.
