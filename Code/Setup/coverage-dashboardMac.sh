#!/bin/bash
# VNM Coverage Dashboard for macOS/Linux
set -e

# Backend coverage
echo "Generating backend coverage report..."
if [ -d "../BackEnd/Tests" ]; then
  dotnet test ../BackEnd/Tests --collect:"XPlat Code Coverage" || echo "Backend tests failed."
  if [ -d "../BackEnd/Tests/TestResults" ]; then
    reportgenerator -reports:../BackEnd/Tests/TestResults/**/*.xml -targetdir:../TestCoverage/backend -reporttypes:Html
    echo "Backend coverage report generated at ../TestCoverage/backend/index.html"
  fi
else
  echo "Backend test directory not found."
fi

# UI coverage
echo "Generating UI coverage report..."
if [ -d "../ReactUI" ]; then
  cd ../ReactUI
  npm run coverage || echo "UI tests failed."
  if [ -d "coverage" ]; then
    mkdir -p ../TestCoverage/ui
    cp -r coverage/* ../TestCoverage/ui/
    echo "UI coverage report generated at ../TestCoverage/ui/index.html"
  fi
  cd -
else
  echo "UI directory not found."
fi

# Summary page
if [ -d "../TestCoverage" ]; then
  echo "<html><body><h1>Test Coverage Dashboard</h1><ul>" > ../TestCoverage/index.html
  [ -f ../TestCoverage/backend/index.html ] && echo "<li><a href='backend/index.html'>Backend Coverage</a></li>" >> ../TestCoverage/index.html
  [ -f ../TestCoverage/ui/index.html ] && echo "<li><a href='ui/index.html'>UI Coverage</a></li>" >> ../TestCoverage/index.html
  echo "</ul></body></html>" >> ../TestCoverage/index.html
  echo "Summary dashboard generated at ../TestCoverage/index.html"
else
  echo "TestCoverage directory not found."
fi
