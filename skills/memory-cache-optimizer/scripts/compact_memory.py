#!/usr/bin/env python3
"""Write compact, high-signal updates into workspace memory markdown files."""

from __future__ import annotations

import argparse
import re
from datetime import datetime, timezone
from pathlib import Path

REDACTION_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|token|secret|password|passwd)\s*[:=]\s*[^\s\"']+"),
    re.compile(r"(?i)bearer\s+[a-z0-9._-]+"),
    re.compile(r"AKIA[0-9A-Z]{16}"),
    re.compile(r"xox[baprs]-[A-Za-z0-9-]{10,}"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}"),
]


def redact(text: str) -> str:
    out = text
    for p in REDACTION_PATTERNS:
        out = p.sub("[REDACTED]", out)
    return out


def compact_lines(lines: list[str], max_items: int) -> list[str]:
    items: list[str] = []
    for line in lines:
        line = line.strip()
        if not line:
            continue
        if line.startswith("- "):
            items.append(line)
        elif len(line) < 160:
            items.append(f"- {line}")
    # dedupe preserving order
    seen = set()
    out = []
    for item in items:
        if item in seen:
            continue
        seen.add(item)
        out.append(item)
        if len(out) >= max_items:
            break
    return out


def append_compaction(target: Path, bullets: list[str], source: str) -> None:
    ts = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
    block = [
        "\n## Compacted update",
        f"- Source: {source}",
        f"- At: {ts}",
        *bullets,
        "",
    ]
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("a", encoding="utf-8") as f:
        f.write("\n".join(block))


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--source", required=True, help="Source markdown file to compact")
    ap.add_argument("--target", required=True, help="Target memory markdown file to append")
    ap.add_argument("--max-items", type=int, default=8)
    args = ap.parse_args()

    src = Path(args.source)
    tgt = Path(args.target)
    if not src.exists():
        raise SystemExit(f"Source file not found: {src}")

    raw = src.read_text(encoding="utf-8")
    raw = redact(raw)
    bullets = compact_lines(raw.splitlines(), max_items=args.max_items)
    append_compaction(tgt, bullets, str(src))

    print(f"Wrote {len(bullets)} compact bullets to {tgt}")


if __name__ == "__main__":
    main()
