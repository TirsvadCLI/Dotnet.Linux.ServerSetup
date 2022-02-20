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
RUN dotnet build
WORKDIR /app/src/ServerSetup/bin/Debug/net6.0/linux-x64
RUN ./ServerSetup
RUN cat logs/run.log

