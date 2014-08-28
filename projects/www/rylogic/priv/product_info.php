<?php

	class ProductInfo
	{
		public $name;     // Product name
		public $version;  // Product version
		public $desc;     // Product text description
		public $price;    // Product price
		public $currency; // The currency of '$price'
	
		function __construct($product)
		{
			global $host,$db_user,$db_pass,$db_name;

			// Open a connection to the products database
			$link = new mysqli($host,$db_user,$db_pass,$db_name);
			if ($link->connect_error)
				die('connect error ('.$link->connect_errno.') '.$link->connect_error);

			$this->name     = $product;
			$this->version  = $this->db_get($link, 'Version');
			$this->desc     = $this->db_get($link, 'Description');
			$this->price    = $this->db_get($link, 'Price');
			$this->currency = $this->db_get($link, 'Currency');

			$link->close();
		}
	
		private function db_get($link, $column)
		{
			$col = $link->query("select ".$column." from products where Name='".$this->name."' limit 1") or die("error".mysqli_error($link));
			$col = $col->fetch_assoc()[$column];
			return $col;
		}
	}

?>