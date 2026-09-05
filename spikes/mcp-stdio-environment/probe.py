#!/usr/bin/env python3
"""Spike probe: record what environment and cwd a stdio MCP server is launched with.

Deliberately NOT a working MCP server. The question is what the launcher hands the child process,
and that is answered the moment the process starts — the handshake is irrelevant to it. Failing the
handshake is the cheapest possible probe and costs the launcher one logged error.
"""
import json
import os
import sys

out = os.environ.get("MCP_PROBE_OUT", "")
if out:
    with open(out, "w", encoding="utf-8") as f:
        json.dump(
            {
                "cwd": os.getcwd(),
                "saw_AIDE_SESSION": os.environ.get("AIDE_SESSION"),
                "saw_AIDE_CONTRACT_LOG": os.environ.get("AIDE_CONTRACT_LOG"),
                "saw_AIDE_WORKSPACE": os.environ.get("AIDE_WORKSPACE"),
                "saw_SPIKE_MARKER": os.environ.get("SPIKE_MARKER"),
                "env_count": len(os.environ),
                # A few names the parent certainly has, to tell "inherited nothing" from
                # "inherited a curated subset".
                "saw_PATH": bool(os.environ.get("PATH")),
                "saw_USERPROFILE": bool(os.environ.get("USERPROFILE")),
            },
            f,
            indent=2,
        )

sys.exit(0)
