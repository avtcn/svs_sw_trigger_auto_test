#!/usr/bin/bash
while true
do
	# List bmp files to be deleted
	find . -mmin 1 -name "*.bmp" 
	# Remove bmp files old than 1 minute
	find . -mmin 1 -name "*.bmp" -delete

	sleep 10s 
done
