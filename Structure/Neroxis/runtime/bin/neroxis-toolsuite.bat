@echo off
set DIR="%~dp0"
set JAVA_EXEC="%DIR:"=%\java"



pushd %DIR% & %JAVA_EXEC% %CDS_JVM_OPTS% -XX:+AutoCreateSharedArchive "-XX:SharedArchiveFile=%~dp0/neroxis-toolsuite.jsa" -p "%~dp0/../app" -m com.faforever.neroxis.toolsuite/com.faforever.neroxis.toolsuite.MapToolSuite  %* & popd
