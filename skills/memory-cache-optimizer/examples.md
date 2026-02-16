# Good / Bad examples

## Good: exact cache reuse
- Prompt: `Summarize open loops from today and yesterday memory logs.`
- Context unchanged, cache hit returns capsule only.
Why good: avoids full reread/restuff, preserves quality.

## Good: semantic reuse with guardrails
- Prompt A: `recap unresolved packcrafter items`
- Prompt B: `what’s still pending for packcrafter?`
- Same context signature, high similarity.
Why good: reuses prior retrieval safely.

## Good: forced fresh recompute
- Prompt: `fresh recompute: summarize today memory and ignore cache`
Why good: explicit user intent overrides cache.

## Bad: reusing semantic result across changed context
- Prompt similar, but today’s memory file changed significantly.
Why bad: stale reuse risk.
Fix: require compatible context signature.

## Bad: storing raw secrets in capsule/snippets
- Source includes tokens/passwords and cache stores them unredacted.
Why bad: security leak.
Fix: apply redaction regex before persistence.

## Bad: oversized snippets
- Capsule includes long transcript chunks not essential to answer.
Why bad: token bloat returns.
Fix: hard-limit relevant snippets to only critical lines.
