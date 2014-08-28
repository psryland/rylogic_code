<?php

	class LicenceInfo
	{
		public $fields; // The currency of '$price'
	
		// Generates RyLogViewer licence data for the given user data and product.
		// Returns an xml object populated with the licence data.
		function __construct(UserInfo $user, ProductInfo $product)
		{
			global $rootdir;

			// Join the user data together and salt.
			$user_details =
				$user->name   ."\n".
				$user->email  ."\n".
				$user->company."\n".
				$product->name."\n".
				"Rylogic Limited Is Awesome";

			// Create the RSA crypto service
			$rsa = new Crypt_RSA();
			$rsa->loadKey(file_get_contents($rootdir.'/priv/private_key.xml'), CRYPT_RSA_PRIVATE_FORMAT_XML);
			$rsa->setHash('sha1');
			$rsa->setEncryptionMode(CRYPT_RSA_ENCRYPTION_PKCS1);
			$rsa->setSignatureMode(CRYPT_RSA_ENCRYPTION_PKCS1);

			// Create the signature
			$activation_code = base64_encode($rsa->sign($user_details));

			$fields = array(
				'licence_holder'  => $user->name,
				'email_address'   => $user->email,
				'company'         => $user->company,
				'product'         => $product->name,
				'version'         => $product->version,
				'activation_code' => $activation_code);
		}

		// Return the licence data as xml
		function AsXml()
		{
			// Create an xml document
			$xml = new DOMDocument('1.0','UTF-8');
			$xml->formatOutput = true;
			$xml->preserveWhiteSpace = false;
			$root = $xml->appendChild($xml->createElement('root'));

			// Add the user details
			foreach ($fields as $key=>$value)
				$root->appendChild($xml->createElement($key, $value));

			return $xml;
		}
	}
?>