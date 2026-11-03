<#
.SYNOPSIS
    Shares this project's Claude Code memory and session transcripts between PCs through the git repo.

.DESCRIPTION
    Claude Code keeps per-project memory and session transcripts outside the repo, in
    %USERPROFILE%\.claude\projects\<project-path-slug>\. This script copies them into the repo (.claude\memory and
    .claude\sessions, transcripts gzipped) and back, so each PC can pick up where the other left off.

      -Save   Before you switch PCs: copy this PC's memory and transcripts into the repo. Then commit and push.
      -Load   After you pull on the other PC: copy the repo's memory and transcripts into Claude's folder for this
              project. Then "claude --resume" (or the session list in VS Code) shows the sessions.

    A transcript is only replaced when the incoming copy is bigger (transcripts only ever grow), so nothing newer is lost.
    The project's CLAUDE.md also imports .claude\memory, so Claude has the shared context on any PC even without -Load.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .claude\sync-claude.ps1 -Save
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .claude\sync-claude.ps1 -Load
#>
param(
    [switch]$Save,
    [switch]$Load
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression

if ($Save -eq $Load) {
    Write-Host 'Use exactly one of -Save (this PC -> repo) or -Load (repo -> this PC).'
    exit 1
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
# Claude Code names a project's folder after its path, every non-alphanumeric character turned into '-'.
$slug = $repo -replace '[^A-Za-z0-9]', '-'
$claudeHome = Join-Path $env:USERPROFILE '.claude'
$projectDir = Join-Path (Join-Path $claudeHome 'projects') $slug
$localMemory = Join-Path $projectDir 'memory'
$repoMemory = Join-Path $repo '.claude\memory'
$repoSessions = Join-Path $repo '.claude\sessions'

function Compress-File([string]$source, [string]$target) {
    $in = [System.IO.File]::Open($source, 'Open', 'Read', 'ReadWrite')
    try {
        $out = [System.IO.File]::Create($target)
        try {
            $gzip = New-Object System.IO.Compression.GZipStream($out, [System.IO.Compression.CompressionLevel]::Optimal)
            try { $in.CopyTo($gzip) } finally { $gzip.Dispose() }
        } finally { $out.Dispose() }
    } finally { $in.Dispose() }
}

function Expand-File([string]$source, [string]$target) {
    $in = [System.IO.File]::OpenRead($source)
    try {
        $gzip = New-Object System.IO.Compression.GZipStream($in, [System.IO.Compression.CompressionMode]::Decompress)
        try {
            $out = [System.IO.File]::Create($target)
            try { $gzip.CopyTo($out) } finally { $out.Dispose() }
        } finally { $gzip.Dispose() }
    } finally { $in.Dispose() }
}

function Get-GzipLength([string]$path) {
    # The uncompressed size is stored (mod 4 GB) in the last 4 bytes of a gzip file.
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 4) { return 0 }
    return [System.BitConverter]::ToUInt32($bytes, $bytes.Length - 4)
}

Write-Host "Repo:            $repo"
Write-Host "Claude project:  $projectDir"

if ($Save) {
    if (-not (Test-Path $projectDir)) {
        Write-Host 'This PC has no Claude data for the project yet; nothing to save.'
        exit 0
    }
    New-Item -ItemType Directory -Force $repoMemory | Out-Null
    New-Item -ItemType Directory -Force $repoSessions | Out-Null

    if (Test-Path $localMemory) {
        Get-ChildItem $localMemory -Filter *.md | ForEach-Object {
            Copy-Item $_.FullName (Join-Path $repoMemory $_.Name) -Force
            Write-Host "  memory   $($_.Name)"
        }
    }

    Get-ChildItem $projectDir -Filter *.jsonl | ForEach-Object {
        $target = Join-Path $repoSessions ($_.Name + '.gz')
        if ((Test-Path $target) -and (Get-GzipLength $target) -ge $_.Length) {
            Write-Host "  session  $($_.Name) (repo copy is already as long)"
            return
        }
        Compress-File $_.FullName $target
        Write-Host ("  session  {0} ({1:N1} MB -> {2:N1} MB)" -f $_.Name, ($_.Length / 1MB), ((Get-Item $target).Length / 1MB))
        # Large tool outputs a session saved beside its transcript.
        $folder = Join-Path $projectDir $_.BaseName
        if (Test-Path $folder) {
            $destination = Join-Path $repoSessions $_.BaseName
            New-Item -ItemType Directory -Force $destination | Out-Null
            Copy-Item (Join-Path $folder '*') $destination -Recurse -Force
        }
    }
    Write-Host 'Saved. Commit and push .claude\memory and .claude\sessions.'
}

if ($Load) {
    New-Item -ItemType Directory -Force $localMemory | Out-Null
    if (Test-Path $repoMemory) {
        Get-ChildItem $repoMemory -Filter *.md | ForEach-Object {
            Copy-Item $_.FullName (Join-Path $localMemory $_.Name) -Force
            Write-Host "  memory   $($_.Name)"
        }
    }

    if (Test-Path $repoSessions) {
        Get-ChildItem $repoSessions -Filter *.jsonl.gz | ForEach-Object {
            $name = $_.Name.Substring(0, $_.Name.Length - 3)
            $target = Join-Path $projectDir $name
            if ((Test-Path $target) -and (Get-Item $target).Length -ge (Get-GzipLength $_.FullName)) {
                Write-Host "  session  $name (this PC's copy is already as long)"
                return
            }
            Expand-File $_.FullName $target
            Write-Host "  session  $name"
            $base = [System.IO.Path]::GetFileNameWithoutExtension($name)
            $folder = Join-Path $repoSessions $base
            if (Test-Path $folder) {
                $destination = Join-Path $projectDir $base
                New-Item -ItemType Directory -Force $destination | Out-Null
                Copy-Item (Join-Path $folder '*') $destination -Recurse -Force
            }
        }
    }
    Write-Host 'Loaded. Run "claude --resume" in the project folder (or open the session list in VS Code) to continue a session.'
}
