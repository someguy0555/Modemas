#! /bin/bash

rm -rf ./Modemas.Tests/TestResults/*
dotnet test --settings ./myconfig.runsettings
dotnet tool run reportgenerator   -reports:"./Modemas.Tests/TestResults/*/coverage.cobertura.xml"   -targetdir:"./CoverageReport"   -reporttypes:Html 
firefox ./CoverageReport/index.html
