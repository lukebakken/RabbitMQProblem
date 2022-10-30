$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 'Latest' -ErrorAction 'Stop' -Verbose

1..10 | ForEach-Object {
    Start-Job -ScriptBlock { dotnet run }
}
