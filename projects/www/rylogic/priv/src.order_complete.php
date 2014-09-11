<?php

//hack
$_GET['token']='1234';
$_GET['PayerID']='abcd';

// This page is returned to from the PayPal checkout site
require_once($rootdir."/priv/functions.php");
require_once($rootdir."/priv/product_info.php");
require_once($rootdir."/priv/user_info.php");
order_complete();

function order_complete()
{
	global $www_pp_com;

	$is_pp_reply = isset($_GET["token"]) && isset($_GET["PayerID"]);
	if (!$is_pp_reply)
	{
		?>
		<div class="order_summary">
			<p class='red'><b>Error</b> : Invalid response from PayPal.</p>
			<p>
				Your account has not been charged. Please attempt the
				transaction again or contact support for assistance.
			</p>
			<?php SupportEmail(); ?>
		</div>
		<?php
		return;
	}

	// If PayPal has replied with a token and payer id, continue with the "DoExpressCheckoutPayment"
	// Note: we haven't received any payment yet, just the token.
	$token    = SanitiseInput($_GET["token"  ]);
	$payer_id = SanitiseInput($_GET["PayerID"]);

	// Get session variables
	$product = unserialize($_SESSION['product']);
	$user    = unserialize($_SESSION['user']);

	$api_vars =
		'&TOKEN='.urlencode($token).
		'&PAYERID='.urlencode($payer_id).
		'&PAYMENTREQUEST_0_PAYMENTACTION='.urlencode("SALE").
		'&PAYMENTREQUEST_0_CURRENCYCODE='.urlencode($product->currency).
		'&PAYMENTREQUEST_0_AMT='.urlencode($product->price).
		'&PAYMENTREQUEST_0_ITEMAMT='.urlencode($product->price).
		'&L_PAYMENTREQUEST_0_QTY0='.urlencode("1").
		'&L_PAYMENTREQUEST_0_AMT0='.urlencode($product->price).
		'&L_PAYMENTREQUEST_0_NAME0='.urlencode($product->name).
		'&L_PAYMENTREQUEST_0_DESC0='.urlencode($product->desc).
		'';

	// Execute "DoExpressCheckoutPayment" at this point to Receive payment from user.
	//$do_payment_result = PayPalHttpPost('DoExpressCheckoutPayment', $api_vars);
	$do_payment_result = array(//hack
		'ACK'=>'Success',
		'PAYMENTINFO_0_PAYMENTSTATUS'=>'Complete',
		'PAYMENTINFO_0_TRANSACTIONID'=>'123456789',
		);
	if (!PayPalSuccessAck($do_payment_result))
	{
		?>
		<div class="order_summary">
			<p class='red'><b>Error</b> : Transaction failed.</p>
			<p><?=urldecode($do_payment_result["L_LONGMESSAGE0"])?></p>
			<p>
				The transaction was not completed. Your account has not been charged.
				Please attempt the transaction again or contact support for assistance.
			</p>
			<?php SupportEmail(); ?>
		</div>
		<?php
		return;
	}

	// Order complete!
	// We can retrive transaction details using either GetTransactionDetails,
	// or GetExpressCheckoutDetails. GetTransactionDetails requires a Transaction ID, 
	// GetExpressCheckoutDetails requires Token returned by SetExpressCheckOut
	$api_vars = '&TOKEN='.urlencode($token);
	//$payment_details = PayPalHttpPost('GetExpressCheckoutDetails', $api_vars);
	$payment_details = array(//hack
		'ACK'=>'Success',
		'PAYMENTINFO_0_PAYMENTSTATUS'=>'Complete',
		'PAYMENTINFO_0_TRANSACTIONID'=>'123456789',
		);
	if (!PayPalSuccessAck($payment_details))
	{
		?>
		<div class="order_summary">
			<p class='red'><b>Error</b> : Access transaction details failed.</p>
			<p><?=urldecode($payment_details["L_LONGMESSAGE0"])?></p>
			<p>
				The transaction was completed, but there was an error while
				accessing the payment information. Please email support for
				assistance.
			</p>
			<?php SupportEmail(); ?>
		</div>
		<?php
		return;
	}

	// Sometimes payments are kept pending even when the transaction is complete.
	// We need to notify the user about it and ask them to manually approve the transaction.
	$transaction_id = urldecode($payment_details['PAYMENTINFO_0_TRANSACTIONID']);
	$payment_status = urldecode($do_payment_result["PAYMENTINFO_0_PAYMENTSTATUS"]);
	$transaction_id = urldecode($do_payment_result['PAYMENTINFO_0_TRANSACTIONID']);
	$payment_pending = strcasecmp('pending', $payment_status) == 0;

	// Add the user data to the database
	$res = AddCustomerToDB($user, $product, $transaction_id, !$payment_pending);
	if (!$res['ok'] && !$res['duplicate'])
	{
		?>
		<div class="order_summary">
			<p class='red'><b>Error</b> : Recording customer details failed.</p>
			<p><?=$res['msg']?></p>
			<p>
				An error has occurred while attempted to record your customer
				details in our database. Please email support for assistance.
			</p>
			<?php SupportEmail(); ?>
		</div>
		<?php
		return;
	}

	// If payment isn't pending, ship the product
	if (!$payment_pending)
	{
		$res = SendLicenceEmail($user, $product);
		if (!$res['ok'])
		{
			?>
			<div class="order_summary">
				<p class='red'><b>Error</b> : Failed to send licence information email.</p>
				<p><?=$res['msg']?></p>
				<p>
					An error has occurred while attempted to generate and send your licence
					information. Your purchase has been recorded in our database but there
					appears to be a problem generating the licence email. Please contact
					support for assistance.
				</p>
				<?php SupportEmail(); ?>
			</div>
			<?php
			return;
		}
	}
	
	?>
	<div class='order_summary'>
		<div class='impact green'>Success!</div>

		<p>Your Transaction ID : <?=$transaction_id?></p>

		<?php if ($payment_pending): ?>
			<p class="red"><b>Payment Pending</b></p>
			<p>You need to manually authorize this payment in your <a target='_new' href='http://<?=$www_pp_com;?>'>Paypal Account</a></p>
		<?php else: ?>
			<p class="green"><b>Transaction Complete!</b></p>
			<p class="header1"><b>Thank you for purchasing <?=$product->name?>!</b></p>
			<p>
				An email containing your licence information, invoice, and instructions
				has been sent to <span class="user_email"><?=$user->email?></span>.
			</p>
			<p>
				You should receive this email within a few minutes (please remember to
				check your email spam folder). If you have any problems regarding this
				transaction or with the licence information, please contact support.
			</p>
			<?php SupportEmail(); ?>
		<?php endif;?>

	</div>
	<?php
}
?>

