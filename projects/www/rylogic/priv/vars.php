<?php
	session_start();

	$testing = true;

	$rootdir = $_SERVER["DOCUMENT_ROOT"].'/..';
	$host    = $_SERVER['SERVER_NAME'];
	$db_name = "freew_15189448_rylogic";
	$db_user = "root";
	$db_pass = "Arct1cCommand0";

	$pp_com     = $testing ? "sandbox.paypal.com" : "paypal.com";             // PayPal host addr
	$www_pp_com = "www.".$pp_com;                                             // PayPal host addr
	$pp_user    = 'paul_api1.rylogic.co.nz';                                  // PayPal API Username
	$pp_pass    = 'YEZHXEWZEHZGBSRC';                                         // Paypal API password
	$pp_sig     = 'An5ns1Kso7MWUdW4ErQKJJJ4qi4-AV1h8vHBES.zwMPTEhFzCZCcatxH'; // Paypal API Signature

	$support_email = "support@rylogic.co.nz";
?>
