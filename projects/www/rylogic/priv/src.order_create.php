<?php
session_start();
require_once($rootdir."/priv/functions.php");

$product   = "RyLogViewer";
$names     = array('name'=>'Licenced To', 'email'=>'E-mail Address', 'company'=>'Company Name');
$input     = array('name'=>''           , 'email'=>''              , 'company'=>''            );
$errors    = array('name'=>''           , 'email'=>''              , 'company'=>''            );
$req       = array('name'=>true         , 'email'=>true            , 'company'=>false         );
$validated = false;

$link = new mysqli($host,$db_user,$db_pass,$db_name);
if ($link->connect_error) die('connect error ('.$link->connect_errno.') '.$link->connect_error);
$desc     = $link->query("select Description from products where Name='".$product."' limit 1") or die("error".mysqli_error($link));
$desc     = $desc->fetch_assoc()['Description'];
$price    = $link->query("select Price from products where Name='".$product."' limit 1") or die("error".mysqli_error($link));
$price    = $price->fetch_assoc()['Price'];
$currency = $pp_currency_code;
$link->close();

// The form on this page POSTs back to this page if 'validated' is not true.
// Once validated, the form POSTs to the order_action.php page.
if ($_SERVER["REQUEST_METHOD"] == "POST")
{
	$validated = ValidateNameEmailCompany($_POST, $input, $errors);
	if ($validated)
	{
		// Save the user data in the session
		$_SESSION['product' ] = $product;
		$_SESSION['desc'    ] = $desc;
		$_SESSION['price'   ] = $price;
		$_SESSION['currency'] = $pp_currency_code;
		foreach ($input as $key=>$value)
			$_SESSION[$key] = $value;
	}
}

// The url the form should POST to
$post_url = $validated ? "order_action.php" : htmlspecialchars($_SERVER['PHP_SELF']);

?>
<div>
	<?php if (!$validated): ?>
	<p class='heading1'>Purchase RyLogViewer via <img style="vertical-align:-50%" src="https://www.paypalobjects.com/en_US/i/logo/PayPal_mark_50x34.gif" alt="PayPal" /></p>
	<p>
		Please complete the form below. RyLogViewer uses a personalized product key
		generated from the information given here. Your product key will be sent to
		the email address given below on completion of the transaction.
	</p>
	<?php else: ?>
	<p class='heading1'>Review licence holder information</p>
	<?php endif; ?>

	<form action='<?=$post_url?>' method='post'>
		<div class='order_form'>

			<?php
			foreach ($names as $key=>$value)
			{
				// Add an error message above the field if there is one
				if (!empty($errors[$key]))
					echo "<span class='error'>".$errors[$key]."</span><br/>";

				// Output the Name: <input>
				echo $value.": <input type ='text' name='".$key."'";

				// Initialise with value if available
				if (!empty($input[$key]))
					echo " value='".$input[$key]."'";

				// Once validated, make the fields readonly
				if ($validated)
					echo " readonly='true'";

				echo "/>";

				// Indicate if the field if required or optional
				if (!$validated)
					echo $req[$key] ? " (required)" : " (optional)";

				echo "<br /><br />";
			}
			?>

			<!-- Output the price and product description -->
			<table>
				<tr><th style="text-align:left">Product</th><th style="text-align:right">Price</th></tr>
				<tr>
					<td style="text-align:left">
						<?=$product;?><br/>
						<span style='font-size:xx-small'><?=$desc?></span>
					</td>
					<td style="text-align:right">
						<?=$price;?> <?=$currency?>
					</td>
				</tr>
			</table>
		
			<?php if (!$validated): ?>

				<input type="checkbox" name="eula" value="accept" onclick="document.getElementById('continue_btn').disabled = !this.checked;">
				<span class='fine_print'>I accept that the use of RyLogViewer is subject to the following <a href='rylogviewer_eula.txt'>EULA</a>.</span>
				<br/><br/>

				<input type='submit' id='continue_btn' value='Continue' disabled='disabled'/>

			<?php else: ?>

				<p>
					Please confirm the information above is correct and, if so, continue to paypal
					to complete the transaction.
				</p>
				<a href="/order_create.php">Edit Information</a>
				<input class='align_right' type="image" src="/img/paypal.gif" name="submit" alt="PayPal - The safer, easier way to pay online!">

			<?php endif; ?>
		</div>
	</form>
</div>
