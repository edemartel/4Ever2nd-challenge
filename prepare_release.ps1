Remove-Item build.zip -Force
Compress-Archive -Force -Path Server\Application.csproj,Server\*.cs -DestinationPath build.zip