# Run-PinSpike.ps1 — PD-5's attended half in one command (the three steps, in order, stopping on the first red).
#
#   pwsh -File spikes\compile-session-pin-wire\Run-PinSpike.ps1            (from C:\projects\ai-de)
#   pwsh -File spikes\compile-session-pin-wire\Run-PinSpike.ps1 -DryRun    (steps 1-2 to session/new only: no prompt, no tokens)
#
# 1. setup-fixture.js  — regenerates the fixture repo and its bare origin (no model, < 1 s)
# 2. run-spike.js      — the attended step: a pinned compile session on your subscription, the read
#                        prompt, then the hostile line; every frame recorded (emails redacted)
# 3. assert-spike.py   — the seven assertions over the frames directory run-spike.js just wrote
#
# Exit 0 = GREEN (the artifact is written; the agentic rung is admissible — Ruling 68).
# Exit ≠ 0 = RED or a step failed — a hard stop for every agentic rung, never a fallback; the
# frames directory named below is the finding. Nothing here sends anything the three steps do not.
param([switch]$DryRun)
$ErrorActionPreference = "Stop"
$spike = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $spike)
Set-Location $repo
$env:PYTHONIOENCODING = "utf-8"

function Step($n, $what, $cmd) {
    Write-Host ""
    Write-Host "== step $n : $what" -ForegroundColor Cyan
    & $cmd
    if ($LASTEXITCODE -ne 0) {
        Write-Host "step $n failed (exit $LASTEXITCODE) — stopping here." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Step 1 "setup-fixture.js (fresh fixture repo + bare origin)" { node "$spike\setup-fixture.js" }

# The frames directory is whichever one run-spike.js creates during this run: snapshot before, diff after.
$framesRoot = Join-Path $spike "frames"
$before = @(Get-ChildItem -Directory $framesRoot -ErrorAction SilentlyContinue | ForEach-Object { $_.Name })

if ($DryRun) {
    Step 2 "run-spike.js --dry-run (initialize + session/new with the pin triple; no prompt, no tokens)" { node "$spike\run-spike.js" --dry-run }
    Write-Host ""
    Write-Host "DRY RUN complete — the wire shape is under frames\; nothing was prompted. Run again without -DryRun for the attended half." -ForegroundColor Green
    exit 0
}

Step 2 "run-spike.js (attended — watch the frames; the pinned session, the read prompt, the hostile line)" { node "$spike\run-spike.js" }

$after = @(Get-ChildItem -Directory $framesRoot | ForEach-Object { $_.Name })
$new = @($after | Where-Object { $before -notcontains $_ } | Sort-Object)
if ($new.Count -ne 1) {
    Write-Host "expected exactly one new frames directory, found $($new.Count): $($new -join ', ') — stopping here." -ForegroundColor Red
    exit 3
}
$framesDir = Join-Path $framesRoot $new[0]
Write-Host "frames: $framesDir"

Step 3 "assert-spike.py (the seven assertions)" { python "$spike\assert-spike.py" $framesDir }

Write-Host ""
Write-Host "GREEN — PD-5 observed on the wire. Evidence: $framesDir and spikes\compile-session-pin-wire\compile-pin-spike.json" -ForegroundColor Green
Write-Host "Tell the conductor; it takes the artifact into the join and dispatches CV-3."
exit 0
