#!/usr/bin/bash
# Build YASTM
echo "Cleaning source..."
dotnet clean ./YASTM/Source
echo "Removing old trash......."
rm -R ./YASTM/Source/bin/ ./YASTM/Source/obj/ ./YASTM/1.6/Assemblies/YASTM.*
echo "Building shiny things......"
dotnet build -c Release ./YASTM/Source
