#!/usr/bin/perl
#
# Sample Passthrough 2.0 CGI script for Parature
# Carlos M. Fernández <cfernand@sju.edu>
#
# This sample uses LDAP to authenticate the user and retrieve attributes
# (first name, last name, email address) that it then incorporates into
# the JSON dropoff message.
#
# This script uses libwww-perl 5.8, and as such has a few workarounds
# needed to get SSL support working properly with Crypt::SSLeay. Newer
# versions of libwww-perl may not need those workarounds, but I have not
# tested them.
#
# How It Works
#
# The script takes a username and password pair from an HTML form (not
# provided by this script) and checks them against LDAP. If they check out,
# it builds a JSON message, sends it to the dropoff URL, retrieves the
# dropoff reference ID, and then redirects the user to the corresponding
# instance with that reference ID.
#
# What you need to use this script:
#
# * Your SSL certificate and private key files
# * The certificate chain bundle from your certificate authority
# * The CA bundle from Parature -- procedures available at 
#   http://superuser.com/questions/97201/how-to-save-a-remote-server-ssl-certificate-locally-as-a-file
# * LDAP server's hostname, base DN, and attribute names for username,
#   email, first name, and last name
# * Your Parature server instance name, instance ID, department ID,
#   and email address for an account in the instance
# * libwww-perl 5.833 or compatible
# * Crypt::SSLeay 0.57 or compatible
# * JSON 2.15 or compatible
# * Net::LDAP 0.40 or compatible
# * CGI 3.51 or compatible
# 
# What you need to do:
# 
# * Store the SSL files in a secure location readable by the CGI
#   script
# * Update the environment variables in the BEGIN block to point
#   to those files:
#   * PERL_LWP_SSL_CA_FILE -- CA bundle for Parature's SSL certificate
#   * HTTPS_CERT_FILE -- your SSL certificate
#   * HTTPS_CA_FILE   -- your SSL CA bundle
#   * HTTPS_CA_DIR    -- location of your and Parature's CA bundles
#   * HTTPS_KEY_FILE  -- your private key
# * Update the constants with the relevant information -- see the
#   accompanying comments for a description of each of them
# * Put the file in a cgi-bin directory on your web server
# * Test!

use strict;
use warnings;
use Net::SSL ();
# Set up environment options for Net::SSL
BEGIN {
#  $Net::HTTPS::SSL_SOCKET_CLASS = "Net::SSL"; # Force use of Net::SSL
  $ENV{PERL_NET_HTTPS_SSL_SOCKET_CLASS} = "Net::SSL";
  $ENV{PERL_LWP_SSL_VERIFY_HOSTNAME}    = 1;
  $ENV{PERL_LWP_SSL_CA_FILE}            = '/path/to/server.ca-bundle';
  $ENV{HTTPS_CERT_FILE} = '/path/to/client.crt';
  $ENV{HTTPS_CA_FILE}   = '/path/to/client.ca-bundle';
  $ENV{HTTPS_CA_DIR}    = '/path/to/';
  $ENV{HTTPS_KEY_FILE}  = '/path/to/client.key';
}

use LWP::UserAgent;
use Net::LDAP;
use CGI qw(:standard);
use JSON;

# A few configuration constants
# Our LDAP server
use constant LDAPSERVER => 'ldap.example.com';
# Left side of user's DN
use constant UIDATTRIB  => 'uid';
# Right side of user's DN -- i.e., our search base
use constant LDAPBASE   => 'ou=People, o=example.com';
# Our Parature system account email address
use constant SESSEMAIL  => 'systemuser@example.com';
# URL instance name
use constant INSTANCE   => 's1';
# Our passthrough instance ID
use constant INSTANCEID => 'exampleSecPass';
# Our passthrough department ID
use constant DEPTID     => 12345;
# SSO drop-off endpoint
use constant SSOURL     => 'https://sso-mutual-auth.parature.com/ext/ref/dropoff';
# Final destination URL
use constant PORTALURL  => 'http://' . INSTANCE . '.parature.com/ics/support/security2.asp';
# Your certificate authority file
use constant SSLCAFILE  => '/path/to/server.ca-bundle';
# Your SSL certificate file
use constant SSLCRTFILE => '/path/to/client.crt';
# Your SSL private key file
use constant SSLKEYFILE => '/path/to/client.key';

my $cgi = new CGI;

# Grab the username and password from the form data
my $user = $cgi->param( "username" );
my $pass = $cgi->param( "password" );

# Build the DN with which we'll attempt the LDAP bind
my $dn = UIDATTRIB . "=" . $user . "," . LDAPBASE;

# Bind to LDAP using the built DN
my $ldap = Net::LDAP->new( LDAPSERVER ) or die "$@";
my $mesg = $ldap->bind( $dn, password => $pass );

# send out the headers and top of response page
#print "Pragma: no-cache\r\nContent-type: text/html\r\n\r\n";
print $cgi->header( -type => 'text/html' );
print "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\"\n" .
    " \"http://www.w3.org/TR/html4/loose.dtd\">\n" .
    "<html lang=\"en-US\">\n" .
    "    <head>\n" .
    "    </head>\n" .
    "    <body>\n";

# If the bind succeeded, then the user entered valid credentials
if ($mesg->error eq "Success") {
    # Get the user's email address, first name, and last name
    my $searchresult = $ldap->search(base => LDAPBASE, filter => "(" . UIDATTRIB . "=" . $user . ")");
    my $entry = $searchresult->entry ( 0 );
    my $email = $entry->get_value('mail');
    my $givenName = $entry->get_value('givenName');
    my $sn = $entry->get_value('sn');

    # Build JSON message object
    my $mesg = { subject => INSTANCEID,
                 payload => {
                     sessEmail => SESSEMAIL,
                     cEmail     => $email,
                     cUname     => $user,
                     cFname     => $givenName,
                     cLname     => $sn,
                     deptID     => DEPTID
                  }
               };
    my $jsonmsg = encode_json $mesg;

    # Send JSON object with user details
    my $ua = LWP::UserAgent->new(
        ssl_opts => {
            verify_hostname => 1,
            SSL_verify_mode => 1,
            SSL_ca_file     => SSLCAFILE,
            SSL_cert_file   => SSLCRTFILE,
            SSL_key_file    => SSLKEYFILE 
        }
    );
    $ua->default_header( 'Content-Type' => "application/json" );
    $ua->default_header( 'ping.instanceId' => INSTANCEID );

    my $response = $ua->post( SSOURL, Content => $jsonmsg );

    # Check the response from the dropoff
    if ( $response->is_success ) {
        # Extract ref ID
        my $resp = $response->content( );
        $resp =~ /{"REF":"(\w+)"}/;
        my $refID = $1;
        if ( length $refID > 0 ) {
            # Redirect user to portal URL
            print '        <form action="' . PORTALURL . '" method="post" name="UserLogin">', "\n";
            print '            <input type="hidden" name="refID" value="' . $refID . '" />', "\n";
            print '            <input type="hidden" name="instanceID" value="' . INSTANCEID . '" />', "\n";
            print '        </form>', "\n";
            print '        <script language="javascript">document.UserLogin.submit();</script>', "\n";
        }
        else {
            print '        <script language="javascript">history.back(); alert("Invalid refID received - Please try again.")</script>', "\n";
            print $jsonmsg, "\n";
            print $response->decoded_content;
        }
    }
    else {
        print '        <script language="javascript">history.back(); alert("Invalid passthrough response received - Please try again.")</script>', "\n";
        print $jsonmsg, "\n";
        print $response->decoded_content;
    }
} else {
    print '        <script language="javascript">history.back(); alert("Invalid Login - Please try again.")</script>', "\n";
}

print "    </body>\n";
print "</html>";

# Disconnect from LDAP
$ldap->unbind;

