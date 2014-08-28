<?php
// This page is redirected to from the create page
require_once($rootdir."/priv/functions.php");
require_once($rootdir."/priv/product_info.php");
require_once($rootdir."/priv/user_info.php");
order_action();

function order_action()
{
	global $host, $www_pp_com;
	
	// Validate the session
	if (!isset($_SESSION['product']) || !isset($_SESSION['user']))
	{
		?>
		<div class='order_summary'>
			<p class='red'>Invalid session</p>
			<p>Click <a href='http://<?=$host?>/index.php'>here</a> to return to the home page</p>
		</div>
		<?php
		return;
	}

	// Get the product and user info from the session
	$product = unserialize($_SESSION['product']);
	$user    = unserialize($_SESSION['user']);

	//Parameters for SetExpressCheckout, which will be sent to PayPal
	$api_vars =
		'&METHOD=SetExpressCheckout'.
		'&RETURNURL='.urlencode("https://".$host."/order_complete.php").
		'&CANCELURL='.urlencode("https://".$host."/order_cancelled.php").
		'&ALLOWNOTE=1'.
		'&PAYMENTREQUEST_0_PAYMENTACTION='.urlencode("SALE").
		'&PAYMENTREQUEST_0_CURRENCYCODE='.urlencode($product->currency).
		'&PAYMENTREQUEST_0_AMT='.urlencode($product->price).
		'&PAYMENTREQUEST_0_ITEMAMT='.urlencode($product->price).
		'&L_PAYMENTREQUEST_0_QTY0='. urlencode("1").
		'&L_PAYMENTREQUEST_0_AMT0='.urlencode($product->price).
		'&L_PAYMENTREQUEST_0_NAME0='.urlencode($product->name).
		'&L_PAYMENTREQUEST_0_DESC0='.urlencode($product->desc).
		'&NOSHIPPING=1'.
		'&LOGOIMG='.urlencode("http://".$host."/img/logo.png"). //site logo
		'';

	// Execute the "SetExpressCheckOut" method to obtain paypal token
	$response = PayPalHttpPost('SetExpressCheckout', $api_vars);
	if (!PayPalSuccessAck($response))
	{
		// Show error message
		echo "<div class='red'>";
		if (isset($response['L_LONGMESSAGE0']))
			echo "<b>Error</b> : ".urldecode($response["L_LONGMESSAGE0"])."<br/>";
		else if (isset($response['L_SHORTMESSAGE0']))
			echo "<b>Error</b> : ".urldecode($response["L_SHORTMESSAGE0"])."<br/>";
		if (isset($response['L_ERRORCODE0']))
			echo "<b>Error Code</b> : ".urldecode($response["L_ERRORCODE0"])."<br/>";
		echo "</div>";
		return;
	}

	// Redirect to the paypal checkout site
	$paypalurl ='https://'.$www_pp_com.'/cgi-bin/webscr?cmd=_express-checkout&token='.$response["TOKEN"].'';
	?>
	<div class='order_summary'>
		<p class='heading1 green'>Contacting Paypal...</p>
		<p>If this page does not redirect automatically, please click: <a href='<?=$paypalurl?>'>PayPal Secure Checkout</a>.<p>
	</div>
	<?php
	
	// Redirect user to PayPal store with Token received.
	header('refresh:3;url='.$paypalurl);
}
?>
