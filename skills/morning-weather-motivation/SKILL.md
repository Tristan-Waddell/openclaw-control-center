---
name: morning-weather-motivation
description: Send a single daily morning update with current weather + today forecast for Charlotte, NC and a short motivational line. Use when a scheduled 8:00 AM reminder asks for weather and motivation, and avoid duplicate sends.
---

# Morning Weather + Motivation

1. Get current weather and today's forecast for **Charlotte, NC**.
2. Send exactly **one** concise user-facing message.
3. Include:
   - current conditions (temp + feels-like + brief condition),
   - today high/low and rain expectation,
   - one short motivational line.
4. Keep tone upbeat and precise.
5. If weather fetch is unavailable, send one fallback message with a brief apology and motivational line.

## Anti-duplicate rule

- If the trigger is a scheduled reminder/system event, produce one final user message only.
- Do not send a second follow-up that repeats the same update.
