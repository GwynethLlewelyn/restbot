#!/bin/bash

if [ $# -ne 3 ] ; then
	echo "dilation-meter.sh firstname lastname password" ;
	exit 1 ;
fi
SESSION=`perl botlogin.pl "$1" "$2" "$3"`
while [ 1 ] ; do 
	DILATION=`perl bot-dilation.pl "$SESSION"` ;
	LEN=`echo "$DILATION" | wc -c` ;
	if [ $LEN -eq 1 ] ; then
		DILATION=100 ;
	fi ;
done | /sw/bin/dialog --gauge "Time Dilation" 7 80

perl botquit.pl $SESSION
