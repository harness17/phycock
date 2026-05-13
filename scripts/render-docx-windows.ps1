param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDir,

    [switch]$EmitPdf
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$inputFile = (Resolve-Path -LiteralPath $InputPath).Path
$outputPath = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDir))
[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
Get-ChildItem -LiteralPath $outputPath -Filter 'page-*.emf' -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $outputPath -Filter 'page-*.png' -ErrorAction SilentlyContinue | Remove-Item -Force

$word = $null
$document = $null

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0

    $document = $word.Documents.Open($inputFile, $false, $true)
    $pageCount = $document.ComputeStatistics(2)

    if ($EmitPdf) {
        $pdfPath = Join-Path $outputPath ([System.IO.Path]::GetFileNameWithoutExtension($inputFile) + '.pdf')
        $document.ExportAsFixedFormat($pdfPath, 17)
        Write-Host "PDF: $pdfPath"
    }

    for ($page = 1; $page -le $pageCount; $page++) {
        $pageStart = $document.GoTo(1, 1, $page).Start
        if ($page -lt $pageCount) {
            $pageEnd = $document.GoTo(1, 1, $page + 1).Start - 1
        }
        else {
            $pageEnd = $document.Content.End
        }

        $range = $document.Range($pageStart, $pageEnd)
        $emfBytes = [byte[]]$range.EnhMetaFileBits
        $emfPath = Join-Path $outputPath ("page-$page.emf")
        $pngPath = Join-Path $outputPath ("page-$page.png")

        [System.IO.File]::WriteAllBytes($emfPath, $emfBytes)

        $image = [System.Drawing.Image]::FromFile($emfPath)
        try {
            $canvas = New-Object System.Drawing.Bitmap $image.Width, $image.Height
            $graphics = [System.Drawing.Graphics]::FromImage($canvas)
            try {
                $graphics.Clear([System.Drawing.Color]::White)
                $graphics.DrawImage($image, 0, 0, $image.Width, $image.Height)
                $canvas.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally {
                $graphics.Dispose()
                $canvas.Dispose()
            }
        }
        finally {
            $image.Dispose()
        }

        Write-Host "PNG: $pngPath"
    }

    Write-Host "Rendered $pageCount page(s)."
}
finally {
    if ($document -ne $null) {
        $document.Close($false)
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($document) | Out-Null
    }
    if ($word -ne $null) {
        $word.Quit()
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
