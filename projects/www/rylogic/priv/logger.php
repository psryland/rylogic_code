<?php

// Logging class
class Logger
{
	private $filepath;
	private $logging;

	// Construct the logger with an output file and whether it's enabled or not
	function __construct($logfilepath, $enable)
	{
		$this->filepath = $logfilepath;
		$this->logging = $enable;
	}

	// Enable/Disable logging
	function SetLogging($state)
	{
		$this->logging = $state;
	}

	// Write a message to the log
	function Log($msg, $force = false)
	{
		if ($this->logging || $force)
			file_put_contents($this->filepath, $msg, FILE_APPEND);
	}
}
?>