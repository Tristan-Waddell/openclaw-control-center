---
name: subagent-channel-provisioning
description: Create and maintain Discord visibility channels for subagents. Use when a user asks to add/create subagents and wants each subagent to have a dedicated Discord channel for transparent work logs, status updates, and outputs.
---

# Subagent Channel Provisioning

Always keep subagent execution visible in Discord by creating one channel per active subagent and posting work traces there.

## Workflow

1. Discover active agents with `agents_list`.
2. List Discord channels with `message` action `channel-list`.
3. Find the target category:
   - Prefer category named `Subagent Channels`.
   - If not found, ask the user where to place channels (or create the category only if explicitly requested).
4. For each configured non-`main` agent id:
   - Channel name must be the raw agent id (no `subagent-` prefix), lowercased.
   - If channel already exists, reuse it.
   - If missing, create it with `message` action `channel-create`, type `0` (text), and set `parentId` to the category id.
   - Set a short topic describing the channel as that subagent’s workspace/log stream.
5. If creation fails due to permissions, report exactly what permission is missing (`Manage Channels`) and stop destructive retries.

## Visibility rules

When using subagents for work:

- Post a kickoff note in that subagent’s channel (task + expected output).
- Post completion summary in that same channel with links/artifacts.
- Keep updates concise and operational; avoid noisy chatter.

## Naming + mapping

- `communications` → `#communications`
- `fairseed-rankings` → `#fairseed-rankings`
- `packcrafter-ai` → `#packcrafter-ai`
- Any new subagent id should map directly to `#<agent-id>`.

## Safety

- Do not delete channels unless explicitly asked.
- Do not rename existing channels unless explicitly asked.
- Ask before creating external-facing announcements outside the designated subagent channels.
