FROM tirsvad/debian_11-dotnet_6:latest AS build

WORKDIR /app

COPY . .

# RUN dotnet build

# docker build -t packagemanager-service:latest .
# docker run --rm -it -P 5000:80 packagemanager-service:latest

# run the unit tests

FROM build AS test

COPY .localNugetFeed/ /srv/Nuget

WORKDIR /app/src/ServerSetup/
# RUN dotnet build
RUN dotnet publish
WORKDIR /app/src/ServerSetup/bin/Debug/net6.0/linux-x64/publish
# RUN ./ServerSetup --help
RUN ./ServerSetup config copy
RUN ./ServerSetup --no-upgrade-os

# RUN ./ServerSetup copy
# RUN cat logs/run.log

