<?php
require_once($rootdir."/priv/functions.php");
require_once($rootdir."/priv/user_info.php");
require_once($rootdir."/priv/product_info.php");
require_once($rootdir."/priv/licence_info.php");
header('refresh:3;url=http://localhost/index.php');

$user = new UserInfo;
$user->name = "Paul Ryland";
$user->email= "psryland@yahoo.co.nz";
$user->company = "Rylogic Limited";
$product = new ProductInfo("RyLogViewer");

$lic = new LicenceInfo($user, $product);
$body= GenerateLicenceEmailBodyHtml($user, $product, $lic);
echo wordwrap($body, 70,"<br/>",true);

?>
<p>Buy RyLogViewer Now!</p>
<div>
	<a href='https://<?=$host?>/order_create.php' class='buy_button'>Buy Now!</a>
</div>
<img src='img/main.png' alt='Main UI' />

<h3>ToDo</h3>
<p>encrypt email address in db</p>
<p>generate product key</p>
<p>use the logger class</p>
