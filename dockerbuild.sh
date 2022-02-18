#!/bin/bash

[ -d "/srv/programming/NugetPackages/" ] && cp -r /srv/programming/NugetPackages/* ./.localNugetFeed
docker build --pull --rm -f "Dockerfile" -t linuxserversetup:latest "."

# cleanup
shopt -s extglob
cd .localNugetFeed
rm !("README.md")
