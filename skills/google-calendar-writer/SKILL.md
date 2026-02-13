---
name: google-calendar-writer
description: Create Google Calendar events directly via Calendar API using stored OAuth refresh-token secrets. Use when the user asks to add, edit, or schedule calendar events and you need a deterministic local script-based path.
---

# Google Calendar Writer

Use the bundled script to create events without external Python packages.

## Secrets source

Read OAuth secrets from `/root/.openclaw/secrets/google_calendar_oauth.env`:
- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `GOOGLE_REFRESH_TOKEN`
- `GOOGLE_CALENDAR_ID` (fallback `primary`)

## Create an event

Run:

```bash
python3 /root/.openclaw/workspace/skills/google-calendar-writer/scripts/create_event.py \
  --title "Tee time (9 holes)" \
  --start "2026-02-14T13:30:00-05:00" \
  --end "2026-02-14T16:00:00-05:00" \
  --tz "America/New_York" \
  --location "" \
  --description "" \
  --reminder-minutes 30
```

## Notes

- Always use Tristan local time (`America/New_York`) unless explicitly overridden.
- Default reminder should be 30 minutes before.
- Return the created event link (`htmlLink`) and id in the user update.
