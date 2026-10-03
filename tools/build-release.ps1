# 本地发布构建脚本（PCL CE Linux）
#
# 作用：把 Microsoft OAuth 客户端 ID 在**编译期**注入到程序里（不写进源码、不进 git）。
#
# 原理：PCL.Core.SourceGenerators 的 EnvironmentInteropGenerator 在编译时读取环境变量 ——
#       只要设置了 PCL_WRITE_SECRET，所有 PCL_ 开头的环境变量都会去掉前缀后写入 SecretDictionary，
#       再由 PCL.Core.App.Secrets.MSOAuthClientId 读取。这正是上游的做法。
#
# 重要：编译服务器（VBCSCompiler / MSBuild 节点）会常驻并缓存环境变量，
#       直接 export 后构建**不会生效**（实测密钥进不去产物）。本脚本会先关掉它们。
#
# 用法：
#   pwsh -File tools/build-release.ps1                 # 构建 Windows + Linux 两套包
#   pwsh -File tools/build-release.ps1 -Target linux   # 只构建 Linux
#   pwsh -File tools/build-release.ps1 -Target windows # 只构建 Windows
#
# 客户端 ID 的提供方式（二选一，都不需要改代码）：
#   1) 环境变量 PCL_MS_CLIENT_ID
#   2) 本地文件 .secrets/ms_client_id.txt（已在 .gitignore 中，不会被提交）

param(
    [ValidateSet('all', 'windows', 'linux')]
    [string]$Target = 'all',
    [string]$OutputRoot = 'dist'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$proj = 'src/Plain Craft Launcher 2/Plain Craft Launcher 2.csproj'
$secretFile = Join-Path $repoRoot '.secrets/ms_client_id.txt'

# ---- 1. 取客户端 ID ----
$clientId = $env:PCL_MS_CLIENT_ID
if ([string]::IsNullOrWhiteSpace($clientId) -and (Test-Path $secretFile)) {
    $clientId = (Get-Content $secretFile -Raw).Trim()
    Write-Host "已从 $secretFile 读取客户端 ID" -ForegroundColor DarkGray
}
if ([string]::IsNullOrWhiteSpace($clientId)) {
    Write-Host "错误：没有找到客户端 ID。" -ForegroundColor Red
    Write-Host "请设置环境变量 PCL_MS_CLIENT_ID，或把 ID 写到 $secretFile" -ForegroundColor Yellow
    Write-Host "（该文件已被 .gitignore 忽略，不会提交）" -ForegroundColor DarkGray
    exit 1
}
$idPrefix = $clientId.Substring(0, [Math]::Min(8, $clientId.Length))
Write-Host "客户端 ID：$idPrefix...（共 $($clientId.Length) 字符）" -ForegroundColor Green

# ---- 2. 关掉常驻编译服务器，否则注入不生效 ----
Write-Host "正在关闭编译服务器…" -ForegroundColor DarkGray
dotnet build-server shutdown | Out-Null

# ---- 3. 注入环境变量并构建 ----
$env:PCL_WRITE_SECRET = '1'
$env:PCL_MS_CLIENT_ID = $clientId
$env:MSBUILDDISABLENODEREUSE = '1'

function Build-One([string]$rid, [string]$outDir, [bool]$singleFile) {
    Write-Host "`n=== 构建 $rid -> $outDir ===" -ForegroundColor Cyan
    $publishArgs = @(
        'publish', $proj,
        '-c', 'Release',
        '-r', $rid,
        '--self-contained', 'true',
        '-o', $outDir
    )
    if (-not $singleFile) { $publishArgs += '-p:PublishSingleFile=false' }
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "构建失败：$rid" }
}

if ($Target -in @('all', 'windows')) {
    Build-One 'win-x64' (Join-Path $OutputRoot 'win-x64') $true
}
if ($Target -in @('all', 'linux')) {
    Build-One 'linux-x64' (Join-Path $OutputRoot 'linux-x64') $true
    Build-One 'linux-x64' (Join-Path $OutputRoot 'linux-x64-flat') $false
    foreach ($d in @((Join-Path $OutputRoot 'linux-x64'), (Join-Path $OutputRoot 'linux-x64-flat'))) {
        $sh = Join-Path $d 'run.sh'
        @(
            '#!/usr/bin/env bash',
            'set -e',
            'cd "$(dirname "$0")"',
            'export LC_ALL=zh_CN.UTF-8',
            'export LANG=zh_CN.UTF-8',
            'exec ./PCL "$@"'
        ) -join "`n" | Set-Content -Path $sh -NoNewline -Encoding utf8
    }
}

# ---- 4. 校验密钥确实进了产物 ----
Write-Host "`n=== 校验 ===" -ForegroundColor Cyan
$found = $false
Get-ChildItem -Path $OutputRoot -Recurse -Filter 'PCL.Core.dll' -ErrorAction SilentlyContinue | ForEach-Object {
    $bytes = [System.IO.File]::ReadAllBytes($_.FullName)
    $utf16 = [System.Text.Encoding]::Unicode.GetBytes($clientId)
    for ($i = 0; $i -le $bytes.Length - $utf16.Length; $i++) {
        if ($bytes[$i] -eq $utf16[0]) {
            $match = $true
            for ($j = 1; $j -lt $utf16.Length; $j++) {
                if ($bytes[$i + $j] -ne $utf16[$j]) { $match = $false; break }
            }
            if ($match) { $found = $true; break }
        }
    }
    if ($found) { Write-Host ("  已注入：{0}" -f $_.FullName) -ForegroundColor Green }
}
if (-not $found) {
    Write-Host "  警告：产物里没有找到客户端 ID，注入可能失败！" -ForegroundColor Red
    exit 1
}
Write-Host "`n完成。" -ForegroundColor Green
