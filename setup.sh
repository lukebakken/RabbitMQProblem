#!/bin/sh
sed "s|@@PWD@@|$PWD|" rabbitmq.conf.in > rabbitmq.conf
