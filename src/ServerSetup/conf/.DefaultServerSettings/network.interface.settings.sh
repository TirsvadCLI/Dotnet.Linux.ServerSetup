#!/bin/bash

IPV6=$(ip addr show dev eth0 | sed -e's/^.*inet6 \([^ ]*\)\/.*$/\1/;t;d')
# ip -6 addr | grep 'scope global' | grep -oP '(?<=inet6\s)[\da-f:]+' ## WORKS ON CONTABO VPS
# ip -6 addr | grep -oP '(?<=inet6\s)[\da-f:]+'
#cat /sys/module/ipv6/parameters/disable
printf """
""" > /etc/network/interface