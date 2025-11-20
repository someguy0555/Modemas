Remove-Item -Recurse -Force "./Modemas.Tests/TestResults/*" -ErrorAction Ignore
dotnet test --collect:"XPlat Code Coverage"
dotnet tool run reportgenerator `
    -reports:"./Modemas.Tests/TestResults/*/coverage.cobertura.xml" `
    -targetdir:"./CoverageReport" `
    -reporttypes:Html
Start-Process "./CoverageReport/index.html"
