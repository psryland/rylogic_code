<?php

require_once("logger.php");

// Paypal payment processor
class PayPalIpn
{
	private $testing;      // True if interacting with the sandbox site
	private $logger;       // A reference to the shared logger
	private $ipn_verified; // True if the payment has been verified

	function __construct($testing, Logger& $logger)
	{
		$this->testing = $testing;
		$this->logger = $logger;
		$this->ipn_verified = false;
	}

	// Begin the transaction
	function ProcessPost()
	{
		// Everything below here is originally from
		// http://designertuts.com/wp-content/uploads/2007/10/paypalipnphp.txt,
		// which was said to be a copy of the example code specified on the Paypal site.
		// Of course, I modified formatting and necessary logic.  Quite a lot, actually.
		$this->logger->Log("ProcessPost:\r\n".var_export($_POST, true)."\r\n");

		// Paypal POSTs HTML FORM variables to this page.
		// We must return all the variables back to PayPal unchanged and
		// add an extra parameter 'cmd 'with value '_notify-validate'
		$response = 'cmd=_notify-validate';

		// Check for magic quotes (this comes from a PayPal pdf on IPN usage - don't remember the URL):
		$magic_quotes_func_exists = function_exists('get_magic_quotes_gpc');
		//if ()
		//{
		//    $magic_quotes_func_exists = true;
		//}

		// The following variable is used for a quick, dummy check to keep from sending a
		// response unless the POST was long, meaning it probably came from PayPal:
		$numPosts = 0;

		// Go through each of the POSTed vars and add them to the variable
		foreach ($_POST as $key => $value)
		{
			$numPosts += 1;
			if ($magic_quotes_func_exists == true && get_magic_quotes_gpc() == 1)
			{
				$value = urlencode(stripslashes($value));
			}
			else
			{
				$value = urlencode($value);
			}
			$response .= "&$key=$value";
		}

		$pp_url = $this->testing ? "www.sandbox.paypal.com" : "www.paypal.com";
		
		// Post back to PayPal system to validate
		$header = "POST /cgi-bin/webscr HTTP/1.0\r\n";
		$header .= "Host: ".$pp_url."\r\n";
		$header .= "Content-Type: application/x-www-form-urlencoded\r\n";
		$header .= "Content-Length: ".strlen($response)."\r\n\r\n";

		// In a live application send it back to www.paypal.com, but during development use the PayPal sandbox.
		// Paypal Sandbox only seems to accept using ssl connections, and on port 443.
		$socket = fsockopen("ssl://".$pp_url, 443, $socket_err_num, $socket_err_str, 30);
		if (!$socket)
		{
			// HTTP ERROR Failed to connect.  Send me an email notification:
			$mail_Body = "Error from fsockopen:\r\n".$socket_err_str."\r\n\r\n".
						"Original PayPal Post Data (COULD BE BOGUS!)" .
						"\r\n\r\n";
			foreach ($_POST as $key => $value)
			{
				$value = urlencode(stripslashes($value));
				$mail_Body .= "&$key=$value" . "\r\n";
			}
			mail($myEmail, "IPN Error Noficiation: Failed to connect to PayPal", $mail_Body, "someone@somewhere.com");

			// The original code used "fwrite($fh, $socket_err_str)" for the following:
			$this->logger->Log("Socket error: " . $socket_err_str);
			return;
		}

		// Now respond to PayPal's posting. The previous 'if' statement returned
		// and terminated this if there was a socket error, so we can concentrate on what we
		// need. First, send our response to the POST, and check for verification:
		$receivedVerification = false;
		if ($numPosts > 3)
		{
			// Only send a response if it comes from PayPal.
			// The quick and dirty way to do so is see how many posts were in the original message:
			fputs($socket, $header.$response);
			$this->logger->Log("\r\nSENT:\r\n".$header.$response."\r\n\r\n");

			// And get their response.  First lines will be transaction data, finally followed by
			// 'VERIFIED' or 'UNVERIFIED' (or something).  It took a little work to find out that
			// SSL had to be used for the Sandbox, and to get it working correctly.
			// But the previous '$socket' initialization finally succeeded.
			$receivedVerification = false;
			while (!feof($socket))
			{
				$result = fgets($socket, 1024);  // Get a line of response
				$this->logger->Log("RECEIVED: ".$result."\r\n");
				if (strcmp($result, "VERIFIED") == 0)
					$receivedVerification = true;
			}
		}

		fclose($socket);

		$ret = false;
		if ($receivedVerification == false)
		{
			$this->logger->Log("\r\n\r\nINVALID TRANSACTION! (Improper PayPal response received)\r\n");
		}
		else
		{
			$this->ipn_verified = true;
			$this->logger->Log("TRANSACTION VERIFIED!\r\n");
			$ret = true;
		}
		return $ret;
	}

	// True if the payment has been verified
	function IpnVerified()
	{
		return $this->ipn_verified;
	}
}
?>