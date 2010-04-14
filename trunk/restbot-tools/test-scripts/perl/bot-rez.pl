#!/usr/bin/perl
#--------------------------------------------------------------------------------
# LICENSE:
#     This file is part of the RESTBot Project.
# 
#     RESTbot is free software; you can redistribute it and/or modify it under
#     the terms of the Affero General Public License Version 1 (March 2002)
# 
#     RESTBot is distributed in the hope that it will be useful,
#     but WITHOUT ANY WARRANTY; without even the implied warranty of
#     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#     Affero General Public License for more details.
#
#     You should have received a copy of the Affero General Public License
#     along with this program (see ./LICENSING) If this is missing, please 
#     contact alpha.zaius[at]gmail[dot]com and refer to 
#     <http://www.gnu.org/licenses/agpl.html> for now.
#
# COPYRIGHT: 
#     RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
#-------------------------------------------------------------------------------
use XML::XPath;
use HTTP::Request::Common qw(POST);
use LWP::UserAgent;

$ua = new LWP::UserAgent;
if ( ( $#ARGV + 1 ) < 4 ) {
	print "bad args - url session name x,y,z [path]\n";
	exit;
}
my $req;
if ( $ARGV[4] != "" ) {
	$req = POST $ARGV[0] . "/rez_from_inventory/" . $ARGV[1] . "/",
		[ "name" => $ARGV[2] ,  "pos" => $ARGV[3] , "path" => $ARGV[4] ];
} else {
	$req = POST $ARGV[0] . "/rez_from_inventory/" . $ARGV[1] . "/",
    [ "name" => $ARGV[2] ,  "pos" => $ARGV[3] ];
}
$res =  $ua->request($req);
if ( $res->code == 200 ) {
	my $xp = XML::XPath->new(xml => $res->content);
	print $xp->getNodeText('/restbot/status') . "\n";
}
