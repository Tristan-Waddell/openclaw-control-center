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

## Quick verification
1. Run same prompt twice with unchanged memory → second run should hit exact cache.
2. Modify `memory/YYYY-MM-DD.md` → next run should miss due to signature change.
3. Use paraphrase prompt (same entities) with embeddings enabled → semantic hit expected.
4. Include `fresh` in prompt → bypass expected.
5. Insert fake token in input → verify cached blob has redacted value.
