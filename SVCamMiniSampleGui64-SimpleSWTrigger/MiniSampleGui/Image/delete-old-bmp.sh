#!/usr/bin/bash
while true
do
	find . -mmin 1 -name "*.bmp" 
	find . -mmin 1 -name "*.bmp" -delete
	sleep 10s
done
