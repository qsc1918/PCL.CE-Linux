#!/usr/bin/env pwsh
# Fix Keyboard static class references
# WPF: Keyboard.FocusedElement, Keyboard.Modifiers, Keyboard.IsKeyDown
# Avalonia: TopLevel.GetTopLevel(this)?.KeyboardDevice, or use InputElement extensions
# For now, fix the simple cases:
# - Keyboard.FocusedElement -> null (stub, needs per-case)
# - Keyboard.Focus(element) -> element.Focus() (already done in fix-keyboard.ps1)
# - Keyboard.Modifiers -> (KeyboardDevice?.Modifiers ?? ModifierKeys.None)
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\bKeyboard\.' -Quiet }
Write-Host "$($files.Count) files with Keyboard."
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # Keyboard.FocusedElement -> null (temporary stub)
    $content = $content -replace '\bKeyboard\.FocusedElement\b', 'null /* [port] Keyboard.FocusedElement */'
    # Keyboard.Modifiers -> (KeyboardDevice?.Modifiers ?? ModifierKeys.None)
    $content = $content -replace '\bKeyboard\.Modifiers\b', '(KeyboardDevice?.Modifiers ?? ModifierKeys.None)'
    # Keyboard.IsKeyDown(key) -> false (temporary stub)
    $content = $content -replace '\bKeyboard\.IsKeyDown\(([^)]+)\)', 'false /* [port] Keyboard.IsKeyDown($1) */'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
