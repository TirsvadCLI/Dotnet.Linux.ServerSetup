FROM tirsvad/debian_11-dotnet_6:latest AS build

WORKDIR /app

COPY . .

# RUN dotnet build

# docker build -t packagemanager-service:latest .
# docker run --rm -it -P 5000:80 packagemanager-service:latest

# run the unit tests

FROM build AS test

COPY .localNugetFeed/ /srv/Nuget

WORKDIR /app/src/LinuxServerSetup/
RUN dotnet build
WORKDIR /app/src/LinuxServerSetup/bin/Debug/net6.0/linux-x64
RUN ./LinuxServerSetup
RUN cat logs/run.log

