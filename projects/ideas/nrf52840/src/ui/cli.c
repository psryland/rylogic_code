// Copyright (c) Rex Bionics 2020
#include "forward.h"
#include "ui/cli.h"

// Notes:
//  - The sensor uses USB for the CLI.
//  - This allows users to connect the sensor to a PC USB port and do things like set 
//    the stability window etc.

enum { CLI_LOG_QUEUE_SIZE = 10 };

// Declare a CLI instance with transport via USB virtual comm port
NRF_CLI_CDC_ACM_DEF(m_cli_usb_transport);
NRF_CLI_DEF(m_cli_usb, "dongle:~$ ", &m_cli_usb_transport.transport, '\r', CLI_LOG_QUEUE_SIZE);

// Declare a CLI instance with transport via the real time terminal
NRF_CLI_RTT_DEF(m_cli_rtt_transport);
NRF_CLI_DEF(m_cli_rtt, "dongle:~$ ", &m_cli_rtt_transport.transport, '\n', CLI_LOG_QUEUE_SIZE);

// Initialise the CLI
void CLI_Init()
{
	// USB should be initialised first, but is not required to be enabled at this point.
	//ASSERT(nrfx_usbd_is_enabled());

	// Start the CLI module
	Check(nrf_cli_init(&m_cli_usb, NULL, true, true, NRF_LOG_SEVERITY_INFO));
	Check(nrf_cli_start(&m_cli_usb));

	// Start the RTT
	Check(nrf_cli_init(&m_cli_rtt, NULL, true, true, NRF_LOG_SEVERITY_INFO));
	Check(nrf_cli_start(&m_cli_rtt));
}

// Pump the CLI event queue
void CLI_Process()
{
	nrf_cli_process(&m_cli_usb);
	nrf_cli_process(&m_cli_rtt);
}

#define UNKNOWN_PARAMETER     "Unknown parameter: "
#define WRONG_PARAMETER_COUNT "Wrong parameter count\n"

// Add a CLI command
static void cmd_wtf(nrf_cli_t const * p_cli, size_t argc, char **argv)
{
	UNUSED_PARAMETER(argc);
	UNUSED_PARAMETER(argv);
	nrf_cli_error(p_cli, "... exactly.");
}
NRF_CLI_CMD_REGISTER(wtf, NULL, "wtf", cmd_wtf);

// Display the firmware version
static void cmd_firmware_version(nrf_cli_t const * p_cli, size_t argc, char **argv)
{
	UNUSED_PARAMETER(argc);
	UNUSED_PARAMETER(argv);
	#ifdef DEBUG
	const char config = 'D';
	#else
	const char config = 'R';
	#endif

	nrf_cli_fprintf(p_cli, NRF_CLI_OPTION, "\n");
	nrf_cli_fprintf(p_cli, NRF_CLI_OPTION, "Version: %d.%02d.%02d.%c.%d\n", VERSION_MAJOR, VERSION_MINOR, VERSION_BUILD, config, ProductRepoRevision);
	nrf_cli_fprintf(p_cli, NRF_CLI_OPTION, "\n");
}
NRF_CLI_CMD_REGISTER(version, NULL, "version", cmd_firmware_version);
