#!/usr/bin/env pwsh
# Fix Keyboard.FocusedElement -> FocusManager.GetFocusedElement or similar
# WPF: Keyboard.FocusedElement returns IInputElement
# Avalonia: no static Keyboard class; use TopLevel.GetTopLevel(this)?.FocusManager
# Simplified: Keyboard.Focus(x) -> x.Focus() in most cases
# Keyboard.IsKeyDown -> no direct equivalent, use KeyBinding or InputElement
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\bKeyboard\.' -Quiet }
Write-Host "$($files.Count) files contain Keyboard."
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # Keyboard.FocusedElement -> FocusManager.GetFocusManager(this)?.GetFocusedElement()
    # Actually simpler: just leave as TODO comment for now, as this is per-case
    # Keyboard.Focus(element) -> element.Focus()
    $content = [regex]::Replace($content, 'Keyboard\.Focus\(([^)]+)\)', '$1.Focus()')
    # Keyboard.IsFocused -> IsFocused (property on element in Avalonia)
    # Actually Avalonia uses IInputElement.IsFocused too
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
