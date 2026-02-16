---
name: skill-creator-monitor-sync
description: Automatically sync the Discord skills monitor board after any skill is created, renamed, updated, enabled, or disabled. Use whenever doing skill-creator work so #skills-monitor stays current without waiting for periodic refresh.
---

# Skill Creator Monitor Sync

After any skill creation/update operation, immediately refresh the skills monitor board.

## Trigger events
Run sync after any of these:
- new skill folder created
- SKILL.md updated
- skill renamed
- skill disabled/enabled in `.skills-disabled.txt`
- skill removed

## Required post-update actions
1. Rebuild the skills list from `/root/.openclaw/workspace/skills`.
2. Apply hidden/disabled rules from `/root/.openclaw/workspace/.skills-disabled.txt`.
3. Keep built-ins hidden line: `Built-in skills: hidden`.
4. Edit pinned board message in `#skills-monitor` (`channelId=1473093215707070535`, `messageId=1473093253363662911`) immediately.
5. If pinned message is missing, post a new board and pin it.

## Format requirements
- One skill per line.
- One ultra-brief sub-line for what it does.
- Keep command help block at bottom.

## Safety
- Do not expose hidden built-in skill names in the board.
- Do not delete skills unless explicitly requested.
