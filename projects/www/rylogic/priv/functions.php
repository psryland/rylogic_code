<?php

// Validates user input removing hacking attempts
function SanitiseInput($data)
{
	$data = trim($data);
	$data = stripslashes($data);
	$data = htmlspecialchars($data);
	return $data;
}

// $input is an AA containing 'name','email','company' user input
// Returns an array 'validated' with sanitised versions of the user input
// 'errors' contains an error string for each failing variable
// returns true if all input fields are valid
function ValidateNameEmailCompany(&$input, &$validated, &$errors)
{
	$validated['name'   ] = "";
	$validated['email'  ] = "";
	$validated['company'] = "";

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
		$errors['name'] = "Only letters and white space allowed"; 
		$valid = false;
	}
	else
	{
		$validated['name'] = $name;
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
		$validated['email'] = $email;
	}

	// Validate company
	$company = SanitiseInput($input["company"]);
	$validated['company'] = $company;

	return $valid;
}

// Wrapper for calling a Paypal API call
function PayPalHttpPost($method_name, $api_vars)
{
	global $pp_sandbox, $pp_user, $pp_pass, $pp_sig;
	
	// Set up your API credentials, PayPal end point, and API version.
	$api_user     = urlencode($pp_user);
	$api_pass     = urlencode($pp_pass);
	$api_sig      = urlencode($pp_sig);
	$version      = urlencode('109.0');
	$api_endpoint = "https://api-3t".($pp_sandbox?".sandbox":"").".paypal.com/nvp";
	
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
function PayPalSuccessAck(&$response)
{
	return
		"SUCCESS"            == strtoupper($response["ACK"]) ||
		"SUCCESSWITHWARNING" == strtoupper($response["ACK"]);
}

?>
