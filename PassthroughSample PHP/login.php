<html>
    <head>
        <meta charset="UTF-8">
        <title>Parature Passthrough 2.0</title>
    </head>
    <body>
        <?php
        error_reporting(E_ALL);



		$privateKey = '/path/to/private-nopwd.key';
        $publicKey = '/path/to/publiccert.pem';
		$portalURL = 'https://supportcenteronline.com/ics/support/security2.asp';
        /* Then check the paths are readable */
        if (!is_readable($privateKey)) {
            die('Can\'t access $privateKey');
        }
        if (!is_readable($publicKey)) {
            die('Can\'t access $publicKey');
        }
        

        /* Now set non-changing parameters */
        $stepOneURL = 'https://sso-mutual-auth.parature.com/ext/ref/dropoff';
        $instanceId = 'mySecPass';
		//a valid CSR email address. preferably a CSR account designated for performing passthrough operations
        $sessEmail = 'api@yourCompany.com';


        /* Now build the JSON for Step One */
        $myArray = array(
            'subject' => $instanceId,
            'payload' => array(
                'sessEmail' => $sessEmail,
				//Pass all the values you passed in the past, as each persons use case is different.
                'cEmail' => 'userEmail@email.com',
                'cUname' => 'chappy',
                'cStatus' => 'REGISTERED',
                'cFname' => 'Charlie',
                'cLname' => 'Chaplin',
                //this is needed for new passthrough, might not be passed in the old passthrough
                'deptID' => 825
            ),
        );
        $myJson = json_encode($myArray);
        //echo '<p>JSON: ' . $myJson . '</p>';


        /* ...then assemble and send the cURL for Step One */
        $ch = curl_init($stepOneURL);

        curl_setopt($ch, CURLOPT_POSTFIELDS, $myJson);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true); // return results in a string
        curl_setopt($ch, CURLOPT_HTTPHEADER, array(
            'Content-Type: application/json',
            'Content-Length: ' . strlen($myJson),
            'ping.instanceId: ' . $instanceId,
                )
        );

        /* These can help with debugging but are not needed for production */
        //curl_setopt($ch, CURLOPT_VERBOSE, true);
        //curl_setopt($ch, CURLOPT_CERTINFO, true);

        /* Set Verify Host to True to prove that we are who we say we are */
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, '1');
        curl_setopt($ch, CURLOPT_SSLCERT, $publicKey);
        curl_setopt($ch, CURLOPT_SSLKEY, $privateKey);

		/* Make sure we're using TLS not SSLv2 or SSLv3 */
		curl_setopt($ch, CURLOPT_SSLVERSION, CURL_SSLVERSION_TLSv1);
		
        $response = curl_exec($ch);

        if (!$response) {
            die ("<p>Curl Error " . curl_errno($ch) . "</p>\r\n<p>" . curl_error($ch) . "</p>\r\n");
        } 


        /* ...and decode the JSON that comes back */
        $responseArray = json_decode($response, true);
        //echo json_last_error_msg();
        //echo var_dump($responseArray) ;
        $refID = $responseArray['REF'];
        //echo "<p>refID: $refID</p>\r\n"


        /* Now Step Two - redirect to portal */
        ?>
        <form name="loginform" method="post" action="<?php echo $portalURL; ?>">
            <input type="hidden" name="refID" value="<?php echo $refID; ?>"/>
            <input type="hidden" name="instanceID" value="<?php echo $instanceId; ?>"/>
        </form>
        <script type="text/javascript">
            document.loginform.submit();
        </script>

    </body>
</html>
