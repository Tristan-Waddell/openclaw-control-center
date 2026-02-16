#!/usr/bin/env python3
"""Stable key hashing for memory-cache-optimizer."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from typing import Iterable


def normalize_prompt(text: str) -> str:
    text = text.lower().strip()
    text = re.sub(r"\s+", " ", text)
    text = re.sub(r"[^a-z0-9\s:_/-]", "", text)
    return text


def context_signature(paths: Iterable[Path], slot: str = "") -> str:
    records = []
    for p in sorted({Path(x) for x in paths}):
        if not p.exists() or not p.is_file():
            continue
        st = p.stat()
        records.append({"path": str(p), "mtime": st.st_mtime, "size": st.st_size})
    payload = {"slot": slot or "", "files": records}
    raw = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()
    return hashlib.sha256(raw).hexdigest()


def exact_key(prompt: str, ctx_sig: str, slot: str = "") -> str:
    norm = normalize_prompt(prompt)
    raw = f"{norm}||{ctx_sig}||{slot}".encode()
    return hashlib.sha256(raw).hexdigest()


def prompt_fingerprint(prompt: str) -> str:
    return hashlib.sha256(normalize_prompt(prompt).encode()).hexdigest()


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--prompt", required=True)
    ap.add_argument("--slot", default="")
    ap.add_argument("--paths", nargs="*", default=[])
    args = ap.parse_args()

    paths = [Path(p) for p in args.paths]
    ctx_sig = context_signature(paths, args.slot)
    out = {
        "normalized_prompt": normalize_prompt(args.prompt),
        "prompt_fingerprint": prompt_fingerprint(args.prompt),
        "context_signature": ctx_sig,
        "exact_key": exact_key(args.prompt, ctx_sig, args.slot),
    }
    print(json.dumps(out, indent=2))


if __name__ == "__main__":
    main()
