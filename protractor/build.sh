#!/bin/bash
set -e

# Build
dotnet build

mkdir -p ./Output/Protractor/Plugins

# Install to Output
cp bin/Debug/protractor.dll ./Output/Protractor/Plugins/
cp ../Protractor.version ./Output/Protractor/
cp LICENSE ./Output/Protractor/
cp gpl.txt ./Output/Protractor/
cp mit.txt ./Output/Protractor/

# Package
rm -f Output/ProtractorContinued.zip
(cd Output; zip -r ProtractorContinued.zip Protractor)

echo "Done. Output/ProtractorContinued.zip ready."
