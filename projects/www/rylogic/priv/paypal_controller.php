<?php
//This should be placed in the subdirectory beneath the user-accessible
//one. On localhost this can't be done, so remember to do it manually
//when copying to the site.
//http://www.codeproject.com/Articles/619111/Some-Simple-PHP-Classes-for-Managing-PayPal-Instan

require_once("logger.php");
require_once("paypal_ipn.php");
//require_once("mailer.php");
//require_once("rmw_store_db.php");

class PayPalController
{
	private $db;
	private $logger;
	private $testing;

	function __construct($testing, $logging)
	{
		$this->testing = $testing;

		// Create a logger shared by the other classes
		global $rootdir;
		$this->logger = new Logger($rootdir."\ppc_log.txt", $logging);

		//// Create a database helper
		//$this->db = new RmwStoreDB();
		//$this->db->SetLogger($this->logger);
	}

	// Do the paypal instant payment notification
	function ProcessPayPalIpnPayment()
	{
		$this->logger->Log("Processing a payment.\r\n");
		$processor = new PayPalIpn($this->testing, $this->logger);

		// The whole payment process
		if (!$processor->ProcessPost()) return;
		if ($this->DuplicateTransaction()) return;
		if (!$this->Verify()) return;
		if (!$this->AddOrderToDatabase()) return;

		$this->SendProduct();
	}

	//
	private function DuplicateTransaction()
	{
		$ret = false;
		if ($this->db->ItemExists("orders", "payPalTransId", $_POST['txn_id']))
		{
			$this->logger->Log("Transaction: ".$_POST['txn_id']." exists\r\n");
			$ret = true;
		}
		else
		{
			$this->logger->Log("Transaction: ".$_POST['txn_id']." does not exist\r\n");
		}
		return $ret;
	}

	//
	private function Verify()
	{
		// First, check for valid item number:
		if (!$this->db->itemExists("products", "id", $_POST['item_number']))
		{
			$this->logger->Log("Item number: ".$_POST['item_number']." doesn't exist in database\r\n");
			return false;
		}
		else
		{
			$this->logger->Log("Item number: ".$_POST['item_number']." exists in database\r\n");
		}

		// Check that we received the proper amount, or more, in the case of Texas taxes:
		$this->dbPrice = $this->db->getCellValue("price", "products", "id", $_POST['item_number']);
		if ($_POST['mc_gross'] < $this->dbPrice)
		{
			$this->logger->Log("Payment received (".$_POST['mc_gross'].") less than item price. (".$this->dbPrice."\r\n");
			return false;
		}
		else
		{
			$this->logger->Log("Adequate payment received (".$_POST['mc_gross'].").\r\n");
		}

		if ($_POST['mc_currency'] != "USD")
		{
			$this->logger->Log("Paid in non-US funds - need to investigate.\r\n");
			return false;
		}
		else
		{
			$this->logger->Log("US Currency received - OK.\r\n");
		}

		if ($_POST['receiver_email'] != "emailAddress@someplace.com" && $_POST['receiver_email'] != "sandboxEmailAddress@someplace.com")
		{
			$this->logger->Log("Incorrect receiver email received (".$_POST['receiver_email'].")\r\n");
			return false;
		}
		else
		{
			$this->logger->Log("Correct email received (".$_POST['receiver_email'].")\r\n");
		}

		// And the most important one:
		if ($_POST['payment_status'] != "Completed")
		{
			$this->logger->Log("Payment incomplete from PayPal\r\n");
			return false;
		}
		return true;
	}

	private function AddOrderToDatabase()
	{
		// Everything will revolve around email address as of primary importance;
		// if there is one in the database the record will be updated to reflect any
		// changes to the account.  If one doesn't exist, one will be created.
		$this->logger->Log("Updating database.\r\n");
		$this->db->AddOrUpdateUser();
		$this->db->AddOrder();
		return true;
	}

	private function SendProduct()
	{
		$mailHandler = new Mailer();
		$mailHandler->SetLogger($this->logger);

		if ($this->testing)
		{
			$mailTo = 'emailAddress@someplace.com';
		}
		else
		{
			$mailTo = $_POST['payer_email'];
		}

		// The following will need modified per your own requirements.
		// I am leaving this code in simply to show you the approach I take:
		if ($_POST['item_number'] == "XXXX" || $_POST['item_number'] == "XXXY")
		{
			if ($_POST['option_selection1'] == 'EPub')
			{
				$this->logger->Log("Sending EPub to ".$mailTo."\r\n");
				$mailHandler->SendEbook('EPub', $mailTo);
			}
			else if ($_POST['option_selection1'] == 'MOBI')
			{
				$this->logger->Log("Sending MOBI to ".$mailTo."\r\n");
				$mailHandler->SendEbook('MOBI', $mailTo);
			}
			else
			{
				$this->logger->Log("SOMETHING WRONG - Not EPub or MOBI!\r\n");
			}
		}
	}
}
?>