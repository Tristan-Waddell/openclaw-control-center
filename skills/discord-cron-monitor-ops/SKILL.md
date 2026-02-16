---
name: discord-cron-monitor-ops
description: Maintain a Discord cron-monitor channel with a pinned board of current cron jobs and handle channel commands to pause, resume, deactivate, run, and refresh jobs. Use when users ask to monitor cron status in Discord and manage jobs from that channel.
---

# Discord Cron Monitor Ops

Keep cron operations visible and controllable from Discord.

## Channel setup
1. Use `message` with `channel-list` to locate category `Monitoring Channels`.
2. Create or reuse text channel `cron-monitor` inside that category.
3. Create one board message and pin it.

## Board message requirements
- Title: `Cron Monitor Board`.
- Show all jobs from `cron list`.
- Include for each job:
  - name
  - id
  - enabled status
  - schedule (`cron` expression / interval / one-shot time)
  - last status if available
- Include command help:
  - `cron pause <job-id>`
  - `cron resume <job-id>`
  - `cron deactivate <job-id>`
  - `cron run <job-id>`
  - `cron refresh`

## Update loop
- Refresh the pinned board on meaningful cron changes and on periodic maintenance.
- Preferred periodic cadence: every 5 minutes using a cron job with `payload.kind=systemEvent` and `sessionTarget=main`.
- If the pinned message disappears, post a new board and pin it.

## Command handling in channel
When a user posts one of these commands in `cron-monitor`:
- `cron pause <id>` → `cron update` with `enabled:false`
- `cron resume <id>` → `cron update` with `enabled:true`
- `cron deactivate <id>` → same as pause (`enabled:false`)
- `cron run <id>` → `cron run`
- `cron refresh` → regenerate board immediately

After each command:
1. Confirm action in-channel (short acknowledgment).
2. Refresh pinned board.

## Safety
- Never remove jobs unless the user explicitly asks `remove`.
- If id is ambiguous or missing, ask for exact `job-id`.
- Keep replies concise and operational.
