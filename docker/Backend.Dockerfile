FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY sources/Backend ./Backend
COPY sources/App/AppConfig.xml ./AppConfig.xml

WORKDIR /src/Backend
RUN dotnet restore
RUN dotnet publish -c Release -o /app

RUN mkdir -p /app/wwwroot

RUN apt-get update && apt-get install -y xmlstarlet
WORKDIR /src
RUN VERSION=$(xmlstarlet sel -t -v '/AppConfig/Version' AppConfig.xml) && \
    VERSION_NAME=$(xmlstarlet sel -t -v '/AppConfig/VersionName' AppConfig.xml) && \
    echo '<?xml version="1.0" encoding="utf-8"?>' > /app/wwwroot/Manifest.xml && \
    echo '<Manifest>' >> /app/wwwroot/Manifest.xml && \
    echo "    <Version>$VERSION</Version>" >> /app/wwwroot/Manifest.xml && \
    echo "    <VersionName>$VERSION_NAME</VersionName>" >> /app/wwwroot/Manifest.xml && \
    echo '    <PathDownload>download/</PathDownload>' >> /app/wwwroot/Manifest.xml && \
    echo '    <FileSource>source.zip</FileSource>' >> /app/wwwroot/Manifest.xml && \
    echo '    <FileSetup>MonkeySpeakSetup.exe</FileSetup>' >> /app/wwwroot/Manifest.xml && \
    echo '</Manifest>' >> /app/wwwroot/Manifest.xml

RUN ls -la /app/wwwroot/
RUN cat /app/wwwroot/Manifest.xml


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENTRYPOINT ["dotnet", "monkeyspeakbackend.dll"]