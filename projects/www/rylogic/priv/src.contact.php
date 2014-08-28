<?php
require_once($rootdir."/priv/functions.php");
contact();

function contact()
{
	if (isset($_GET['emailto']))
	{
		$emailto = SanitiseInput($_GET['emailto']);
		header('Location: mailto:'.$emailto.'@'.'rylogic.co.nz');
		return;
	}

	?>
	<div>
		<h3>Contact</h3>
		<p>
			For licencing, bug reporting, feature requests, and any other enquiries,
			please contact Rylogic Limited at the following addresses:
		</p>
		<?php SupportEmail(); ?>
		<div id='mailing_address'>
			<b><i>Mailing Address:</i></b><br />
			Rylogic Limited<br />
			22 Beaumont Drive<br />
			Rolleston, 7614<br />
			New Zealand<br />
		</div>
	</div>
	<?php
}
