#! /opt/microsoft/powershell/7/pwsh

Remove-Item -Recurse -Force "./Modemas.Tests/TestResults/*" -ErrorAction Ignore
dotnet test --settings ./myconfig.runsettings
dotnet tool run reportgenerator `
    -reports:"./Modemas.Tests/TestResults/*/coverage.cobertura.xml" `
    -targetdir:"./CoverageReport" `
    -reporttypes:Html
Invoke-Expression "./CoverageReport/index.html"
