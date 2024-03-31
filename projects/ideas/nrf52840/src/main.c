// Copyright (c) Rex Bionics 2020
#include "forward.h"
#include "usb/usbd.h"
#include "ble/ble.h"
#include "ui/cli.h"
#include "ui/user_interface.h"
#include "device/millisecond_ticker.h"

// Disable the soft device so stepping in debug works
#define ENABLE_SDH 1
#if !ENABLE_SDH
#pragma message("ENABLE_SDH is disabled")
#endif

// VT100 codes
static char const vt100_save[] = NRF_CLI_VT100_SAVECURSOR;
static char const vt100_rest[] = NRF_CLI_VT100_RESTORECURSOR;
static char const vt100_home[] = NRF_CLI_VT100_CURSORHOME;
static char const vt100_clear[] = NRF_CLI_VT100_CLEARSCREEN;

// Track stats of received sensor's data
typedef struct monitor_stats_s
{
	// The device unique ID (MAC address)
	uint8_t id[6];

	// The counter value last time a packet was received
	uint32_t last;

	// The data last received
	rex_node_sensor_data_t data;

} monitor_stats_t;
static monitor_stats_t m_monitor_stats[10];
static void AddSensorPacketToMonitor(rex_node_sensor_packet_t const* pkt)
{
	static uint8_t zero[6] = {0,0,0,0,0,0};
	uint32_t now = Ticker_Get();

	// Find a slot in the monitor stats
	int i = 0, j = -1, k = 0;
	for (; i != ARRAY_SIZE(m_monitor_stats); ++i)
	{
		monitor_stats_t const* stat = &m_monitor_stats[i];

		// Found an exact match on 'id'
		if (memcmp(&stat->id[0], &pkt->id[0], sizeof(stat->id)) == 0)
			break;

		// Found an empty slot
		if (j == -1 && memcmp(&stat->id[0], &zero[0], sizeof(stat->id)) == 0)
			j = i;

		// Found the oldest one
		if (now - stat->last > now - m_monitor_stats[k].last)
			k = i;
	}
	if (i == ARRAY_SIZE(m_monitor_stats))
	{
		// If there's no match, use a free slot. Otherwise, overwrite the oldest one
		i = (j != -1) ? j : k;
		memcpy(&m_monitor_stats[i].id[0], &pkt->id[0], sizeof(m_monitor_stats[i].id));
	}

	// Record the last data for the sensor
	m_monitor_stats[i].data = pkt->data;
	m_monitor_stats[i].last = now;
}

// Output status monitor stats
enum { StatusMonitorUpdateRateMS = 500 };
static nrf_cli_t const* m_status_monitor = NULL;
static void StatusMonitorOutput()
{
	#if NRF_CLI_ENABLED
	if (m_status_monitor == NULL)
		return;

	static uint8_t zero[6] = {0,0,0,0,0,0};
	uint32_t now = Ticker_Get();

	// Move the cursor back to the start of the line
	nrf_cli_fprintf(m_status_monitor, NRF_CLI_NORMAL, "%s%s                                                   \n", vt100_save, vt100_home);
	nrf_cli_fprintf(m_status_monitor, NRF_CLI_OPTION, "         ID        |            Accel                | Loc | Seq | Age(ms) |          \n");
	for (int i = 0; i != ARRAY_SIZE(m_monitor_stats); ++i) 
	{
		monitor_stats_t const* stat = &m_monitor_stats[i];
		if (memcmp(&stat->id[0], &zero[0], sizeof(stat->id)) == 0) continue;
		nrf_cli_fprintf(m_status_monitor, NRF_CLI_OPTION, " %2.2x.%2.2x.%2.2x.%2.2x.%2.2x.%2.2x | ", stat->id[5], stat->id[4], stat->id[3], stat->id[2], stat->id[1], stat->id[0]);
		nrf_cli_fprintf(m_status_monitor, NRF_CLI_OPTION, " ["FLT_FMT5", "FLT_FMT5", "FLT_FMT5"] |", FLT_ARG5(stat->data.accel[0]), FLT_ARG5(stat->data.accel[1]), FLT_ARG5(stat->data.accel[2]));
		nrf_cli_fprintf(m_status_monitor, NRF_CLI_OPTION, " %3.3d | %3.3d | %7d | \n", stat->data.location, stat->data.seq, now - stat->last);
	}
	nrf_cli_fprintf(m_status_monitor, NRF_CLI_OPTION, "                                              \n%s", vt100_rest);
	#endif
}

// Trigger output of the status monitor
APP_TIMER_DEF(m_timer_status_monitor);
static bool m_status_monitor_output_pending;
static void HandleStatusMonitorTick(void* context)
{
	UNUSED_PARAMETER(context);
	m_status_monitor_output_pending = true;
}

// Status monitor
static void StatusMonitor_Init()
{
	// Create an app timer for outputing the status
	Check(app_timer_create(&m_timer_status_monitor, APP_TIMER_MODE_REPEATED, &HandleStatusMonitorTick));
}
static void StatusMonitor_Process()
{
	if (!m_status_monitor_output_pending) return;
	m_status_monitor_output_pending = false;
	StatusMonitorOutput();
}

// Dump received packets to the log
static bool m_log_packets = false;
static void LogPackets(rex_node_sensor_packet_t const* pkt)
{
	if (!m_log_packets)
		return;

	char msg[256]; int i = 0;
	i += snprintf(&msg[i], sizeof(msg) - i, "ID: %2.2X%2.2X%2.2X%2.2X%2.2X%2.2X  "
		,pkt->id[5]
		,pkt->id[4]
		,pkt->id[3]
		,pkt->id[2]
		,pkt->id[1]
		,pkt->id[0]);
	i += snprintf(&msg[i], sizeof(msg) - i, "Accel: ["FLT_FMT3", "FLT_FMT3", "FLT_FMT3"]  "
		,FLT_ARG3(pkt->data.accel[0])
		,FLT_ARG3(pkt->data.accel[1])
		,FLT_ARG3(pkt->data.accel[2]));
	i += snprintf(&msg[i], sizeof(msg) - i, "Loc: %d  Flags: %d  Seq: %d  CRC: %8.8X  "
		,pkt->data.location
		,pkt->data.flags
		,pkt->data.seq
		,pkt->crc);
	if (i < 0 || i >= (int)sizeof(msg)) { NRF_LOG_INFO("Take that stack! (also, increase the size of 'msg')"); }
	else                                { NRF_LOG_INFO("%s", NRF_LOG_PUSH(msg)); }
}

// Process log messages
static void Log_Process()
{
	for (; NRF_LOG_PROCESS(); ) {}
}

// Entry Point
int main(void)
{
	// ** Remember that the SoftDevice (S140) must be present. **

	// Initialize loging system and GPIOs.
	Check(NRF_LOG_INIT(NULL));
	NRF_LOG_DEFAULT_BACKENDS_INIT();
	NRF_LOG_INFO("\n\n RexNode Dongle Started -----------------------------");
	NRF_LOG_DEBUG("Debug log test");

	// Initialise power management
	Check(nrf_pwr_mgmt_init());

	// Set up timers
	Check(nrf_drv_clock_init());
	nrf_drv_clock_lfclk_request(NULL);
	Check(app_timer_init());

	// Set up LEDs/Buttons/etc
	UserInterface_Init();

	// Set up the millisecond ticker
	Ticker_Init();

	// Initialise the USB port
	USB_Init();

	// Set up the CLI
	CLI_Init();

	// Initialise BLE
	#if ENABLE_SDH
	BLE_Init();
	#endif

	// Set up the status monitor
	StatusMonitor_Init();

	// Main loop
	NRF_LOG_INFO("Entering main loop");

	for (;;)
	{
		// Process log messages
		Log_Process();

		// Process queued CLI events
		CLI_Process();

		// Indicate the current device state
		UserInterface_Process();

		// Read received sensor data
		#if ENABLE_SDH
		for (rex_node_sensor_packet_t pkt; BLE_ReceivedDataGet(&pkt);)
		{
			AddSensorPacketToMonitor(&pkt);
			LogPackets(&pkt);
			USB_Write(&pkt, sizeof(pkt));
		}
		#else
		{
			rex_node_sensor_packet_t pkt;
			AddSensorPacketToMonitor(&pkt);
		}
		#endif

		// Refresh the status monitor output
		StatusMonitor_Process();

		// Go to low power mode and wait for an event
		// This calls 'sd_app_evt_wait' internally if the softdevice is enabled
		nrf_pwr_mgmt_run();
	}
}

// Command line functions
#if NRF_CLI_ENABLED

// Display the firmware version
static void cmd_show_data(nrf_cli_t const * p_cli, size_t argc, char **argv)
{
	UNUSED_PARAMETER(argc);
	UNUSED_PARAMETER(argv);
	m_log_packets = !m_log_packets;
}
NRF_CLI_CMD_REGISTER(show_data, NULL, "Toggle the display of transmitted data", cmd_show_data);

// Sensor config
static void cmd_monitor(nrf_cli_t const * p_cli, size_t argc, char ** argv)
{
	UNUSED_PARAMETER(argc);
	UNUSED_PARAMETER(argv);
	if (nrf_cli_help_requested(p_cli))
	{
		nrf_cli_help_print(p_cli, NULL, 0);
		return;
	}

	if (m_status_monitor == NULL)
	{
		m_status_monitor = p_cli;
		Check(app_timer_start(m_timer_status_monitor, APP_TIMER_TICKS(StatusMonitorUpdateRateMS), NULL));
		nrf_cli_fprintf(p_cli, NRF_CLI_NORMAL, "%s", vt100_clear);
		nrf_cli_fprintf(p_cli, NRF_CLI_NORMAL, "Status monitor enabled\n");
	}
	else
	{
		Check(app_timer_stop(m_timer_status_monitor));
		nrf_cli_fprintf(p_cli, NRF_CLI_NORMAL, "Status monitor disabled\n");
		m_status_monitor = NULL;
	}
}
NRF_CLI_CMD_REGISTER(monitor, NULL, "Status monitor.", cmd_monitor);

#endif