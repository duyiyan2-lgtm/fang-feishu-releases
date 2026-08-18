#!/usr/bin/env python3
"""Split a large delivery artifact into Git-hosting-friendly verified chunks."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from pathlib import Path


CHUNK_SIZE = 16 * 1024 * 1024
ARCHIVE_FORMAT = "fang-feishu-file-chunks-v1"


def build(source: Path, output: Path, output_name: str, clean: bool) -> dict[str, object]:
    if not source.is_file():
        raise FileNotFoundError(source)
    if output.exists() and any(output.iterdir()):
        if not clean:
            raise FileExistsError(f"Output directory is not empty: {output}")
        shutil.rmtree(output)

    chunks_dir = output / "chunks"
    chunks_dir.mkdir(parents=True, exist_ok=True)
    full_hash = hashlib.sha256()
    parts: list[dict[str, object]] = []

    with source.open("rb") as stream:
        index = 1
        while True:
            data = stream.read(CHUNK_SIZE)
            if not data:
                break
            digest = hashlib.sha256(data).hexdigest().upper()
            part_name = f"{index:03d}-{digest.lower()}.part"
            (chunks_dir / part_name).write_bytes(data)
            full_hash.update(data)
            parts.append(
                {
                    "index": index,
                    "path": f"chunks/{part_name}",
                    "size": len(data),
                    "sha256": digest,
                }
            )
            index += 1

    manifest = {
        "format": ARCHIVE_FORMAT,
        "fileName": output_name,
        "size": source.stat().st_size,
        "sha256": full_hash.hexdigest().upper(),
        "chunkSize": CHUNK_SIZE,
        "parts": parts,
    }
    output.mkdir(parents=True, exist_ok=True)
    (output / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    (output / "SHA256SUMS.txt").write_text(
        f"{manifest['sha256']}  {output_name}\n", encoding="utf-8"
    )
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--name", help="Restored file name; defaults to the source name")
    parser.add_argument("--clean", action="store_true")
    args = parser.parse_args()

    manifest = build(
        args.source.resolve(),
        args.output.resolve(),
        args.name or args.source.name,
        args.clean,
    )
    print(
        f"Archived {manifest['fileName']}: {manifest['size']} bytes, "
        f"{len(manifest['parts'])} chunks, SHA-256 {manifest['sha256']}"
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except OSError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        raise SystemExit(1)
