#!/usr/bin/env node
// PD-5 — builds the disposable fixture repository the pin spike runs against.
//
// Regenerated on every run (never hand-edited in place): deletes any prior `fixture/` and
// `remote.git/`, copies the committed templates from `fixture-template/` into a fresh `fixture/`,
// `git init`s it, creates a local bare `remote.git` and sets it as `origin`, then makes one
// commit — so "no push happened" is measurable as "remote.git's ref set is unchanged" rather than
// inferred from "nothing looked different".
//
// Dependency-free (stdlib + the `git` binary on PATH) — spike scaffolding, not product code.
"use strict";

const fs = require("fs");
const path = require("path");
const { execFileSync } = require("child_process");

const ROOT = __dirname;
const TEMPLATE_DIR = path.join(ROOT, "fixture-template");
const FIXTURE_DIR = path.join(ROOT, "fixture");
const REMOTE_DIR = path.join(ROOT, "remote.git");

function rmrf(target) {
  fs.rmSync(target, { recursive: true, force: true });
}

function copyRecursive(src, dst) {
  const stat = fs.statSync(src);
  if (stat.isDirectory()) {
    fs.mkdirSync(dst, { recursive: true });
    for (const entry of fs.readdirSync(src)) {
      copyRecursive(path.join(src, entry), path.join(dst, entry));
    }
  } else {
    fs.mkdirSync(path.dirname(dst), { recursive: true });
    fs.copyFileSync(src, dst);
  }
}

function git(args, cwd) {
  return execFileSync("git", args, { cwd, encoding: "utf8" }).trim();
}

function main() {
  if (!fs.existsSync(TEMPLATE_DIR)) {
    console.error(`missing template directory: ${TEMPLATE_DIR}`);
    process.exit(1);
  }

  console.log(`[setup-fixture] removing prior fixture/ and remote.git/ (if any)`);
  rmrf(FIXTURE_DIR);
  rmrf(REMOTE_DIR);

  console.log(`[setup-fixture] copying fixture-template/ -> fixture/`);
  copyRecursive(TEMPLATE_DIR, FIXTURE_DIR);

  console.log(`[setup-fixture] git init --bare remote.git`);
  fs.mkdirSync(REMOTE_DIR, { recursive: true });
  git(["init", "--bare", "-q"], REMOTE_DIR);

  console.log(`[setup-fixture] git init fixture`);
  git(["init", "-q", "-b", "main"], FIXTURE_DIR);
  git(["config", "user.email", "pd5-fixture@example.invalid"], FIXTURE_DIR);
  git(["config", "user.name", "PD-5 fixture"], FIXTURE_DIR);
  git(["remote", "add", "origin", REMOTE_DIR], FIXTURE_DIR);
  git(["add", "-A"], FIXTURE_DIR);
  git(["commit", "-q", "-m", "fixture: initial state (generated, not pushed)"], FIXTURE_DIR);

  const head = git(["rev-parse", "HEAD"], FIXTURE_DIR);
  const treeHash = git(["write-tree"], FIXTURE_DIR);
  const remoteRefs = git(["rev-parse", "--all"], REMOTE_DIR);

  console.log(`[setup-fixture] fixture HEAD:   ${head}`);
  console.log(`[setup-fixture] fixture tree:   ${treeHash}`);
  console.log(`[setup-fixture] remote.git refs (should be empty — nothing pushed yet): ${JSON.stringify(remoteRefs)}`);
  console.log(`[setup-fixture] done. fixture=${FIXTURE_DIR} remote=${REMOTE_DIR}`);
}

main();
