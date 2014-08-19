<?php
// This page is called in response to a successful 'SetExpressCheckout'
// PayPal POSTs to this page on success.
session_start();
require_once($rootdir."/priv/functions.php");

// Get session variables
$product  = $_SESSION['product'];
$email    = $_SESSION['email'];
$price    = $_SESSION['price'];
$currency = $_SESSION['currency'];

// If PayPal has replied with a token and payer id, continue with the "DoExpressCheckoutPayment"
// Note: we haven't received any payment yet, just the token.
$is_pp_reply = isset($_GET["token"]) && isset($_GET["PayerID"]);
if ($is_pp_reply)
{
	$token    = SanitiseInput($_GET["token"  ]);
	$payer_id = SanitiseInput($_GET["PayerID"]);
	$api_vars =
		'&TOKEN='.urlencode($token).
		'&PAYERID='.urlencode($payer_id).
		'&PAYMENTREQUEST_0_PAYMENTACTION='.urlencode("SALE").
		'&PAYMENTREQUEST_0_CURRENCYCODE='.urlencode($currency).
		'&PAYMENTREQUEST_0_AMT='.urlencode($price).
		'&PAYMENTREQUEST_0_ITEMAMT='.urlencode($price).
		'&L_PAYMENTREQUEST_0_QTY0='.urlencode("1").
		'&L_PAYMENTREQUEST_0_AMT0='.urlencode($price).
		'&L_PAYMENTREQUEST_0_NAME0='.urlencode($product).
		'&L_PAYMENTREQUEST_0_DESC0='.urlencode($desc).
		'';
	//'&L_PAYMENTREQUEST_0_NUMBER0='.urlencode("1").
	//'&PAYMENTREQUEST_0_TAXAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_SHIPPINGAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_HANDLINGAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_SHIPDISCAMT='.urlencode("0").
	//'&PAYMENTREQUEST_0_INSURANCEAMT='.urlencode("0").

	// Execute "DoExpressCheckoutPayment" at this point to Receive payment from user.
	//$response = PayPalHttpPost('DoExpressCheckoutPayment', $api_vars);
	if (true)//PayPalSuccessAck($response))
	{
		echo
			"<div class='impact'>Success!</div>".
			"<p>Your Transaction ID : ".urldecode($response['PAYMENTINFO_0_TRANSACTIONID'])."</p>";
	
		// Sometimes payments are kept pending even when the transaction is complete.
		// We need to notify user about it and ask him manually approve the transiction.
		$payment_pending = 'Pending' == $response["PAYMENTINFO_0_PAYMENTSTATUS"];
		if ($payment_pending)
		{
			?>
			<p style="color:red">
				Transaction Complete, but payment is still pending.
				You need to manually authorize this payment in your <a target="_new" href="http://".<?php echo $pp_host;?> >Paypal Account</a>
			</p>
			<?php
		}
		else
		{
			?>
			<p class="impact">Transaction Complete!</p>;
			<p>An email containing your product key and invoice has been sent to <span class="user_email"><?php echo $email; ?></span></p>
			<?php
		}

		// We can retrive transaction details using either GetTransactionDetails or GetExpressCheckoutDetails.
		// GetTransactionDetails requires a Transaction ID, 
		// GetExpressCheckoutDetails requires Token returned by SetExpressCheckOut
		$api_vars = '&TOKEN='.urlencode($token);
		$response = PayPalHttpPost('GetExpressCheckoutDetails', $api_vars);
		if (PayPalSuccessAck($response))
		{
			// Save customer info in the database
			
			// Generate product key
			
			// Send email
			
			
			echo '<br /><b>Stuff to store in database :</b><br /><pre>';
			/*
			#### SAVE BUYER INFORMATION IN DATABASE ###
			//see (http://www.sanwebe.com/2013/03/basic-php-mysqli-usage) for mysqli usage
			
			$buyerName = $response["FIRSTNAME"].' '.$response["LASTNAME"];
			$buyerEmail = $response["EMAIL"];
			
			//Open a new connection to the MySQL server
			$mysqli = new mysqli('host','username','password','database_name');
			
			//Output any connection error
			if ($mysqli->connect_error) {
			die('Error : ('. $mysqli->connect_errno .') '. $mysqli->connect_error);
			}       
			
			$insert_row = $mysqli->query("INSERT INTO BuyerTable 
			(BuyerName,BuyerEmail,TransactionID,ItemName,ItemNumber, ItemAmount,ItemQTY)
			VALUES ('$buyerName','$buyerEmail','$transactionID','$ItemName',$ItemNumber, $ItemTotalPrice,$ItemQTY)");
			
			if($insert_row){
			print 'Success! ID of last inserted record is : ' .$mysqli->insert_id .'<br />'; 
			}else{
			die('Error : ('. $mysqli->errno .') '. $mysqli->error);
			}
			
				*/
			
			echo '<pre>';
			print_r($response);
			echo '</pre>';
		} else
		{
			echo '<div style="color:red"><b>GetTransactionDetails failed:</b>'.urldecode($response["L_LONGMESSAGE0"]).'</div>';
			echo '<pre>';
			print_r($response);
			echo '</pre>';
		}
	}
	else
	{
		?>
		<div class="error">
			<b>Error</b> : <?php echo urldecode($response["L_LONGMESSAGE0"]); ?>
		</div>
		<?php
	}
}
else
{
	?>
	<p class='error'>
		<b>Error</b> : Invalid response from Paypal
	</p>
	<p>Your account has not been charged.</p>
	<p></p>
	<?php
}
?>
<!--<div id="content">
	<p class="impact">Order Successful!</p>
	<p>Thank you once again for your purchase.</p>
	<p>
		An email containing your licence key was sent to <span class="user_email"><?=$email;?></span>.
		Please remember to check your email spam folder if you do not receive this email within a few minutes.
	</p>
</div>-->
