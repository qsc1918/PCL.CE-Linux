#!/usr/bin/env pwsh
# Fix WPF Visibility enum usage -> Avalonia IsVisible bool
# Pure ASCII - no Chinese comments
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\.Visibility\b' -Quiet }
Write-Host "$($files.Count) files contain .Visibility"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # .Visibility = Visibility.xxx assignments
    $content = $content -replace '\.Visibility\s*=\s*Visibility\.Visible', '.IsVisible = true'
    $content = $content -replace '\.Visibility\s*=\s*Visibility\.Hidden', '.IsVisible = false'
    $content = $content -replace '\.Visibility\s*=\s*Visibility\.Collapsed', '.IsVisible = false'
    # Equality comparisons
    $content = $content -replace '==\s*Visibility\.Visible', '== true'
    $content = $content -replace '!=\s*Visibility\.Visible', '!= true'
    $content = $content -replace '==\s*Visibility\.Hidden', '== false'
    $content = $content -replace '==\s*Visibility\.Collapsed', '== false'
    $content = $content -replace '!=\s*Visibility\.Hidden', '!= false'
    $content = $content -replace '!=\s*Visibility\.Collapsed', '!= false'
    # .Visibility != -> .IsVisible !=
    $content = $content -replace '\.Visibility\s*!=\s*', '.IsVisible != '
    # .Visibility == -> .IsVisible ==
    $content = $content -replace '\.Visibility\s*==\s*', '.IsVisible == '
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        $remaining = [regex]::Matches($content, 'Visibility\.(Visible|Hidden|Collapsed)').Count
        Write-Host "Fixed: $($f.Name) (enum refs remaining: $remaining)"
    }
}
