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
use Data::Dumper;
use HTTP::Request::Common qw(POST);
use LWP::UserAgent;
use Digest::MD5 qw(md5_hex);

$ua = new LWP::UserAgent;
if ( ( $#ARGV + 1 ) < 4 ) {
	print "bad args - url firstname lastname password\n";
	exit;
}
my $req = POST $ARGV[0] . '/establish_session/pass/',
	[ 'first' => $ARGV[1], 'last' => $ARGV[2], 'pass' =>  md5_hex($ARGV[3]) ];

$res =  $ua->request($req);
if ( $res->code == 200 ) {
	my $xp = XML::XPath->new(xml => $res->content);
	print $xp->getNodeText('/restbot/success/session_id') . "\n";
#	print $res->content . "\n";
}
