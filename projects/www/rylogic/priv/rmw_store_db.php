<?php

require_once("db_config.php");
require_once("logger.php");

class RmwStoreDB
{
	private $loggerC;  //This will actually be a reference; see 'setloggerC' for mechanism which accomplishes this.
	private $lastRowC;

	function SetLogger(mylogger $logFile)
	{
		$this->loggerC = $logFile;
	}


	function tellDb($sql)
	{
		$ret = mysql_query($sql);
		if (!$ret) {
			$this->loggerC->log(
						"DATABASE ERROR:\r\n" . mysql_error() . "\r\n" .
									"Query: " . HtmlEntities($sql));
			die();
		}
		return $ret;
	}


	function itemExists($table, $column, $value) {
		$rows = $this->tellDb("Select * from " . $table . " where " .
					$column . " = '" . $value . "'");
		$lastRowC = mysql_fetch_array($rows);
		if ($lastRowC) return true;
		return false;
	}


	function getCellValue($what, $table, $column, $theId) {
		//No checking will be done to ensure only one row is returned, and default value will
		//be '0.00.'
		$rows = $this->tellDb("Select " . $what . " from " . $table .
					" where " . $column . " = " . $theId);
		$row = mysql_fetch_array($rows);
		if ($row) return $row['price'];
		else return 0.00;
	}


	function addOrUpdateUser() {
		//We are using the $_POST data, which is global to the application.

		//These are the fields we are dealing with: 'first_name'  'last_name'
		//'payer_email', and, for shipped products ONLY - 'address_name'
		//'address_street' (can be 2 lines separated by \r\n?)  'address_city'
		//'address_state'  'address_zip'  'address_country_code'
		//'address_country'

		//First, see if the user exists:
		$rows = $this->tellDb("Select * from customers where email = '" .
					$_POST['payer_email'] . "'");
		//Simply use the first of the returned rows:
		$row = mysql_fetch_array($rows);
		if (!$row) {
			$this->loggerC->log("Adding user to database");
			$this->addUser();
		}
		else {
			$this->loggerC->log("User already exists in DB.\r\n");
			//See if the records match the ones we have, and if not, change it:
			$this->updateUser($row);
		}
	}


	private function addUser() {
		$cmd = "Insert into customers (firstName, lastName, shippingName, email, " .
					"addressLine1, city, state, zipCode, country) values ('"
					. $_POST['first_name'] . "', '" .
					$_POST['last_name'] . "', '" .
					$_POST['address_name'] . "', '" .
					$_POST['payer_email'] . "', '" .
					$_POST['address_street'] . "', '" .
					$_POST['address_city'] . "', '" .
					$_POST['address_state'] . "', '" .
					$_POST['address_zip'] . "', '" .
					$_POST['address_country'] . "')";

		$this->tellDb($cmd);

		$this->loggerC->log("Added: '" . $_POST['first_name'] .
					"', '" . $_POST['last_name'] .
					"', '" . $_POST['address_name'] .
					"', '" . $_POST['payer_email'] .
					"', '" . $_POST['address_street'] .
					"', '" . $_POST['address_city'] .
					"', '" . $_POST['address_state'] .
					"', '" . $_POST['address_zip'] .
					"', '" . $_POST['address_country'] .
					"')");
	}


	private function updateUser(array $row) {
		//First, check old values:
		if ($row['firstName']    != $_POST['first_name']     ||
			$row['lastName']     != $_POST['last_name']      ||
			$row['shippingName'] != $_POST['address_name']   ||
			$row['email']        != $_POST['payer_email']    ||
			$row['addressLine1'] != $_POST['address_street'] ||
			$row['city']         != $_POST['address_city']   ||
			$row['state']        != $_POST['address_state']  ||
			$row['zipCode']      != $_POST['address_zip']    ||
			$row['country']      != $_POST['address_country']) {

			//Form a command string:
			$cmd = "UPDATE customers SET ";
			$cmd .= "firstName = '"    . $_POST['first_name']      . "', ";
			$cmd .= "lastName = '"     . $_POST['last_name']       . "', ";
			$cmd .= "shippingName = '" . $_POST['address_name']    . "', ";
			$cmd .= "addressLine1 = '" . $_POST['address_street']  . "', ";
			$cmd .= "city = '"         . $_POST['address_city']    . "', ";
			$cmd .= "state = '"        . $_POST['address_state']   . "', ";
			$cmd .= "zipCode = '"      . $_POST['address_zip']     . "', ";
			$cmd .= "country = '"      . $_POST['address_country'] . "' ";
			$cmd .= "WHERE email = '" . $_POST['payer_email'] . "'";
			$this->loggerC->log("\r\nChanging user with email " . $_POST['payer_email'] . "\r\n");

			$old = $row['firstName'] . ", " . $row['lastName'] .
						", " . $row['shippingName'] . ", " .
						$row['email'] . ", " .
						$row['addressLine1'] . ", " . $row['city'] .
						", " .$row['state'] . ", " .
						$row['zipCode'] . ", " . $row['country'] .
						"\r\n\r\n";

			$this->loggerC->log($old);
			$this->loggerC->log($cmd . "\r\n");

			$this->tellDb($cmd);
		}
	}


	function addOrder() {
		//Everything will be in the $_POST values
		//First, get the customer number:
		$cmd = "Select id from customers where email = '" .
					$_POST['payer_email'] . "'";
		//We just entered this information into the database, so error checking isn't
		//really needed?  We will put something in just for grins.
		$rows = $this->tellDb($cmd);
		$row = mysql_fetch_array($rows);
		if (!$row) {
			$this->loggerC->log("HUGE PROBLEM! CUSTOMER ID NOT FOUND -" .
						" ABORTING\r\n");
			die();
		}
		$id = $row['id'];
		$theDate = date('F j, Y, g:i a');
		$tz = date('T');
		$ppID = $_POST['txn_id'];
		$grossPay = $_POST['payment_gross'];
		$shipping = $_POST['shipping'];
		$cmd = "Insert into orders (customer, date, timeZone, payPalTransId, " .
					"grossPmt, shipping) values ('$id', '" .
					"$theDate', '$tz', '$ppID', '" .
					"$grossPay', '$shipping')";

		$this->tellDb($cmd);
		$this->loggerC->log("Inserting order into orders table:\r\n" . $cmd .
					"\r\n\r\n");

		//Now we have to add the order items into the orderItems table.
		//First, we need to get the order number from the record which was just entered:

		$cmd = "Select id from orders where payPalTransId = '$ppID'";
		$rows = $this->tellDb($cmd);
		$row = mysql_fetch_array($rows);
		//Should not need this, but...
		if (!$row) {
			$this->loggerC->log("HUGE PROBLEM! ORDER ID NOT FOUND -" .
						" ABORTING\r\n");
			die();
		}

		$id = $row['id'];
		//And the command to enter the item:
		$itemNum = $_POST['item_number'];
		$qty = $_POST['quantity'];
		$info = $_POST['option_selection1'];
		$cmd = "Insert into orderItems (orderNumber, item, quantity, extraInfo)" .
					" values('$id', '$itemNum', '$qty'," .
					" '$info')";
		$this->loggerC->log(
					"Inserting into order items:\r\n" . $cmd . "\r\n");
		$this->tellDb($cmd);
	}

}
?>