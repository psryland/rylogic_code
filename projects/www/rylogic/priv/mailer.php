<?php

require_once("emailPassword.php");
require_once("logger.php");
require_once("../../vendor/swiftmailer/swiftmailer/lib/swift_required.php");

class Mailer
{
	private $myEmail;
	private $logger;   // A reference to the shared logger

	function __construct()
	{
		$this->myEmail = "someone@somewhere.com";
	}

	function SetLogger(Logger &$logFile)
	{
		$this->logger = $logFile;
	}

	function SendEbook($ebookType, $mailTo)
	{
		// You can either send a link to the product or a file.  The choice is yours.
		// Refer to swift mailer documentation, and other online resources for the appropriate
		// steps to take for each option.  If you want to send a file, the 'mailWithAttachments'
		// routine may be useful.  The usage of it is:
		$this->MailWithAttachment($fileName, $path, $mailTo, $this->myEmail, $from, $replyTo, $subject, $msg);
	}

	private function MailWithAttachment($filename, $path, $mailTo, $from_mail, $from_name, $replyto, $subject, $message)
	{
		// This code derives from
		// http://www.finalwebsites.com/forums/topic/php-e-mail-attachment-script
		$transport = Swift_SmtpTransport::newInstance('mailServer.com', 465, 'ssl')
			->setUsername(hiddenEmailAccount())
			->setPassword(hiddenEmailPassword());
		$mailer = Swift_Mailer::newInstance($transport);
		$message = Swift_Message::newInstance()
			->setSubject($subject)
			->setFrom(array($from_mail => $from_name))
			->setTo(array($mailTo))
			->setBody($message)
			//->addPart('<p>Here is the message itself</p>', 'text/html')
			->attach(Swift_Attachment::fromPath($path.$filename));
		$this->logger->forceLog("Sending ".$filename." to ".$mailTo." : ");
		$result = $mailer->send($message);
		$this->logger->forceLog("Result = ".$result."\r\n");
	}
}
?>