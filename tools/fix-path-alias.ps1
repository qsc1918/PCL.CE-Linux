# Fix Path ambiguity: for .cs files that have both Avalonia.Controls.Shapes using and System.IO usage
# with unqualified Path, add using alias. Also dedupe using lines. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$n = 0
Get-ChildItem $Root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    # skip files that already have the alias
    if ($t -match 'using Path =') { return }
    # must use unqualified Path as type or ctor
    $usesPath = $t -match '(?m)(^|[^.\w])Path(\s|\(|\.|>|,|\))' -and $t -notmatch 'System\.IO\.Path|IcoPath|ExePath|pathImage'
    if (-not $usesPath) { return }
    if ($t -notmatch 'using Avalonia\.Controls\.Shapes;') { return }
    # add alias right after the Shapes using
    $t = [regex]::Replace($t, 'using Avalonia\.Controls\.Shapes;', "using Avalonia.Controls.Shapes;`r`nusing Path = Avalonia.Controls.Shapes.Path;", 'Multiline')
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t); $n++ }
}
"alias added: $n files"
