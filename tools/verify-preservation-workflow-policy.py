"""Refuse a preservation push whose workflows differ from the current integration policy."""
import argparse
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('candidate')
    parser.add_argument('--policy', default='main')
    args = parser.parse_args()
    trees = []
    for ref in (args.policy, args.candidate):
        result = subprocess.run(['git', 'rev-parse', '--verify', ref + ':.github/workflows'],
                                text=True, capture_output=True)
        if result.returncode:
            print('PRESERVATION-WORKFLOW-NOT-CHECKED: cannot resolve ' + ref)
            return 2
        trees.append(result.stdout.strip())
    if trees[0] != trees[1]:
        print('PRESERVATION-WORKFLOW-DRIFT: apply the current workflow policy before pushing ' + args.candidate)
        return 1
    print('Preservation workflow policy matches ' + args.policy)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
