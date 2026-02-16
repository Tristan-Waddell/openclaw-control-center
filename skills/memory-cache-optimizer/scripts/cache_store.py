#!/usr/bin/env python3
"""Store cache entries (JSONL mode) for memory-cache-optimizer."""

from __future__ import annotations

import argparse
import json
import re
from datetime import datetime, timedelta, timezone
from pathlib import Path

REDACTION_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|token|secret|password|passwd)\s*[:=]\s*[^\s\"']+"),
    re.compile(r"(?i)bearer\s+[a-z0-9._-]+"),
    re.compile(r"AKIA[0-9A-Z]{16}"),
    re.compile(r"(?i)-----BEGIN (RSA|EC|OPENSSH|PGP) PRIVATE KEY-----[\s\S]+?-----END (RSA|EC|OPENSSH|PGP) PRIVATE KEY-----"),
    re.compile(r"xox[baprs]-[A-Za-z0-9-]{10,}"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}"),
]


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def redact_obj(obj):
    text = json.dumps(obj, ensure_ascii=False)
    for pattern in REDACTION_PATTERNS:
        text = pattern.sub("[REDACTED]", text)
    return json.loads(text)


def load_meta(cache_dir: Path) -> dict:
    meta = cache_dir / "meta.json"
    if not meta.exists():
        return {
            "version": "1.0.0",
            "limits": {
                "ttl_days": {"exact": 30, "semantic": 7},
                "capsule_token_budget": 700,
            },
        }
    return json.loads(meta.read_text(encoding="utf-8"))


def append_jsonl(path: Path, row: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(row, ensure_ascii=False) + "\n")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--cache-dir", default="cache")
    ap.add_argument("--id", required=True)
    ap.add_argument("--type", choices=["exact", "semantic"], required=True)
    ap.add_argument("--context-signature", required=True)
    ap.add_argument("--prompt-fingerprint", required=True)
    ap.add_argument("--source-files", required=True, help="JSON array: [{path,mtime,size}]")
    ap.add_argument("--capsule", required=True, help="Path to capsule JSON file")
    ap.add_argument("--result-summary", required=True)
    ap.add_argument("--ttl-days", type=int, default=None)
    ap.add_argument("--embedding", default="", help="Optional JSON array string")
    ap.add_argument("--tokens-avoided-est", type=int, default=0)
    args = ap.parse_args()

    cache_dir = Path(args.cache_dir)
    blobs_dir = cache_dir / "blobs"
    blobs_dir.mkdir(parents=True, exist_ok=True)

    meta = load_meta(cache_dir)
    ttl_default = meta.get("limits", {}).get("ttl_days", {}).get(args.type, 30 if args.type == "exact" else 7)
    ttl_days = args.ttl_days if args.ttl_days is not None else int(ttl_default)

    created = utc_now()
    expires = created + timedelta(days=ttl_days)

    capsule_path = Path(args.capsule)
    capsule_obj = json.loads(capsule_path.read_text(encoding="utf-8"))
    capsule_obj = redact_obj(capsule_obj)

    embedding = None
    if args.embedding.strip():
        try:
            embedding = json.loads(args.embedding)
        except json.JSONDecodeError:
            embedding = None

    source_files = json.loads(args.source_files)

    blob_rel = f"blobs/{args.id}.json"
    blob_abs = cache_dir / blob_rel

    blob_payload = {
        "capsule": capsule_obj,
        "result_summary": args.result_summary,
    }
    blob_abs.write_text(json.dumps(blob_payload, ensure_ascii=False, indent=2), encoding="utf-8")

    size_bytes = blob_abs.stat().st_size
    entry = {
        "id": args.id,
        "type": args.type,
        "created_at": created.isoformat().replace("+00:00", "Z"),
        "expires_at": expires.isoformat().replace("+00:00", "Z"),
        "context_signature": args.context_signature,
        "prompt_fingerprint": args.prompt_fingerprint,
        "embedding": embedding,
        "capsule": capsule_obj,
        "result_summary": args.result_summary,
        "stats": {
            "tokens_avoided_est": max(0, int(args.tokens_avoided_est)),
            "size_bytes": size_bytes,
            "hits": 0,
            "last_accessed_at": None,
        },
        "source_files": source_files,
        "blob_path": blob_rel,
        "skill_version": meta.get("version", "1.0.0"),
    }

    entry = redact_obj(entry)
    append_jsonl(cache_dir / "index.jsonl", entry)
    append_jsonl(
        cache_dir / "metrics.jsonl",
        {
            "ts": created.isoformat().replace("+00:00", "Z"),
            "event": "store",
            "id": args.id,
            "type": args.type,
            "context_signature": args.context_signature,
            "tokens_avoided_est": entry["stats"]["tokens_avoided_est"],
        },
    )

    print(json.dumps({"stored": True, "id": args.id, "blob": str(blob_abs)}, indent=2))


if __name__ == "__main__":
    main()
