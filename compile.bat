:: Build YASTM

echo "Cleaning source..."
dotnet clean .\YASTM\Source\
echo "Removing old trash......."
Remove-Item -Recurse -Force .\YASTM\Source\bin, .\YASTM\Source\obj, .\YASTM\1.6\Assemblies\YASTM.*
echo "Building shiny things......"
dotnet build -c Release .\YASTM\Source