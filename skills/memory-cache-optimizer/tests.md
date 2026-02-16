# Manual acceptance tests

## 1) Exact cache hit
1. Ensure `cache/index.jsonl` is empty.
2. Run retrieval for prompt `summarize pending infra tasks`.
3. Run same prompt again without changing memory files.
Expected:
- Run 1: miss + write entry.
- Run 2: exact hit.
- Cache report shows same key and valid TTL.

## 2) Cache miss after memory edit
1. Edit today’s `memory/YYYY-MM-DD.md` (append one bullet).
2. Re-run the same prompt.
Expected:
- Context signature changes.
- Cache miss.
- New entry written.

## 3) Semantic hit (if embeddings enabled)
1. Prompt A: `summarize unresolved gateway reliability work for today`.
2. Prompt B: `what gateway stability items are still open today?`.
Expected:
- B gets semantic hit if similarity >= threshold and context signature compatible.
- If embeddings unavailable, fallback to miss/exact-only without error.

## 4) Bypass phrase
1. Use prompt containing `fresh` or `ignore cache`.
Expected:
- Bypass true.
- Recompute performed.
- Cache report reason indicates bypass phrase.

## 5) Redaction behavior
1. Inject fake secret text in source content: `api_key=SECRET123`.
2. Build/store cache entry.
3. Inspect blob JSON.
Expected:
- Secret value is redacted (e.g., `[REDACTED]`).

## 6) GC eviction behavior
1. Set `max_cache_mb` small (e.g., 1 MB) in `cache/meta.json`.
2. Create enough entries to exceed limit.
3. Run `scripts/cache_gc.py --cache-dir cache --max-mb 1`.
Expected:
- Expired entries removed first, then oldest/lowest-value entries.
- Total size <= limit.
- GC summary printed.
