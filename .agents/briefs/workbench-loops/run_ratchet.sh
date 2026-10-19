#!/bin/sh
# Step-8 ratchet on clean detached trees: run_ratchet.sh <parent sha> <head sha> <label>
set -u
REPO=/c/Projects/ai-de
S=${RATCHET_OUT_DIR:-${TMPDIR:-/tmp}/ratchet-out}; mkdir -p "$S"
P=/c/Projects/rt-$3-p
H=/c/Projects/rt-$3-h
OUT=$S/ratchet-$3-out
git -C "$REPO" worktree add --detach "$P" "$1" >/dev/null 2>&1 || exit 3
git -C "$REPO" worktree add --detach "$H" "$2" >/dev/null 2>&1 || exit 3
python -I "$REPO/.agents/briefs/workbench-loops/gate_ratchet.py" "$P" "$H" "$OUT"
rc=$?
git -C "$REPO" worktree remove --force "$P"
git -C "$REPO" worktree remove --force "$H"
echo "ratchet exit $rc"
exit $rc
