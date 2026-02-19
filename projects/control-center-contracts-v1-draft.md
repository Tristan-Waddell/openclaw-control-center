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

## Notes
- All mutation endpoints must include/return `correlationId`.
- Timestamps are ISO-8601 UTC.
- This draft will be tightened before implementation in `Contracts` module.
