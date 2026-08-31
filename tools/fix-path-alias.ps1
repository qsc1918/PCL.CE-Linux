# Comprehensive Path fix: add Shapes alias + qualify all System.IO.Path calls. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$ioMembers = 'Combine|GetFullPath|GetFileName|GetDirectoryName|GetExtension|GetTempPath|GetRandomFileName|GetShortPathName|Exists|DirectorySeparatorChar|AltDirectorySeparatorChar|ChangeExtension|GetInvalidFileNameChars|GetInvalidPathChars|GetFileNameWithoutExtension|GetPathRoot|HasExtension|Join|GetRelativePath|GetTempFileName|GetPathRoot'

$n = 0
Get-ChildItem $Root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    $hasShapes = $t -match 'using Avalonia\.Controls\.Shapes;'
    # qualify IO usages (not already qualified, not after alias)
    if ($hasShapes) {
        $t = [regex]::Replace($t, "(?<![\w.])(?<!System\.IO\.)(?<!IO\.)Path\.($ioMembers)\b", "System.IO.Path.`$1")
    }
    # add alias if missing and Shapes using present
    if ($hasShapes -and $t -notmatch 'using Path =') {
        $t = [regex]::Replace($t, 'using Avalonia\.Controls\.Shapes;', "using Avalonia.Controls.Shapes;`r`nusing Path = Avalonia.Controls.Shapes.Path;", 'Multiline')
    }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t); $n++ }
}
"fixed: $n files"
