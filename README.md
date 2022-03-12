# Tirsvad CMS - Linux Server Setup

Quick webserver setup.

## Getting Started

Need a server with debian linux compatibel distibution and root access.

I am using a Linode VP server account. Get one here from 5$ a month <https://www.linode.com/?r=a60fb437acdf27a556ec0474b32283e9661f2561>

### First step

Get this from https://www.nuget.org/packages/TirsvadCLI.Linux.ServerSetup/

or get the source at https://github.com/TirsvadCLI/Linux.ServerSetup and compile it.

#### debian

    apt-get update
    locale-gen && export LC_ALL="en_US.UTF-8" && apt-get -y install curl

Default server setup

## Settings

### Descriptions of settings.yaml file

File is with comments. Easy to go with.

## Features

### TODO

- Command line parse
- Server Configuratio
  - Default config example
    - Load from external source at github
  - Read custom configuration file
    - Optional load from external source github, bitbucket and others
- Hardness server
  - ssh
    - option remove password login and root login
  - firewall enabled (nftables)
  - Fail2ban
  - optional
    - create a user with sudo priviliged
- Nginx
  - compiled edition with RTMP for live stream / broadcasting
  - stunnel for RTPMS workaround. Facebook stream using secure connection via port 443.
- Certbot (LetsEncrypt)
  - adding ssl certificate
- rtmp user access
  - access right for yt, fb and others streaming services
- Easy create configuration from console application

### Done

- OS user and groups
  - Create user and groups in Linux
  - Add superuser (sudo)
