<?php
set_include_path(get_include_path().PATH_SEPARATOR.$rootdir."/phpseclib0.3.7");
//set_include_path(get_include_path().PATH_SEPARATOR."phpseclib0.3.7");
require_once($rootdir."/phpseclib0.3.7/crypt/rsa.php");
require_once($rootdir."/phpmailer/phpmailerautoload.php");
require_once($rootdir."/priv/vars.php");
require_once($rootdir."/priv/product_info.php");
require_once($rootdir."/priv/user_info.php");
require_once($rootdir."/priv/licence_info.php");

// Validates user input removing hacking attempts
function SanitiseInput($data)
{
	$data = trim($data);
	$data = stripslashes($data);
	$data = htmlspecialchars($data);
	return $data;
}

// Output the support email link
function SupportEmail()
{
	echo
	"<div class='email_link_container'>
		<b>Email:</b> <a class='email_link' href='contact.php?emailto=support'>
			<img width='180' height='18' src='/img/support_email.gif' style='vertical-align:-5px'/>
		</a>
	</div>";
}

// Checks for 'user' in the 'product_name' database.
// Returns info about whether they are a customer, paid or not.
function CheckCustomer($user_email, $product_name)
{
	global $host, $db_user, $db_pass, $db_name;

	$res = array(
		'ok'          => true,
		'msg'         => "",
		'is_customer' => false,
		'paid'        => false);

	// Open a link to the db
	$link = new mysqli($host,$db_user,$db_pass,$db_name);
	if ($link->connect_error)
	{
		$res['ok'] = false;
		$res['msg'] = 'Database connection error ('.$link->connect_errno.') '.$link->connect_error;
		return $res;
	}

	// Query for the user email
	$q = $link->query("select count(*) from ".$product_name." where Email = '".$user_email."'");
	$r = $q->fetch_array();

	// See if the customer is unique in the database
	if ($r['count(*)'] == "0")
	{
		$res['msg'] = "No record found for ".$user_email.".";
		return $res;
	}
	if ($r['count(*)'] != "1")
	{
		$res['ok'] = false;
		$res['msg'] = "Multiple records found matching ".$user_email.".";
		return $res;
	}
	$res['is_customer'] = true;
	
	// See if the customer has paid
	$q = $link->query("select count(*) from ".$product_name." where Email = '".$user_email."' and PaidDate is not NULL");
	$r = $q->fetch_array();
	if ($r['count(*)'] == "1")
	{
		$res['paid'] = true;
	}
	
	return $res;
}

// $input is an AA containing 'name','email','company' user input
// Returns an array 'validated' with sanitised versions of the user input
// 'errors' contains an error string for each failing variable
// returns true if all input fields are valid
function ValidateUserInfo(array $input, UserInfo $user, ProductInfo $product, array& $errors)
{
	$user->name    = "";
	$user->email   = "";
	$user->company = "";
	$errors['name'   ] = "";
	$errors['email'  ] = "";
	$errors['company'] = "";

	$valid = true;

	// Validate name
	$name = SanitiseInput($input["name"]); 
	if (empty($name))
	{
		$errors['name'] = "Name is required";
		$valid = false;
	}
	else if (!preg_match("/^[a-zA-Z ]*$/", $name))
	{
		$errors['name'] = "Only letters and white space are allowed"; 
		$valid = false;
	}
	else if (mb_strlen($name) > UserInfo::MaxFieldLength)
	{
		$errors['name'] = "Name too long. Limit ".UserInfo::MaxFieldLength." characters"; 
		$valid = false;
	}
	else
	{
		$user->name = $name;
	}

	// Validate email
	$email = SanitiseInput($input["email"]);
	if (empty($email))
	{
		$errors['email'] = "Email is required";
		$valid = false;
	}
	else if (!filter_var($email, FILTER_VALIDATE_EMAIL))
	{
		$errors['email'] = "Invalid email format"; 
		$valid = false;
	}
	else
	{
		$r = CheckCustomer($email, $product->name);
		if ($r['is_customer'])
		{
			$errors['email'] = "Email address already in use";
			$valid = false;
		}
		else
		{
			$user->email = $email;
		}
	}

	// Validate company
	$company = SanitiseInput($input["company"]);
	if (empty($company))
	{}
	else if (mb_strlen($company) > UserInfo::MaxFieldLength)
	{
		$errors['company'] = "Company name too long. Limit ".UserInfo::MaxFieldLength." characters";
		$valid = false;
	}
	else
	{
		$user->company = $company;
	}

	return $valid;
}

// Wrapper for calling a Paypal API call
function PayPalHttpPost($method_name, $api_vars)
{
	global $pp_com, $pp_user, $pp_pass, $pp_sig;
	
	// Set up your API credentials, PayPal end point, and API version.
	$api_user     = urlencode($pp_user);
	$api_pass     = urlencode($pp_pass);
	$api_sig      = urlencode($pp_sig);
	$version      = urlencode('109.0');
	$api_endpoint = "https://api-3t.".$pp_com."/nvp";
	
	// Set the curl parameters.
	$ch = curl_init();
	curl_setopt($ch, CURLOPT_URL, $api_endpoint);
	curl_setopt($ch, CURLOPT_VERBOSE, 1);
	curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, FALSE); // Turn off the server and peer verification (TrustManager Concept).
	curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, FALSE);
	curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($ch, CURLOPT_POST, 1);

	// Set the request as a POST FIELD for curl.
	// Set the API operation, version, and API signature in the request.
	$nvpreq = "METHOD=$method_name&VERSION=$version&PWD=$api_pass&USER=$api_user&SIGNATURE=$api_sig"."$api_vars";
	curl_setopt($ch, CURLOPT_POSTFIELDS, $nvpreq);

	// Get response from the server.
	$response = curl_exec($ch);
	if(!$response)
	{
		exit("$method_name failed: ".curl_error($ch).'('.curl_errno($ch).')');
	}

	// Extract the response details.
	$response_array = array();
	foreach (explode("&",$response) as $i => $value)
	{
		$pair = explode("=", $value);
		if (sizeof($pair) > 1)
			$response_array[$pair[0]] = $pair[1];
	}

	// Validate the response
	if (sizeof($response_array) == 0 || !array_key_exists('ACK', $response_array))
	{
		exit("Invalid HTTP Response for POST request($nvpreq) to $api_endpoint.");
	}

	// Return the response
	return $response_array;
}

// Tests 'response' for 'SUCCESS' or 'SUCCESSWITHWARNING'
function PayPalSuccessAck(array $response)
{
	return
		"SUCCESS"            == strtoupper($response["ACK"]) ||
		"SUCCESSWITHWARNING" == strtoupper($response["ACK"]);
}

// Add customer data to the database
// Returns true on success, or [false, error message]
function AddCustomerToDB(UserInfo $user, ProductInfo $product, $transaction_id, $paid)
{
	global $host, $db_user, $db_pass, $db_name;
	$res = array(
		'ok' => true,
		'msg' => "",
		'duplicate' => false);
	
	// Open a link to the db
	$link = new mysqli($host,$db_user,$db_pass,$db_name);
	if ($link->connect_error)
	{
		$res['ok'] = false;
		$res['msg'] = 'Database connection error ('.$link->connect_errno.') '.$link->connect_error;
		return $res;
	}

	// Check if already there, duplicates should have been prevented before here
	$q = $link->query("select count(*) from ".$product->name." where Email = '".$user->email."'");
	$r = $q->fetch_array();
	if ($r['count(*)'] != '0')
	{
		$res['ok'] = false;
		$res['msg'] = "Email address already exists in the database";
		$res['duplicate'] = true;
		return $res;
	}

	// Save customer info in the database
	$now = date('Y-m-d H:i:s');
	$record = array(
		"Id"            => "0",
		"Customer"      => "'".$user->name."'",
		"Email"         => "'".$user->email."'",
		"Company"       => "'".$user->company."'",
		"TransactionId" => "'".$transaction_id."'",
		"OrderDate"     => "'".$now."'",
		"PaidDate"      => $paid ? "'".$now."'" : 'NULL',
		"Price"         => "'".$product->price."'",
		"Currency"      => "'".$product->currency."'",
		);

	$sql =
		"INSERT INTO ".$product->name." ".
		"(".implode(",",array_keys($record)).") ".
		"VALUES ".
		"(".implode(",",array_values($record)).")";
	$r = $link->query($sql);
	if (!$r)
	{
		$res['ok'] = false;
		$res['msg'] = "Error (".$link->connect_errno.") ".$link->connect_error.". Failed to insert customer details into database";
		return $res;
	}

	return $res;
}

// Generates and sends an email to 'user' containing
// their licence info. Returns true on success or [false, error message]
function SendLicenceEmail(UserInfo $user, ProductInfo $product)
{
	global $support_email, $testing;
	$res = array(
		'ok' => true,
		'msg' => "");
	
	// Check that the user is a paying customer for this product
	$cust = CheckCustomer($user->email, $product->name);
	if (!$cust['ok'])
	{
		$res['ok'] = false;
		$res['msg'] = $cust['msg'];
		return $res;
	}
	if (!$cust['is_customer'])
	{
		$res['ok'] = false;
		$res['msg'] = "Customer information invalid. ".$cust['msg'];
		return $res;
	}
	if (!$cust['paid'])
	{
		$res['ok'] = false;
		$res['msg'] = "Payment is still pending for this licence";
		return $res;
	}

	// Generate the licence info
	$lic = new LicenceInfo($user, $product);

	// Generate and send the email
	try
	{
		$mailer = new PHPMailer;
		
		// Use gmail smtp for testing
		if ($testing)
		{
			$mailer->SMTPDebug = false;
			$mailer->isSMTP();                        // Set mailer to use SMTP
			$mailer->Host = 'smtp.gmail.com';         // Specify main and backup SMTP servers
			$mailer->Port = 587;                      // TCP port to connect to
			$mailer->SMTPSecure = 'tls';              // Enable TLS encryption, `ssl` also accepted
			$mailer->SMTPAuth = true;                 // Enable SMTP authentication
			$mailer->Username = 'psryland@gmail.com'; // SMTP username
			$mailer->Password = 'fpxvrxpwuacdvhar';   // SMTP password
		}
		
		$mailer->From = $support_email;
		$mailer->FromName = 'RyLogViewer Support';
		$mailer->addAddress($testing ? "paul@rylogic.co.nz" : $user->email, $user->name);     // Add a recipient
		$mailer->addReplyTo($support_email, 'Rylogic Ltd Support');

		$mailer->WordWrap = 70; // Set word wrap to 50 characters
		$mailer->isHTML(true); // Set email format to HTML

		$mailer->Subject = "Your ".$product->name." licence information!";
		$mailer->Body = GenerateLicenceEmailBodyHtml($user, $product, $lic);
		$mailer->AltBody = GenerateLicenceEmailBodyPlain($user, $product, $lic);

		// Send the email
		if (!$mailer->send())
		{
			$res['ok'] = false;
			$res['msg'] = "Licence email could not be sent. Mailer Error: ".$mailer->ErrorInfo;
			return $res;
		}
	}
	catch (Exception $ex)
	{
		$res['ok'] = false;
		$res['msg'] = "Failed to send licence email.<br/>".$ex->getMessage();
		return $res;
	}
	
	return $res;
}

// Generates an html email body containing the licence info
function GenerateLicenceEmailBodyHtml(UserInfo $user, ProductInfo $product, $lic)
{
	return
"<p><b>Thank you for purchasing ".$product->name."!</b></p>
<p>Please copy and paste the following text into the Activation dialog within ".$product->name."</p>
<p>".print_r($lic)."</p>
<p>todo...</p>";
}

// Generates an plain text email body containing the licence info
function GenerateLicenceEmailBodyPlain(UserInfo $user, ProductInfo $product, $lic)
{
	return
"Thank you for purchasing ".$product->name."!\r\n
Please copy and paste the following text into the Activation dialog within ".$product->name.".
".$lic->saveXML()."
todo...";
}
?>

