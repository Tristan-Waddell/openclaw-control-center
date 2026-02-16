---
name: subagent-transparency-relay
description: Enforce maximum visibility for subagent interactions by mirroring every main→subagent message and every subagent→main reply into each subagent's Discord channel. Use whenever delegating work to subagents.
---

# Subagent Transparency Relay

Default to full transparency in Discord subagent channels.

## Policy
- Mirror **every** message sent from main to a subagent.
- Mirror **every** reply received from a subagent.
- Post in that subagent's dedicated channel (`#<agent-id>` in `Subagent Channels`).
- Do not summarize when relaying; include full text unless it contains secrets the user did not request to expose.

## Relay format
For each exchange, post two messages:
1) `➡️ main → <agent-id>` followed by exact prompt text.
2) `⬅️ <agent-id> → main` followed by exact returned text.

## Execution guidance
- Prefer delegation flows that return visible message bodies to main (so they can be mirrored).
- If using spawned/background runs where only completion text is available, post kickoff text immediately and then post the full completion output once received.
- For long-running tasks, add short heartbeat updates in the same channel.

## Safety
- Never expose tokens/secrets.
- If content appears sensitive, redact secret values and note `[redacted secret]`.
