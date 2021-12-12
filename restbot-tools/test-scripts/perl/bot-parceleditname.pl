#!/usr/bin/perl
#--------------------------------------------------------------------------------
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
#-------------------------------------------------------------------------------
use XML::XPath;
use HTTP::Request::Common qw(POST);
use LWP::UserAgent;

$ua = new LWP::UserAgent;
if ( ( $#ARGV + 1 ) < 3 ) {
	print "bad args - url session new_name\n";
	exit;
}
my $req = POST $ARGV[0] . "/parcel_modify/" . $ARGV[1] . "/",
	[ "name" => $ARGV[2] ];
$res =  $ua->request($req);
if ( $res->code == 200 ) {
#	my $xp = XML::XPath->new(xml => $res->content);
#	print $xp->getNodeText('/restbot/name') . "\n";
	print $res->content;
}
