param([switch]$AsJob = $false)

#$fileContent = Get-Content .\Finder.cs -Raw
#$randomString = (65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object { [char]$_ } | Join-String
#$fileContent = $fileContent.Replace('Rando', $randomString)
#Add-Type -TypeDefinition:$fileContent

$run = {
    Add-Type -Path:.\Finder.cs
    [FindFilesFast.Finder]::FindFiles('.', $true, $true) 
    | Select-Object -Property FullName, IsDirectory, IsReparsePoint, FileSize -First 100
    | Format-Table
}

if ($AsJob) {
    Start-Job -ScriptBlock:$run | Wait-Job | Receive-Job
} else {
    . $run
}

