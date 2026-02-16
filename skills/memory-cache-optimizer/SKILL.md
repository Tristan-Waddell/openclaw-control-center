---
name: memory-cache-optimizer
version: 1.0.0
description: Cache-aware memory retrieval and context compaction for OpenClaw workspace memory files. Use when requests repeatedly read memory logs/MEMORY.md, when prompts are similar to recent work, or when large context ingestion would increase token usage.
environment:
  python: ">=3.9"
  sqlite: optional
permissions:
  filesystem:
    read:
      - workspace/**
    write:
      - workspace/skills/memory-cache-optimizer/cache/**
      - workspace/memory/**
      - workspace/MEMORY.md
config:
  ttl_days:
    exact: 30
    semantic: 7
  max_cache_mb: 250
  similarity_threshold: 0.88
  capsule_token_budget: 700
---

# memory-cache-optimizer

Use this skill to reduce token usage by caching compact context capsules derived from workspace memory files.

## Inputs
- user prompt
- optional intent string (short phrase)
- workspace memory paths (`MEMORY.md`, `memory/YYYY-MM-DD.md`, optional notes)
- optional `memory_plugin_slot` string (if detectable)
- optional embeddings provider (assumption; fallback included)

## Outputs
- `Context Capsule` with sections:
  1) Stable facts
  2) Recent changes
  3) Open loops / pending tasks
  4) Relevant snippets (only essential)
- Cache decision notes (hit/miss/bypass + why)
- Optional compact memory write-back if memory is duplicated/noisy

## Trigger conditions
Trigger when any apply:
- Large memory/context read is about to happen.
- User request is similar to a recent request (same intent/entities).
- Repeated retrieval over workspace memory is likely.

## Cache files
- `cache/index.jsonl` (default index)
- `cache/index.sqlite` (optional alternative)
- `cache/blobs/<id>.json` (larger payloads)
- `cache/meta.json` (version + limits)
- `cache/metrics.jsonl` (optional hit/miss telemetry)

## Required safety rules
- Never run opaque shell commands.
- Never download or execute remote scripts.
- Redact secrets before caching.
- Never persist unnecessary personal data; keep snippets minimal.
- Support `Cache Report` mode: show key, hit/miss, TTL, source files, no sensitive payload.

Redaction regex list (apply before write):
- `(?i)(api[_-]?key|token|secret|password|passwd)\\s*[:=]\\s*[^\\s"']+`
- `(?i)bearer\\s+[a-z0-9._-]+`
- `AKIA[0-9A-Z]{16}`
- `(?i)-----BEGIN (RSA|EC|OPENSSH|PGP) PRIVATE KEY-----[\\s\\S]+?-----END (RSA|EC|OPENSSH|PGP) PRIVATE KEY-----`
- `xox[baprs]-[A-Za-z0-9-]{10,}`
- `gh[pousr]_[A-Za-z0-9]{20,}`

## Procedure
1. Detect cache bypass.
   - If prompt contains: `ignore cache`, `fresh`, `recompute`, `don't reuse` → bypass and recompute.
2. Compute context signature.
   - Hash of relevant memory files: `path + mtime + size`.
   - Include `memory_plugin_slot` if available.
3. Compute prompt fingerprint.
   - Normalize prompt (lowercase, collapse whitespace, strip punctuation noise).
4. Attempt exact cache lookup.
   - Key: hash(normalized_prompt + context_signature + slot).
5. Attempt semantic lookup.
   - If embeddings available, compare prompt vector cosine similarity against cached vectors.
   - Reuse only when similarity >= threshold AND context signatures are compatible.
   - If no embeddings, skip semantic path.
6. On hit:
   - Return cached Context Capsule and short usage notes.
   - Do not reread full memory files.
   - Emit Cache Report.
7. On miss:
   - Read minimum memory set (today + yesterday + `MEMORY.md` only if needed).
   - Build Context Capsule (respect `capsule_token_budget`).
   - Store cache entry + blob.
   - If memory is duplicated/noisy, run `scripts/compact_memory.py` and append concise update.
8. Run GC when needed.
   - If cache exceeds `max_cache_mb` or has expired rows, run `scripts/cache_gc.py`.

## Tool usage guidance
- Use filesystem read/write only.
- Prefer deterministic local scripts in `scripts/`.
- If sqlite unavailable, use JSONL mode without failing.

## Assumptions and fallback
Assumption: An embedding provider may exist in the host runtime.
Fallback: exact-match caching only (fully supported) when embeddings are unavailable.
