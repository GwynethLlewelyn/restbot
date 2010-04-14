#!/usr/bin/perl -w
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
use strict;

use XML::XPath;
use HTTP::Request::Common qw(POST);
use LWP::UserAgent;

my $url = $ARGV[0];
my $session = $ARGV[1];

my $ua = new LWP::UserAgent;
if ( ( $#ARGV + 1 ) < 1 ) {
	print "bad args - url session\n";
	exit;
}
my $req = POST $url . "invitations/" . $session . "/",
	[ "action" => "list_invitations" ];

my $res =  $ua->request($req);
if ( $res->code == 200 ) {
	my $xp = XML::XPath->new(xml => $res->content);
	if ( $xp->exists('/restbot') ) {
		print "Valid restjunk\n";
	} else {
		print "Invalid restjunk\n";
		die;
	}
	my $invitations = $xp->find('/restbot/invitations/invite');
	print "Invites - " . $invitations->size() . "\n";
	foreach my $invite ( $invitations->get_nodelist) {
	#	print XML::XPath::XMLParser::as_string($invite) . "\n";
		print "Invited to " . getGroupName($invite->find('group')) . " by " . $invite->find('inviter')  ."\n";
		my $answer = '';
		while ( $answer ne 'y' && $answer ne 'n' ) {
			print "Do you wish to accept ? (y/n) ";
			chomp($answer = <STDIN>);
		}
		if ( $answer eq 'y' ) {
			my $req2 = POST $url . "/invitations/" . $session . "/",
				[ "action" => "accept_invitation", "inviteid" => $invite->find('key') ];
			my $res2 = $ua->request($req2);
		} elsif ( $answer eq 'n' ) {
			my $req2 = POST $url . "/invitations/" . $session . "/",
				[ "action" => "decline_invitation", "inviteid" => $invite->find('key') ];
			my $res2 = $ua->request($req2);
		}
	}
}

sub getGroupName
{
	my $groupkey = $_[0];
	my $req = POST $url . "/get_group_profile/" . $session . "/",
		[ "group" => "$groupkey" ];
	my $res = $ua->request($req);
	if ( $res->code == 200 ) {
		my $xp = XML::XPath->new(xml => $res->content);
		return $xp->getNodeText('/restbot/groupprofile/name');
	}
	return undef;
}
