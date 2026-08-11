# Nombre del archivo de salida
$outputFile = "mi_proyecto_completo.txt"
# Extensiones que quieres incluir (ajusta según tu lenguaje: .cs, .cpp, .py, .js)
$extensions = "*.cs", "*.xaml", "*.sql" 
# Carpetas a ignorar para no saturar a la IA
$excludeFolders = "bin", "obj", ".git", ".vs", "packages"

Remove-Item $outputFile -ErrorAction SilentlyContinue

Get-ChildItem -Recurse -Include $extensions | Where-Object { 
    $path = $_.FullName
    $ignore = $false
    foreach ($folder in $excludeFolders) {
        if ($path -like "*\$folder\*") { $ignore = $true; break }
    }
    !$ignore
} | ForEach-Object {
    "--- INICIO ARCHIVO: $($_.FullName) ---" | Add-Content $outputFile
    Get-Content $_.FullName | Add-Content $outputFile
    "`n--- FIN ARCHIVO ---`n" | Add-Content $outputFile
}

Write-Host "¡Listo! Archivo generado en: $outputFile" -ForegroundColor Green
