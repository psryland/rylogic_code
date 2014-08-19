<?php
require_once($rootdir."/priv/functions.php");

if (!empty($_GET['emailto']))
{
	$emailto = SanitiseInput($_GET['emailto']);
	header('Location: mailto:'.$emailto.'@'.'rylogic.co.nz');
	exit();
}
?>
<h3>Contact</h3>
<p>For licencing, bug reporting, feature requests, and any other enquiries, please contact us at the following addresses:</p>
<div id="email_link_container">
	<b>Email:</b> <a class='email_link' href="contact.php?emailto=support">
		<img width='180' height='18' src="/img/support_email.gif" name='email_support' style="vertical-align:-5px"/>
	</a>
</div>
<div id='mailing_address'>
	<b><i>Mailing Address:</i></b><br />
	Rylogic Limited<br />
	22 Beaumont Drive<br />
	Rolleston, 7614<br />
	New Zealand<br />
</div>
