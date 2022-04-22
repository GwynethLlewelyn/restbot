#!/bin/bash
# LICENSE:
# This file is part of the RESTBot Project.
#
# Copyright (C) 2007-2008 PLEIADES CONSULTING, INC
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Affero General Public License as
# published by the Free Software Foundation, either version 3 of the
# License, or (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Affero General Public License for more details.
#
# You should have received a copy of the GNU Affero General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

# To use this script, make sure you have the following dependencies installed:
# - perl
# - dialog (Ubuntu/Debian: 'apt install dialog'; macOS: 'brew install dialog')

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
done | dialog --gauge "Time Dilation" 7 80

perl botquit.pl $SESSION
