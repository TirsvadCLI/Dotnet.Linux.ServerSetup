#!/bin/bash
SCRIPTPATH="$( cd "$(dirname "$0")" >/dev/null 2>&1 ; pwd -P )"

PROJECTDIR="$(basename $SCRIPTPATH)"

JSONFILE="${SCRIPTPATH}/src/${PROJECTDIR}/obj/project.assets.json"

dotnet build --configuration Release
dotnet pack --configuration Release

# github source add dotnet nuget add source --username USER --password $GITHUBPAT --store-password-in-clear-text --name github "https://nuget.pkg.github.com/TirsvadCLI/index.json"
# GITHUBPAT is my secret key for github
dotnet nuget push "src/${PROJECTDIR}/bin/Release/$(jq -r '.project.restore.projectName' $JSONFILE).$(jq -r '.project.version' $JSONFILE).nupkg"  --api-key $GITHUBPAT --source "github"