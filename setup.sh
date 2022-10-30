#!/bin/sh
sed "s|@@PWD@@|$PWD|" rabbitmq.conf.in > rabbitmq.conf
if [ ! -d rabbitmq_server-3.11.2 ]
then
    curl -LO https://github.com/rabbitmq/rabbitmq-server/releases/download/v3.11.2/rabbitmq-server-generic-unix-3.11.2.tar.xz
    tar xf rabbitmq-server-generic-unix-3.11.2.tar.xz
    ./rabbitmq_server-3.11.2/sbin/rabbitmq-plugins enable rabbitmq_management rabbitmq_auth_mechanism_ssl
fi
RABBITMQ_ALLOW_INPUT=true RABBITMQ_CONFIG_FILE="$PWD/rabbitmq.conf" LOG=debug ./rabbitmq_server-3.11.2/sbin/rabbitmq-server
