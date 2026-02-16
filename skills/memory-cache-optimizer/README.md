# memory-cache-optimizer

Cache-aware memory retrieval + context compaction for OpenClaw workspace memory.

## Why this exists
OpenClaw memory is file-based (`MEMORY.md` + `memory/*.md`). Re-reading large logs for similar prompts can waste tokens. This skill stores compact, reusable context capsules and reuses them when safe.

## How cache works
- **Exact cache**: prompt fingerprint + context signature (file path/mtime/size hash).
- **Semantic cache** (optional): paraphrase matching via embeddings + cosine threshold + compatible context signature.
- **Context Capsule**: high-signal summary with strict sections and tight size budget.

## Storage
- `cache/index.jsonl` (default)
- `cache/index.sqlite` (optional)
- `cache/blobs/<id>.json`
- `cache/meta.json`
- `cache/metrics.jsonl` (optional)

## Invalidation
Cache entry is invalid when:
1. Any referenced memory file changed.
2. TTL expired (default exact 30d, semantic 7d).
3. Skill version changed.
4. Prompt requests bypass (`ignore cache`, `fresh`, `recompute`, `don’t reuse`).

## Safety
- Never cache secrets; redact with regex list in `SKILL.md`.
- Store minimum necessary snippets only.
- Provide cache-report metadata without leaking payloads.
- Do not run remote scripts or opaque shell commands.

> Warning: Skills should be reviewed like code before production use.

## Scripts
- `scripts/cache_key.py`: computes normalized prompt fingerprint + context signature + exact key.
- `scripts/cache_lookup.py`: exact/semantic lookup with bypass support + cache report + metrics append.
- `scripts/cache_store.py`: stores redacted entry/blob + metrics append.
- `scripts/cache_gc.py`: evicts expired/low-value entries to stay within size limits.
- `scripts/compact_memory.py`: writes compact, high-signal memory updates.

## Quick verification
1. Compute key/signature with `cache_key.py`.
2. Store one entry with `cache_store.py`.
3. Run same prompt via `cache_lookup.py` with same context signature → exact hit.
4. Modify `memory/YYYY-MM-DD.md` and recompute signature → miss.
5. Use paraphrase + embedding (if available) with same signature → semantic hit.
6. Include `fresh` in prompt → bypass.
7. Insert fake token in capsule JSON and store; inspect blob for redaction.
