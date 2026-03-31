#!/usr/bin/bash
set -x
# Build YASTM
echo "Cleaning source..."dotnet clean ./YASTM/Source -p:MSBuildEnableWorkloadResolver=false
echo "Removing old trash......."
rm -R ./YASTM/Source/bin/ ./YASTM/Source/obj/ ./YASTM/1.6/Assemblies/YASTM.*
echo "Building shiny things......"
dotnet build -c Release ./YASTM/Source -p:MSBuildEnableWorkloadResolver=false
#dotnet build -c Release ./YASTM/Source
