Start-Job -ScriptBlock {
    Add-Type -Path:.\Finder.cs
    Measure-Command {
        [FindFilesFast.Finder]::FindFiles('', $true) 
        | Select-Object -First 100000 | Measure-Object | ForEach-Object Count | Write-Host
    } | ForEach-Object TotalMilliseconds
} | Wait-Job | Receive-Job


Measure-Command {
    Get-ChildItem "/" -Recurse 2>$null
    | Select-Object -First 100000 | Measure-Object | ForEach-Object Count | Write-Host
} | ForEach-Object TotalMilliseconds

