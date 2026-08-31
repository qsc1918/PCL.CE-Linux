#!/usr/bin/env pwsh
# Fix PropertyMetadata usage in AvaloniaProperty.Register calls
# Pattern: AvaloniaProperty.Register("Name", typeof(T), typeof(Owner), new PropertyMetadata(default))
# -> AvaloniaProperty.Register<TOwner, TValue>("Name", default)
# This is complex; we do a simplified mechanical conversion.
Set-Location "D:\a\j\PCL.CE-Linux"

# First: convert FrameworkPropertyMetadata(default, AffectsRender, callback) 
# -> AvaloniaProperty.Register<TOwner,TValue>("name", default) + AffectsRender in class ctor
# This is too complex for regex. Let's just fix the simple PropertyMetadata(default) cases.

$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'new PropertyMetadata\(' -Quiet }
Write-Host "$($files.Count) files contain PropertyMetadata"

foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    
    # Pattern 1: AvaloniaProperty.Register("Name", typeof(T), typeof(Owner), new PropertyMetadata(defaultVal));
    # -> AvaloniaProperty.Register<Owner, T>("Name", defaultVal);
    # Handle string defaults
    $content = [regex]::Replace($content,
        'AvaloniaProperty\.Register\("(\w+)",\s*typeof\((\w+)\),\s*typeof\((\w+)\),\s*new PropertyMetadata\(([^)]+)\)\)',
        'AvaloniaProperty.Register<$3, $2>("$1", $4)')
    
    # Pattern 2: AvaloniaProperty.Register("Name", typeof(T), typeof(Owner), new PropertyMetadata(..., callback));
    # This has nested parens in callback - can't simple regex. Leave for manual.
    
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        $remaining = [regex]::Matches($content, 'new PropertyMetadata\(').Count
        Write-Host "Fixed: $($f.Name) (remaining: $remaining)"
    }
}
