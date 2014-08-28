<?php
// This page is linked to from the home page and by POSTing the form on this page.
// On POST from the form on this page, the entered data is validated.
// If invalid, error messages etc are displayed.
// If valid, the submit button is a paypal gif and POSTing navigates to the action.php page
require_once($rootdir."/priv/functions.php");
require_once($rootdir."/priv/product_info.php");
require_once($rootdir."/priv/user_info.php");
order_create();

function order_create()
{
	global $host;

	// Variables for this page
	$labels    = array('name'=>'Licenced To', 'email'=>'E-mail Address', 'company'=>'Company Name');
	$errors    = array('name'=>''           , 'email'=>''              , 'company'=>''            );
	$req       = array('name'=>true         , 'email'=>true            , 'company'=>false         );
	$validated = false;

	// Get the product and user info
	$product   = new ProductInfo("RyLogViewer");
	$user      = new UserInfo();

	// POST to this page means validate user input.
	if ($_SERVER["REQUEST_METHOD"] == "POST")
	{
		$validated = ValidateUserInfo($_POST, $user, $product, $errors);
		if ($validated)
		{
			// Save the validated user data in the session
			$_SESSION['product'] = serialize($product);
			$_SESSION['user'   ] = serialize($user);
		}

		// If modify is set, pretend the user data is invalid
		if (isset($_POST['modify']))
			$validated = false;
	}

	// The url the form should POST to
	$post_url = htmlspecialchars($_SERVER['PHP_SELF']);
	$action_url = "https://".$host."/order_action.php";

	echo "<div>";
	{
		// If 'confirm' was clicked and the user info is valid, go to the action page
		if ($validated && isset($_POST['confirm']))
		{
			?>
			<p>Redirecting...</p>
			<p>If this page does not redirect automatically, please click <a href='<?=$action_url?>'>here</a></p>
			<?php
			header("Location: ".$action_url);
			return;
		}

		// If invalid user info, display the 'please complete' form message
		if (!$validated)
		{
			?>
			<p class='heading1'>Purchase RyLogViewer via <img style="vertical-align:-50%" src="https://www.paypalobjects.com/en_US/i/logo/PayPal_mark_50x34.gif" alt="PayPal" /></p>
			<p>
				Please complete the form below. RyLogViewer uses a personalized product key
				generated from the information given here. Your product key will be sent to
				the email address given below on completion of the transaction.
			</p>
			<?php
		}
		// Otherwise, display the summary of entered info and comfirmation message
		else
		{
			?><p class='heading1'>Review licence holder information</p><?php
		}

		?>
		<form action='<?=$post_url?>' method='post'>
			<div class='order_form'>
			<?php

			foreach ($labels as $key=>$value)
			{
				// Add an error message above the field if there is one
				if (!empty($errors[$key]))
					echo "<span class='red'>".$errors[$key]."</span><br/>";

				// Output the Name: <input>
				echo $value.": <input type ='text' name='".$key."'";

				// Initialise with value if available
				if (!empty($user->$key))
					echo " value='".$user->$key."'";

				// Once validated, make the fields readonly
				if ($validated)
					echo " readonly='true'";

				echo "/>";

				// Indicate if the field if required or optional
				if (!$validated)
					echo $req[$key] ? " (required)" : " (optional)";

				echo "<br /><br />";
			}

			// Output the price and product description
			?>
			<table>
				<tr><th style="text-align:left">Product</th><th style="text-align:right">Price</th></tr>
				<tr>
					<td style="text-align:left">
						<?=$product->name;?><br/>
						<span style='font-size:xx-small'><?=$product->desc?></span>
					</td>
					<td style="text-align:right">
						<?=$product->price;?> <?=$product->currency?>
					</td>
				</tr>
			</table>
			<?php

			// If user data invalid, submit button is 'Continue'
			if (!$validated)
			{
				?>
				<input type="checkbox" name="eula" value="accept" onclick="document.getElementById('continue').disabled = !this.checked;">
				<span class='fine_print'>I accept that the use of RyLogViewer is subject to the following <a href='rylogviewer_eula.txt'>EULA</a>.</span><br/><br/>
				<input type='submit' id='continue' name='continue' value='Continue' disabled='disabled'/>
				<?php
			}
			else
			{
				// Otherwise, submit button is paypal link, with "please confirm"
				?>
				<p>
					Please confirm the information above is correct and, if so, continue to paypal
					to complete the transaction.
				</p>
				<input type='submit' name='modify' value='Modify Information'/>
				<input type='image' name='confirm' value='Confirm' src='/img/paypal.gif' alt="PayPal - The safer, easier way to pay online!" class='align_right'>
				<?php
			}

			?>
			</div>
		</form>
		<?php
	}
	echo "</div>";
}
