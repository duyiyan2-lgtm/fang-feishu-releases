#!/usr/bin/env python3
"""Build a content-addressed, deduplicated archive of Android APK files.

APK files are ZIP containers. This tool splits each APK at ZIP entry boundaries,
stores identical byte ranges only once, and writes a manifest that can reproduce
the original APK byte-for-byte. Large ranges are capped so every stored blob is
well below common Git hosting single-file limits.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import sys
import zipfile
from pathlib import Path


MAX_BLOB_SIZE = 16 * 1024 * 1024
ARCHIVE_FORMAT = "fang-feishu-apk-chunks-v1"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def store_range(
    source,
    offset: int,
    length: int,
    chunks_dir: Path,
    seen: set[str],
) -> tuple[list[dict[str, object]], int]:
    parts: list[dict[str, object]] = []
    stored_bytes = 0
    source.seek(offset)
    remaining = length

    while remaining:
        data = source.read(min(remaining, MAX_BLOB_SIZE))
        if not data:
            raise IOError(f"Unexpected end of file at offset {source.tell()}")

        digest = hashlib.sha256(data).hexdigest().lower()
        relative_path = Path(digest[:2]) / f"{digest}.chunk"
        destination = chunks_dir / relative_path
        if digest not in seen:
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(data)
            seen.add(digest)
            stored_bytes += len(data)

        parts.append(
            {
                "sha256": digest.upper(),
                "size": len(data),
                "path": relative_path.as_posix(),
            }
        )
        remaining -= len(data)

    return parts, stored_bytes


def apk_ranges(path: Path) -> list[tuple[int, int, str]]:
    with zipfile.ZipFile(path, "r") as archive:
        offsets = sorted(
            (entry.header_offset, entry.filename) for entry in archive.infolist()
        )
        central_directory_offset = archive.start_dir

    ranges: list[tuple[int, int, str]] = []
    if not offsets:
        return [(0, path.stat().st_size, "complete file")]

    if offsets[0][0] > 0:
        ranges.append((0, offsets[0][0], "preamble"))

    for index, (offset, name) in enumerate(offsets):
        next_offset = (
            offsets[index + 1][0]
            if index + 1 < len(offsets)
            else central_directory_offset
        )
        if next_offset < offset:
            raise ValueError(f"Invalid ZIP offsets in {path}")
        if next_offset > offset:
            ranges.append((offset, next_offset - offset, name))

    file_size = path.stat().st_size
    if central_directory_offset < file_size:
        ranges.append(
            (
                central_directory_offset,
                file_size - central_directory_offset,
                "central directory and end records",
            )
        )
    return ranges


def build_archive(source_dir: Path, output_dir: Path, clean: bool) -> dict[str, object]:
    apk_files = sorted(source_dir.glob("*.apk"), key=lambda item: item.name.lower())
    if not apk_files:
        raise FileNotFoundError(f"No APK files found in {source_dir}")

    if output_dir.exists() and any(output_dir.iterdir()):
        if not clean:
            raise FileExistsError(
                f"Output directory is not empty: {output_dir}. Use --clean to replace it."
            )
        shutil.rmtree(output_dir)

    chunks_dir = output_dir / "chunks"
    manifests_dir = output_dir / "manifests"
    chunks_dir.mkdir(parents=True, exist_ok=True)
    manifests_dir.mkdir(parents=True, exist_ok=True)

    seen: set[str] = set()
    catalog_entries: list[dict[str, object]] = []
    source_total = 0
    stored_total = 0
    checksums: list[str] = []

    for index, apk_path in enumerate(apk_files, start=1):
        size = apk_path.stat().st_size
        source_total += size
        apk_sha256 = sha256_file(apk_path)
        parts: list[dict[str, object]] = []
        ranges = apk_ranges(apk_path)

        with apk_path.open("rb") as source:
            for offset, length, _description in ranges:
                new_parts, new_bytes = store_range(
                    source, offset, length, chunks_dir, seen
                )
                parts.extend(new_parts)
                stored_total += new_bytes

        manifest = {
            "format": ARCHIVE_FORMAT,
            "fileName": apk_path.name,
            "size": size,
            "sha256": apk_sha256,
            "parts": parts,
        }
        manifest_name = f"{apk_path.name}.json"
        (manifests_dir / manifest_name).write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
        catalog_entries.append(
            {
                "fileName": apk_path.name,
                "size": size,
                "sha256": apk_sha256,
                "manifest": f"manifests/{manifest_name}",
                "partCount": len(parts),
            }
        )
        checksums.append(f"{apk_sha256}  {apk_path.name}")
        print(
            f"[{index:02d}/{len(apk_files):02d}] {apk_path.name}: "
            f"{size / 1024 / 1024:.2f} MiB, {len(parts)} parts",
            flush=True,
        )

    catalog = {
        "format": ARCHIVE_FORMAT,
        "sourceFileCount": len(apk_files),
        "sourceBytes": source_total,
        "storedUniqueBytes": stored_total,
        "deduplicationRatio": round(stored_total / source_total, 6),
        "maxBlobBytes": MAX_BLOB_SIZE,
        "packages": catalog_entries,
    }
    (output_dir / "catalog.json").write_text(
        json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    (output_dir / "SHA256SUMS.txt").write_text(
        "\n".join(checksums) + "\n", encoding="utf-8"
    )
    return catalog


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Directory containing APK files")
    parser.add_argument("output", type=Path, help="Archive output directory")
    parser.add_argument(
        "--clean", action="store_true", help="Replace a non-empty output directory"
    )
    args = parser.parse_args()

    catalog = build_archive(args.source.resolve(), args.output.resolve(), args.clean)
    source_mib = catalog["sourceBytes"] / 1024 / 1024
    stored_mib = catalog["storedUniqueBytes"] / 1024 / 1024
    print(
        f"Completed: {catalog['sourceFileCount']} APKs, "
        f"{source_mib:.2f} MiB source -> {stored_mib:.2f} MiB unique chunks "
        f"({catalog['deduplicationRatio']:.2%})."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        raise SystemExit(1)
