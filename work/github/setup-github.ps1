<#
setup-github.ps1 - labels, milestones and issues (drafts in issues/*.md, first line = title) for this project.
Safe to rerun: labels are updated, existing milestones and issue titles are skipped. Needs gh logged in.
  .\setup-github.ps1 -Repo owner/repo [-DryRun]
#>
param([Parameter(Mandatory)][string]$Repo, [switch]$DryRun)
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
function Do-It([string]$what, [scriptblock]$run) { if ($DryRun) { "DRY  $what" } else { & $run | Out-Null; "DONE $what" } }
foreach ($l in (Get-Content "$here\labels.json" -Raw | ConvertFrom-Json)) {
    Do-It "label $($l.name)" { gh label create $l.name -R $Repo --color $l.color --description $l.description --force }
}
$have = @(gh api "repos/$Repo/milestones?state=all" --jq '.[].title')
foreach ($m in (Get-Content "$here\milestones.json" -Raw | ConvertFrom-Json)) {
    if ($have -contains $m.title) { "SKIP milestone $($m.title)"; continue }
    Do-It "milestone $($m.title)" { gh api "repos/$Repo/milestones" -f "title=$($m.title)" -f "description=$($m.description)" }
}
$titles = @(gh issue list -R $Repo --state all --limit 500 --json title --jq '.[].title')
Get-ChildItem "$here\issues" -Filter '*.md' | Sort-Object Name | ForEach-Object {
    $lines = Get-Content $_.FullName -Encoding UTF8
    $title = $lines[0].TrimStart('#', ' ')
    if ($titles -contains $title) { "SKIP issue $title"; return }
    $body = ($lines | Select-Object -Skip 1) -join "`n"
    Do-It "issue $title" { gh issue create -R $Repo --title $title --body $body }
}
