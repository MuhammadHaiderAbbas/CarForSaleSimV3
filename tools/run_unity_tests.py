#!/usr/bin/env python3
"""
Runs the Car Sales edit-mode test suite headlessly (Unity Test Framework) and prints a summary.

    python3 tools/run_unity_tests.py                 # edit-mode tests (default)
    python3 tools/run_unity_tests.py --playmode      # play-mode tests instead
    UNITY=/path/to/Unity python3 tools/run_unity_tests.py

Which Unity? - the UNITY environment variable, or the newest editor found in the
standard Unity Hub install folders (macOS / Windows / Linux). The project must already
have its packages installed (Tools → Car Sales → 1. Check & Install Packages) — e.g. run
Unity on the project once before invoking this.

Exit code 0 = all tests passed (or were skipped), 1 = failures/errors, 2 = Unity not found.
Test results XML is written to build/TestResults-editmode.xml (--results overrides).
"""
import argparse
import glob
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def find_unity():
    env = os.environ.get("UNITY", "").strip()
    if env:
        return env if os.path.exists(env) else None
    candidates = []
    patterns = [
        "/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity",   # macOS
        os.path.expandvars(r"C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe"),  # Windows
        os.path.expanduser("~/Unity/Hub/Editor/*/Editor/Unity"),             # Linux
        os.path.expanduser("~/.local/share/unity3d/Hub/Editor/*/Editor/Unity"),
    ]
    for pat in patterns:
        candidates.extend(glob.glob(pat))

    def version_key(path):
        m = re.search(r"Editor[/\\](\d+(?:\.\d+)*[abf]\d+)", path)
        return [int(p) if p.isdigit() else 0 for p in re.split(r"[.-]", m.group(1))] if m else [0]

    candidates.sort(key=version_key, reverse=True)
    return candidates[0] if candidates else None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--playmode", action="store_true", help="run play-mode tests instead of edit-mode")
    ap.add_argument("--results", default=None, help="where to write the NUnit XML results")
    args = ap.parse_args()

    unity = find_unity()
    if not unity:
        print("Unity editor not found. Set it explicitly, e.g.\n"
              "    UNITY=/Applications/Unity/Hub/Editor/6000.0.23f1/Unity.app/Contents/MacOS/Unity python3 tools/run_unity_tests.py",
              file=sys.stderr)
        return 2

    platform = "PlayMode" if args.playmode else "EditMode"
    results = args.results or os.path.join(PROJECT, "build", f"TestResults-{platform.lower()}.xml")
    os.makedirs(os.path.dirname(results), exist_ok=True)

    cmd = [unity, "-batchmode", "-nographics", "-projectPath", PROJECT,
           "-runTests", "-testPlatform", platform,
           "-testResults", results, "-quit"]
    print(f"[run_unity_tests] {platform} tests via {unity}")
    proc = subprocess.run(cmd)

    if not os.path.exists(results):
        print(f"[run_unity_tests] no results file at {results} (Unity exited with {proc.returncode}). See the editor log for package/compile errors.",
              file=sys.stderr)
        return 1

    root = ET.parse(results).getroot()
    nodes = list(root.iter("test-suite")) or [root]
    total = failed = inconclusive = skipped = 0
    failures = []
    for case in root.iter("test-case"):
        total += 1
        result = case.get("result", "")
        if result == "Failed":
            failed += 1
            msg = case.find("failure")
            text = (msg.findtext("message") or "")[:300] if msg is not None else ""
            failures.append((case.get("fullname", case.get("name")), text))
        elif result in ("Inconclusive", "Skipped"):
            skipped += 1

    print(f"\n[run_unity_tests] {total} tests, {total - failed - skipped} passed, {failed} failed, {skipped} skipped")
    for name, text in failures:
        print(f"  FAIL {name}\n       {text}")
    print(f"[run_unity_tests] results: {results}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
