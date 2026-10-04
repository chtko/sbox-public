#!/usr/bin/env python3
"""
gen_manifest.py — генерирует manifest.json для GitHub-релиза.

Использование (в build.yml):
    python gen_manifest.py <game_dir> <version> > manifest.json

Пример в GitHub Actions:
    - name: Generate manifest
      run: python gen_manifest.py ./dist ${{ steps.tag_sync.outputs.tag }} > manifest.json
"""

import hashlib
import json
import os
import sys


def sha256(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def main():
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} <game_dir> <version>", file=sys.stderr)
        sys.exit(1)

    root    = os.path.abspath(sys.argv[1])
    version = sys.argv[2]

    if not os.path.isdir(root):
        print(f"Error: '{root}' is not a directory", file=sys.stderr)
        sys.exit(1)

    files = {}
    for dirpath, _, filenames in os.walk(root):
        for fn in filenames:
            fp  = os.path.join(dirpath, fn)
            rel = os.path.relpath(fp, root).replace("\\", "/")

            # Пропускаем служебные файлы лаунчера
            if rel in (".sbox_version", "manifest.json"):
                continue

            files[rel] = {
                "size":   os.path.getsize(fp),
                "sha256": sha256(fp),
            }

    manifest = {"version": version, "files": files}
    print(json.dumps(manifest, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()