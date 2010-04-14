#!/usr/bin/perl
use XML::XPath;
use HTTP::Request::Common qw(POST);
use LWP::UserAgent;

$ua = new LWP::UserAgent;
if ( ( $#ARGV + 1 ) < 2 ) {
	print "bad args - url session groupkey\n";
	exit;
}
my $req = POST $ARGV[0] . "/get_roles/" . $ARGV[1] . "/",
	[ 'group' => $ARGV[2] ];

$res =  $ua->request($req);
if ( $res->code == 200 ) {
	print $res->content . "\n";
}
