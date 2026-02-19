# Control Center Contracts v1 (Draft)

## Scope
Initial DTO/contract draft for these domains:
- Agents
- Config
- Cron Jobs
- Skills
- Tasks + Usage
- Projects

---

## 1) Agents
```json
{
  "id": "string",
  "name": "string",
  "model": "string",
  "status": "online|offline|degraded",
  "heartbeat": {
    "enabled": true,
    "every": "string",
    "lastRunAt": "ISO-8601|null"
  },
  "activeSessions": 0
}
```

## 2) Config
```json
{
  "hash": "string",
  "lastTouchedAt": "ISO-8601",
  "valid": true,
  "warnings": [],
  "issues": []
}
```

## 3) Cron Job
```json
{
  "id": "string",
  "name": "string",
  "enabled": true,
  "schedule": {
    "kind": "cron|every|at",
    "expr": "string|null",
    "tz": "string|null",
    "everyMs": "number|null",
    "at": "ISO-8601|null"
  },
  "lastStatus": "ok|error|never",
  "lastRunAt": "ISO-8601|null",
  "nextRunAt": "ISO-8601|null"
}
```

## 4) Skills
```json
{
  "id": "string",
  "name": "string",
  "enabled": true,
  "source": "workspace|bundled|installed",
  "path": "string",
  "description": "string"
}
```

## 5) Tasks + Usage
```json
{
  "activeTasks": [
    {
      "runId": "string",
      "agentId": "string",
      "label": "string",
      "status": "running|done|error",
      "startedAt": "ISO-8601",
      "endedAt": "ISO-8601|null"
    }
  ],
  "usage": {
    "inputTokens": 0,
    "outputTokens": 0,
    "cacheReadTokens": 0,
    "estimatedCost": 0
  }
}
```

## 6) Projects
```json
{
  "id": "string",
  "name": "string",
  "path": "string",
  "branch": "string",
  "latestCommit": "string",
  "status": "active|paused|blocked",
  "summary": "string"
}
```

---

## 7) Realtime Event Envelope + Versioning Rules

### Event envelope (v1)
```json
{
  "eventId": "uuid",
  "type": "string",
  "version": 1,
  "occurredAt": "ISO-8601",
  "correlationId": "string|null",
  "source": {
    "service": "gateway|agent|cron|skill|system",
    "id": "string"
  },
  "payload": {}
}
```

### Initial event types (v1)
- `agent.status.changed`
- `agent.heartbeat.ran`
- `cron.job.created`
- `cron.job.updated`
- `cron.job.deleted`
- `cron.job.run.started`
- `cron.job.run.finished`
- `skill.state.changed`
- `session.task.started`
- `session.task.finished`
- `usage.updated`
- `project.state.changed`

### Versioning rules
- `version` is envelope schema version, not product version.
- Additive payload fields are allowed in v1 and must be ignored safely by older clients.
- Removing/renaming required fields requires v2.
- Event `type` names are stable once published.
- Consumers must default unknown event types to no-op + debug log.

---

## 8) Standard Error Model (v1)
```json
{
  "code": "string",
  "message": "string",
  "details": {},
  "retryable": false,
  "correlationId": "string",
  "httpStatus": 0
}
```

### Error model rules
- `code` is machine-readable and stable.
- `message` is human-readable and safe to display.
- `details` may be omitted for security-sensitive errors.
- `retryable=true` means client may apply retry policy.
- `correlationId` is required for all mutating failures and recommended for all errors.

---

## 9) Contract Compatibility Policy (v1)
- Backward compatibility is required for all minor/patch updates.
- Allowed in v1:
  - add optional fields
  - add new event types
  - add new optional enum values (clients must tolerate unknown)
- Not allowed in v1:
  - remove required fields
  - change existing field types
  - repurpose existing `code` semantics
- Breaking changes require:
  - new contract version (v2)
  - compatibility note and migration mapping
  - temporary dual-read support where practical

---

## Notes
- All mutation endpoints must include/return `correlationId`.
- Timestamps are ISO-8601 UTC.
- This draft will be tightened before implementation in `Contracts` module.
