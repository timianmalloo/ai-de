"""Verify the frozen Atlas nine-entry manifest measurement (read-only, stdlib)."""
import argparse
import hashlib
import json
from pathlib import Path


def verify(raw, fixture):
    expected = fixture["entries"]
    if len(expected) != 9 or len(set(expected)) != 9:
        raise ValueError("fixture must contain nine distinct recorded entries")
    canonical = ("\n".join(expected) + "\n").encode("utf-8")
    if hashlib.sha256(canonical).hexdigest() != fixture["sha256"]:
        raise ValueError("fixture entries disagree with the independent recorded digest")
    lines = raw.decode("utf-8").splitlines()
    actual_hash = hashlib.sha256(raw).hexdigest()
    same_entries = len(lines) == 9 and len(set(lines)) == 9 and set(lines) == set(expected)
    same_order = lines == expected
    exact_bytes = raw == canonical
    return {
        "passed": same_entries and same_order and exact_bytes and actual_hash == fixture["sha256"],
        "identity_set_equal": same_entries,
        "canonical_order_equal": same_order,
        "canonical_bytes_equal": exact_bytes,
        "actual_sha256": actual_hash,
        "expected_sha256": fixture["sha256"],
        "serialization": fixture["serialization"],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--fixture", type=Path, default=Path(__file__).with_name("atlas-audit-manifest.fixture.json"))
    args = parser.parse_args()
    try:
        result = verify(args.manifest.read_bytes(), json.loads(args.fixture.read_text(encoding="utf-8")))
    except (OSError, UnicodeError, ValueError, KeyError, TypeError) as error:
        print(json.dumps({"passed": False, "error": str(error)}))
        return 2
    print(json.dumps(result, sort_keys=True))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
