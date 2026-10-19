"""Refuse a preservation push whose workflows differ from the current integration policy.

Run with no argument the candidate is HEAD, so the pack runner (which runs every gate argument-free)
checks the tree it is standing in against `main`. Pass a ref to check a branch before pushing it.

  python tools/verify-preservation-workflow-policy.py [candidate] [--policy main]
  python tools/verify-preservation-workflow-policy.py --self-test    prove the gate can fail (DC-104)

Exit 0 matches, 1 drift, 2 a ref could not be resolved.
"""
import argparse
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def workflow_tree(ref, cwd=None):
    """The git tree id of .github/workflows at `ref`, or None when it cannot be resolved."""
    result = subprocess.run(['git', 'rev-parse', '--verify', ref + ':.github/workflows'],
                            text=True, encoding='utf-8', errors='replace',
                            capture_output=True, cwd=cwd)
    return None if result.returncode else result.stdout.strip()


def check(policy, candidate, cwd=None):
    trees = []
    for ref in (policy, candidate):
        tree = workflow_tree(ref, cwd)
        if tree is None:
            print('PRESERVATION-WORKFLOW-NOT-CHECKED: cannot resolve ' + ref)
            return 2
        trees.append(tree)
    if trees[0] != trees[1]:
        print('PRESERVATION-WORKFLOW-DRIFT: apply the current workflow policy before pushing ' + candidate)
        return 1
    print('Preservation workflow policy matches ' + policy)
    return 0


def self_test():
    """Build a throwaway repository and require all three exit codes."""
    def git(cwd, *args):
        subprocess.run(['git', '-c', 'user.name=t', '-c', 'user.email=t@t', *args],
                       cwd=cwd, check=True, capture_output=True, text=True,
                       encoding='utf-8', errors='replace')

    with tempfile.TemporaryDirectory() as tmp:
        work = Path(tmp)
        git(work, 'init', '-q', '-b', 'main')
        workflows = work / '.github' / 'workflows'
        workflows.mkdir(parents=True)
        (workflows / 'build.yml').write_text('name: a\n', encoding='utf-8', newline='\n')
        git(work, 'add', '-A')
        git(work, 'commit', '-q', '-m', 'base')
        git(work, 'branch', 'same')
        git(work, 'checkout', '-q', '-b', 'drift')
        (workflows / 'build.yml').write_text('name: b\n', encoding='utf-8', newline='\n')
        git(work, 'commit', '-q', '-am', 'drift')
        git(work, 'checkout', '-q', '-b', 'bare')
        git(work, 'rm', '-q', '-r', '.github')
        git(work, 'commit', '-q', '-m', 'no workflows')
        expected = (('same', 0), ('drift', 1), ('bare', 2), ('missing-ref', 2))
        failures = [f'{ref}: exit {code}, expected {want}'
                    for ref, want in expected
                    if (code := check('main', ref, cwd=work)) != want]
    if failures:
        print('self-test FAILED: ' + '; '.join(failures))
        return 1
    print('self-test OK - a matching, a drifted, a workflow-less and an unknown candidate each exit as declared.')
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('candidate', nargs='?', default='HEAD')
    parser.add_argument('--policy', default='main')
    parser.add_argument('--self-test', action='store_true', help='prove the gate can fail')
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    return check(args.policy, args.candidate)


if __name__ == '__main__':
    raise SystemExit(main())
