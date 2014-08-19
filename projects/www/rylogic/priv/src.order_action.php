<?php
session_start();

// This page is called by POSTing from the order_create page
require_once($rootdir."/priv/functions.php");

if ($_SERVER["REQUEST_METHOD"] == "POST")
{
	$product  = $_SESSION['product' ];
	$desc     = $_SESSION['desc'    ];
	$price    = $_SESSION['price'   ];
	$currency = $_SESSION['currency'];
	
	//Parameters for SetExpressCheckout, which will be sent to PayPal
	$api_vars =
		'&METHOD=SetExpressCheckout'.
		'&RETURNURL='.urlencode("https://".$host."/order_complete.php").
		'&CANCELURL='.urlencode("https://".$host."/order_cancelled.php").
		'&ALLOWNOTE=1'.
		'&PAYMENTREQUEST_0_PAYMENTACTION='.urlencode("SALE").
		'&PAYMENTREQUEST_0_CURRENCYCODE='.urlencode($currency).
		'&PAYMENTREQUEST_0_AMT='.urlencode($price).
		'&PAYMENTREQUEST_0_ITEMAMT='.urlencode($price).
		'&L_PAYMENTREQUEST_0_QTY0='. urlencode("1").
		'&L_PAYMENTREQUEST_0_AMT0='.urlencode($price).
		'&L_PAYMENTREQUEST_0_NAME0='.urlencode($product).
		'&L_PAYMENTREQUEST_0_DESC0='.urlencode($desc).
		'&NOSHIPPING=1'. //set 1 to hide buyer's shipping address, in-case products that do not require shipping
		'&LOGOIMG='.urlencode("http://".$host."/img/logo.png"). //site logo
		'';
	//'&L_PAYMENTREQUEST_0_NUMBER0='.urlencode("0").
	//'&PAYMENTREQUEST_0_TAXAMT='.urlencode($TotalTaxAmount).
	//'&PAYMENTREQUEST_0_SHIPPINGAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_HANDLINGAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_SHIPDISCAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_INSURANCEAMT='.urlencode("0").
	//'&LOCALECODE=GB'. //PayPal pages to match the language on your website.
	//'&CARTBORDERCOLOR=FFFFFF'. //border color of cart

	// Execute the "SetExpressCheckOut" method to obtain paypal token
	$response = PayPalHttpPost('SetExpressCheckout', $api_vars);
	if (PayPalSuccessAck($response))
	{
		$paypalurl ='https://'.$pp_host.'/cgi-bin/webscr?cmd=_express-checkout&token='.$response["TOKEN"].'';
		echo
			"<p class='heading1 green'>Contacting PayPal...</p>".
			"<p>This page should automatically redirect to the secure PayPal site<br/>".
			"If automatic redirection fails, please click here: <a href=".$paypalurl.">PayPal Secure Checkout</a>.<p>";

		// Redirect user to PayPal store with Token received.
		header('Location: '.$paypalurl);
	}
	else
	{
		// Show error message
		echo "<div class='error'>";
		if (key_exists('L_LONGMESSAGE0',$response))
			echo "<b>Error</b> : ".urldecode($response["L_LONGMESSAGE0"])."<br/>";
		else if (key_exists('L_SHORTMESSAGE0',$response))
			echo "<b>Error</b> : ".urldecode($response["L_SHORTMESSAGE0"])."<br/>";
		if (key_exists('L_ERRORCODE0',$response))
			echo "<b>Error Code</b> : ".urldecode($response["L_ERRORCODE0"])."<br/>";
		echo "</div>";
	}
}
?>
