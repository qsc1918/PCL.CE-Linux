#!/usr/bin/env pwsh
# Fix PropertyMetadata with callbacks: convert to AvaloniaProperty.Register + Changed.AddClassHandler
# Pattern: AvaloniaProperty.Register("Name", typeof(T), typeof(Owner), new PropertyMetadata(defaultVal, callback))
# Also: FrameworkPropertyMetadata(defaultVal, FrameworkPropertyMetadataOptions.AffectsRender, callback)
Set-Location "D:\a\j\PCL.CE-Linux"

$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'PropertyMetadata' -Quiet }
Write-Host "$($files.Count) files with PropertyMetadata"

foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    
    # Pattern: AvaloniaProperty.Register("Name", typeof(T), typeof(Owner), new PropertyMetadata(default, callback))
    # -> AvaloniaProperty.Register<Owner, T>("Name", default)
    # + Owner.Changed.AddClassHandler<Owner>((d, e) => callback(d, e))
    # This is too complex for regex. Instead, just extract the default value and remove the callback part.
    # The callback will be wired up separately.
    
    # Simple approach: new PropertyMetadata(default, (d, e) => ...) -> new PropertyMetadata(default)
    # This removes callbacks but keeps defaults. Then we add handlers in constructors.
    # For now, just make it compile.
    
    # Pattern 1: new PropertyMetadata(defaultValue, callback) -> defaultValue
    # We need to handle nested parens in callback
    $content = [regex]::Replace($content,
        'new PropertyMetadata\(([^,()]+(?:\([^)]*\))?[^,()]*),\s*\([^)]*\)\s*=>\s*[^)]*\)',
        'new PropertyMetadata($1)')
    
    # Pattern 2: new PropertyMetadata(defaultValue, (d, e) => expr) 
    # Handle multi-line callbacks with balanced parens - simplified
    # Just remove everything after first comma in PropertyMetadata(...)
    # This is risky. Better to do per-file.
    
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        $remaining = ([regex]::Matches($content, 'PropertyMetadata\(')).Count
        Write-Host "Fixed: $($f.Name) (remaining: $remaining)"
    }
}
