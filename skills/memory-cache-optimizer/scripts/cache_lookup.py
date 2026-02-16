#!/usr/bin/env python3
"""Lookup cache entries (exact + semantic) for memory-cache-optimizer."""

from __future__ import annotations

import argparse
import json
import math
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

BYPASS_TERMS = ["ignore cache", "fresh", "recompute", "don't reuse", "dont reuse"]


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def normalize_prompt(text: str) -> str:
    text = text.lower().strip()
    text = re.sub(r"\s+", " ", text)
    text = re.sub(r"[^a-z0-9\s:_/-]", "", text)
    return text


def prompt_fingerprint(prompt: str) -> str:
    import hashlib

    return hashlib.sha256(normalize_prompt(prompt).encode()).hexdigest()


def is_bypass(prompt: str) -> bool:
    p = prompt.lower()
    return any(term in p for term in BYPASS_TERMS)


def load_entries(index_path: Path) -> list[dict[str, Any]]:
    if not index_path.exists():
        return []
    rows = []
    for line in index_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            rows.append(json.loads(line))
        except json.JSONDecodeError:
            continue
    return rows


def parse_ts(ts: str) -> float:
    return datetime.fromisoformat(ts.replace("Z", "+00:00")).timestamp()


def not_expired(entry: dict[str, Any], now_ts: float) -> bool:
    exp = entry.get("expires_at")
    if not exp:
        return True
    try:
        return parse_ts(exp) > now_ts
    except Exception:
        return True


def cosine(a: list[float], b: list[float]) -> float:
    if not a or not b or len(a) != len(b):
        return -1.0
    dot = sum(x * y for x, y in zip(a, b))
    na = math.sqrt(sum(x * x for x in a))
    nb = math.sqrt(sum(y * y for y in b))
    if na == 0 or nb == 0:
        return -1.0
    return dot / (na * nb)


def update_hits(index_path: Path, target_id: str) -> None:
    entries = load_entries(index_path)
    now = utc_now().isoformat().replace("+00:00", "Z")
    changed = False
    for e in entries:
        if e.get("id") == target_id:
            stats = e.setdefault("stats", {})
            stats["hits"] = int(stats.get("hits", 0)) + 1
            stats["last_accessed_at"] = now
            changed = True
            break
    if changed:
        lines = [json.dumps(e, ensure_ascii=False) for e in entries]
        index_path.write_text("\n".join(lines) + ("\n" if lines else ""), encoding="utf-8")


def append_metric(cache_dir: Path, event: dict[str, Any]) -> None:
    metric_path = cache_dir / "metrics.jsonl"
    with metric_path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(event, ensure_ascii=False) + "\n")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--cache-dir", default="cache")
    ap.add_argument("--prompt", required=True)
    ap.add_argument("--context-signature", required=True)
    ap.add_argument("--similarity-threshold", type=float, default=0.88)
    ap.add_argument("--query-embedding", default="", help="Optional JSON array")
    ap.add_argument("--cache-report", action="store_true")
    args = ap.parse_args()

    cache_dir = Path(args.cache_dir)
    index_path = cache_dir / "index.jsonl"

    now = utc_now()
    now_ts = now.timestamp()

    if is_bypass(args.prompt):
        out = {
            "hit": False,
            "reason": "bypass",
            "cache_report": {
                "bypass": True,
                "trigger": "bypass phrase",
                "context_signature": args.context_signature,
            },
        }
        append_metric(cache_dir, {"ts": now.isoformat().replace("+00:00", "Z"), "event": "bypass"})
        print(json.dumps(out, indent=2))
        return

    rows = load_entries(index_path)
    pf = prompt_fingerprint(args.prompt)

    # Exact lookup
    exact = None
    for e in rows:
        if not not_expired(e, now_ts):
            continue
        if e.get("type") != "exact":
            continue
        if e.get("prompt_fingerprint") == pf and e.get("context_signature") == args.context_signature:
            exact = e
            break

    if exact:
        update_hits(index_path, exact["id"])
        out = {
            "hit": True,
            "hit_type": "exact",
            "entry": {
                "id": exact.get("id"),
                "capsule": exact.get("capsule"),
                "result_summary": exact.get("result_summary"),
            },
            "cache_report": {
                "key": exact.get("id"),
                "ttl_expires_at": exact.get("expires_at"),
                "context_signature": exact.get("context_signature"),
                "source_files": exact.get("source_files", []),
            },
        }
        append_metric(cache_dir, {"ts": now.isoformat().replace("+00:00", "Z"), "event": "hit", "type": "exact", "id": exact.get("id")})
        print(json.dumps(out, indent=2))
        return

    # Semantic lookup (optional)
    query_vec = None
    if args.query_embedding.strip():
        try:
            query_vec = json.loads(args.query_embedding)
        except json.JSONDecodeError:
            query_vec = None

    best = None
    best_score = -1.0
    if query_vec is not None:
        for e in rows:
            if not not_expired(e, now_ts):
                continue
            if e.get("type") != "semantic":
                continue
            # Guardrail: require compatible context signature
            if e.get("context_signature") != args.context_signature:
                continue
            emb = e.get("embedding")
            if not isinstance(emb, list):
                continue
            score = cosine(query_vec, emb)
            if score > best_score:
                best_score = score
                best = e

    if best is not None and best_score >= args.similarity_threshold:
        update_hits(index_path, best["id"])
        out = {
            "hit": True,
            "hit_type": "semantic",
            "similarity": best_score,
            "entry": {
                "id": best.get("id"),
                "capsule": best.get("capsule"),
                "result_summary": best.get("result_summary"),
            },
            "cache_report": {
                "key": best.get("id"),
                "ttl_expires_at": best.get("expires_at"),
                "context_signature": best.get("context_signature"),
                "source_files": best.get("source_files", []),
            },
        }
        append_metric(cache_dir, {"ts": now.isoformat().replace("+00:00", "Z"), "event": "hit", "type": "semantic", "id": best.get("id"), "similarity": best_score})
        print(json.dumps(out, indent=2))
        return

    out = {
        "hit": False,
        "reason": "miss",
        "cache_report": {
            "bypass": False,
            "context_signature": args.context_signature,
            "checked_entries": len(rows),
        },
    }
    append_metric(cache_dir, {"ts": now.isoformat().replace("+00:00", "Z"), "event": "miss", "checked": len(rows)})
    print(json.dumps(out, indent=2))


if __name__ == "__main__":
    main()
