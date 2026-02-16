---
name: morning-weather-motivation
description: Send a single daily morning update with current weather + today forecast for Charlotte, NC and a short motivational line. Use when a scheduled 8:00 AM reminder asks for weather and motivation, and avoid duplicate sends.
---

# Morning Weather + Motivation

1. Get current weather and today's forecast for **Charlotte, NC** using the weather capability.
2. Before sending, check the target channel for a message already posted today by the bot with `Charlotte` + weather markers.
3. If today’s message already exists, stop (no second send).
4. Send exactly **one** concise user-facing message.
5. Include:
   - `Charlotte, NC` label,
   - current conditions (temp + feels-like + brief condition),
   - today high/low and rain expectation,
   - one short motivational line.
6. For motivation freshness:
   - Read `references/motivation-lines.md`.
   - Avoid repeating any line used in the last 7 days (use recent daily messages as memory of used lines).
   - If unsure, pick a different line than the most recent 3.
7. Keep tone upbeat and precise.
8. If weather fetch is unavailable, still send one fallback message (mention data unavailable) plus a fresh motivational line.

## Anti-duplicate rule

- If the trigger is a scheduled reminder/system event, produce one final user message only.
- Never send a follow-up that repeats the same daily update.
- Idempotency check (already posted today) takes priority over re-sending.
