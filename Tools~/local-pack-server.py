#!/usr/bin/env python3
"""
local-pack-server.py — BizSim Asset Delivery local HTTP dev server.

Serves the ./packs/ directory on port 8080 with Content-Length and byte-range
support so that LocalHttpAssetDeliveryProvider can perform resumable downloads.

Usage:
    cd Tools~
    python3 local-pack-server.py [--port 8080] [--dir ./packs]

Structure expected under --dir:
    packs/
        my_on_demand_pack.zip
        another_pack.zip

The pack ZIP must match the pack's StablePackId as declared in AssetDeliverySettings.PackDescriptors.

See Documentation~/LOCAL_DEV_WORKFLOW.md for the full 5-step workflow.
"""

import argparse
import os
import sys
from http.server import HTTPServer, BaseHTTPRequestHandler
from pathlib import Path


class PackHandler(BaseHTTPRequestHandler):
    """HTTP request handler with Content-Length and byte-range (Range header) support."""

    serve_dir: Path = Path("packs")

    def do_GET(self):
        # Strip leading slash and sanitize.
        rel = self.path.lstrip("/")
        target = (self.serve_dir / rel).resolve()

        # Path traversal guard.
        try:
            target.relative_to(self.serve_dir.resolve())
        except ValueError:
            self.send_error(403, "Forbidden")
            return

        if not target.exists() or not target.is_file():
            self.send_error(404, f"Pack not found: {rel}")
            return

        file_size = target.stat().st_size
        start, end = 0, file_size - 1
        status = 200

        range_header = self.headers.get("Range")
        if range_header:
            # Parse "bytes=START-END" or "bytes=START-"
            try:
                parts = range_header.replace("bytes=", "").split("-")
                start = int(parts[0]) if parts[0] else 0
                end   = int(parts[1]) if parts[1] else file_size - 1
                end   = min(end, file_size - 1)
                status = 206
            except (ValueError, IndexError):
                self.send_error(416, "Range Not Satisfiable")
                return

        length = end - start + 1

        self.send_response(status)
        self.send_header("Content-Type", "application/zip")
        self.send_header("Content-Length", str(length))
        self.send_header("Accept-Ranges", "bytes")
        if status == 206:
            self.send_header("Content-Range", f"bytes {start}-{end}/{file_size}")
        self.end_headers()

        with open(target, "rb") as f:
            f.seek(start)
            remaining = length
            chunk_size = 64 * 1024  # 64 KB
            while remaining > 0:
                chunk = f.read(min(chunk_size, remaining))
                if not chunk:
                    break
                self.wfile.write(chunk)
                remaining -= len(chunk)

    def log_message(self, fmt, *args):
        sys.stderr.write(f"[local-pack-server] {self.address_string()} — {fmt % args}\n")


def main():
    parser = argparse.ArgumentParser(description="BizSim Asset Delivery local pack server")
    parser.add_argument("--port", type=int, default=8080, help="Port to listen on (default: 8080)")
    parser.add_argument("--dir",  default="packs",        help="Directory containing pack ZIPs")
    args = parser.parse_args()

    serve_dir = Path(args.dir).resolve()
    if not serve_dir.exists():
        serve_dir.mkdir(parents=True, exist_ok=True)
        print(f"[local-pack-server] Created packs directory: {serve_dir}")

    PackHandler.serve_dir = serve_dir

    server = HTTPServer(("0.0.0.0", args.port), PackHandler)
    print(f"[local-pack-server] Serving '{serve_dir}' on port {args.port}")
    print(f"[local-pack-server] ADB reverse command: adb reverse tcp:{args.port} tcp:{args.port}")
    print(f"[local-pack-server] Device URL: http://10.0.2.2:{args.port}/packs/<pack-name>.zip")
    print("[local-pack-server] Ctrl+C to stop.")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n[local-pack-server] Stopped.")


if __name__ == "__main__":
    main()
