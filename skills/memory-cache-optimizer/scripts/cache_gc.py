#!/usr/bin/env python3
"""Garbage collect cache entries for memory-cache-optimizer (JSONL mode)."""

from __future__ import annotations

import argparse
import json
import os
import time
from pathlib import Path
from typing import Any


def load_entries(index_path: Path) -> list[dict[str, Any]]:
    if not index_path.exists():
        return []
    rows: list[dict[str, Any]] = []
    for line in index_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            rows.append(json.loads(line))
        except json.JSONDecodeError:
            continue
    return rows


def entry_size(entry: dict[str, Any], cache_dir: Path) -> int:
    sz = len(json.dumps(entry, separators=(",", ":")).encode())
    blob = entry.get("blob_path")
    if blob:
      p = cache_dir / blob
      if p.exists():
          sz += p.stat().st_size
    return sz


def is_expired(entry: dict[str, Any], now: float) -> bool:
    exp = entry.get("expires_at")
    if not exp:
        return False
    try:
        # naive parse for ISO8601 with Z
        t = exp.replace("Z", "+00:00")
        from datetime import datetime

        return datetime.fromisoformat(t).timestamp() <= now
    except Exception:
        return False


def total_size(entries: list[dict[str, Any]], cache_dir: Path) -> int:
    return sum(entry_size(e, cache_dir) for e in entries)


def score(entry: dict[str, Any]) -> tuple[int, float]:
    hits = int(entry.get("stats", {}).get("hits", 0))
    last = entry.get("stats", {}).get("last_accessed_at")
    if last:
        try:
            from datetime import datetime
            ts = datetime.fromisoformat(last.replace("Z", "+00:00")).timestamp()
        except Exception:
            ts = 0.0
    else:
        ts = 0.0
    return (hits, ts)


def write_index(index_path: Path, entries: list[dict[str, Any]]) -> None:
    lines = [json.dumps(e, separators=(",", ":")) for e in entries]
    index_path.write_text("\n".join(lines) + ("\n" if lines else ""), encoding="utf-8")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--cache-dir", default="cache")
    ap.add_argument("--max-mb", type=float, default=250)
    args = ap.parse_args()

    cache_dir = Path(args.cache_dir)
    index_path = cache_dir / "index.jsonl"
    entries = load_entries(index_path)
    now = time.time()

    before_count = len(entries)
    before_size = total_size(entries, cache_dir)

    # 1) drop expired first
    kept = [e for e in entries if not is_expired(e, now)]

    # 2) enforce max size: evict lowest score (few hits + oldest access)
    max_bytes = int(args.max_mb * 1024 * 1024)
    while kept and total_size(kept, cache_dir) > max_bytes:
        kept.sort(key=score)  # ascending = least valuable first
        victim = kept.pop(0)
        blob = victim.get("blob_path")
        if blob:
            bp = cache_dir / blob
            if bp.exists():
                try:
                    bp.unlink()
                except OSError:
                    pass

    write_index(index_path, kept)

    after_count = len(kept)
    after_size = total_size(kept, cache_dir)
    print(
        json.dumps(
            {
                "before_count": before_count,
                "after_count": after_count,
                "before_size_bytes": before_size,
                "after_size_bytes": after_size,
                "max_bytes": max_bytes,
                "evicted": before_count - after_count,
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
