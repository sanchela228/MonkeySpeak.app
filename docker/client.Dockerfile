FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY sources/App ./App

WORKDIR /src/App
RUN dotnet restore

RUN dotnet publish -c Release -r win-x64 --self-contained true -o /build/win

RUN apt-get update && apt-get install -y zip
WORKDIR /build
RUN zip -r source.zip win/

CMD ["bash", "-c", "cp -r /build/* /output"]
