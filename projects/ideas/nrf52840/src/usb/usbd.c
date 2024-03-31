// Copyright (c) Rex Bionics 2020
#include "forward.h"
#include "usb/usbd.h"

enum
{
	MAIN_CDC_ACM_COMM_INTERFACE = 0,
	MAIN_CDC_ACM_DATA_INTERFACE = 1,
	MAIN_CDC_ACM_COMM_EPIN = NRF_DRV_USBD_EPIN2,
	MAIN_CDC_ACM_DATA_EPIN = NRF_DRV_USBD_EPIN1,
	MAIN_CDC_ACM_DATA_EPOUT = NRF_DRV_USBD_EPOUT1,
};
static char m_rx_buffer[64];
static void CDCACMUserEventHandler(app_usbd_class_inst_t const *p_inst, app_usbd_cdc_acm_user_event_t event);

// CDC_ACM class instance
APP_USBD_CDC_ACM_GLOBAL_DEF(m_app_cdc_acm,
	CDCACMUserEventHandler,
	MAIN_CDC_ACM_COMM_INTERFACE, // Comm interface number
	MAIN_CDC_ACM_DATA_INTERFACE, // Data interface number 
	MAIN_CDC_ACM_COMM_EPIN,      // Comm In
	MAIN_CDC_ACM_DATA_EPIN,      // Data In
	MAIN_CDC_ACM_DATA_EPOUT,     // Data Out
	APP_USBD_CDC_COMM_PROTOCOL_AT_V250);

// USB user event handler
static void CDCACMUserEventHandler(app_usbd_class_inst_t const *p_inst, app_usbd_cdc_acm_user_event_t event)
{
	app_usbd_cdc_acm_t const *p_cdc_acm = app_usbd_cdc_acm_class_get(p_inst);
	ASSERT(p_cdc_acm == &m_app_cdc_acm);
	switch (event)
	{
		case APP_USBD_CDC_ACM_USER_EVT_PORT_OPEN:
		{
			// Setup first transfer
			UNUSED_VARIABLE(app_usbd_cdc_acm_read(p_cdc_acm, m_rx_buffer, ARRAY_SIZE(m_rx_buffer)));
			break;
		}
		case APP_USBD_CDC_ACM_USER_EVT_PORT_CLOSE:
		{
			break;
		}
		case APP_USBD_CDC_ACM_USER_EVT_TX_DONE:
		{
			break;
		}
		case APP_USBD_CDC_ACM_USER_EVT_RX_DONE:
		{
			NRF_LOG_DEBUG("Bytes waiting: %d", app_usbd_cdc_acm_bytes_stored(p_cdc_acm));
			for (;;)
			{
				// Get amount of data transfered
				size_t size = app_usbd_cdc_acm_rx_size(p_cdc_acm);
				NRF_LOG_DEBUG("RX: size: %u char: %c", size, m_rx_buffer[0]);

				// Fetch data until internal buffer is empty
				if (app_usbd_cdc_acm_read(p_cdc_acm, m_rx_buffer, ARRAY_SIZE(m_rx_buffer)) != NRF_SUCCESS)
					break;
			}
			break;
		}
		default:
		{
			break;
		}
	}
}

// USB state event handler
static void StateEventHandler(app_usbd_event_type_t event)
{
	switch (event)
	{
		case APP_USBD_EVT_DRV_SUSPEND:
		{
			break;
		}
		case APP_USBD_EVT_DRV_RESUME:
		{
			break;
		}
		case APP_USBD_EVT_STARTED:
		{
			break;
		}
		case APP_USBD_EVT_STOPPED:
		{
			app_usbd_disable();
			break;
		}
		case APP_USBD_EVT_POWER_DETECTED:
		{
			NRF_LOG_INFO("USB power detected");
			if (!nrfx_usbd_is_enabled()) app_usbd_enable();
			break;
		}
		case APP_USBD_EVT_POWER_REMOVED:
		{
			NRF_LOG_INFO("USB power removed");
			app_usbd_stop();
			break;
		}
		case APP_USBD_EVT_POWER_READY:
		{
			NRF_LOG_INFO("USB ready");
			app_usbd_start();
			break;
		}
		default:
		{
			break;
		}
	}
}

// Set up the USB-CDC support so that the dongle shows up as a COMM port on the host PC.
// USBD-CDC-ACM = USB - Communications Device Class - Abstract Control Model
void USB_Init()
{
	// Ensure the clock module is initialised
	// This is required by the USBD module (apparently)
	ret_code_t r = nrf_drv_clock_init();
	if (r == NRF_SUCCESS)
	{
		// Make a request to start it.
		nrf_drv_clock_lfclk_request(NULL);
		for ( ;!nrf_drv_clock_lfclk_is_running(); ) {} // 喂 听
	}
	else if (r != NRF_ERROR_MODULE_ALREADY_INITIALIZED)
	{
		Check(r);
	}

	// Generate a USB serial number from the device addr (FIRC->DEVICEADDR)
	app_usbd_serial_num_generate();

	// Configure the USB with event handlers
	static const app_usbd_config_t usbd_config =
	{
		#if CLI_OVER_USB_CDC_ACM
		.ev_handler = app_usbd_event_execute,
		#endif
		.ev_state_proc = StateEventHandler,
	};
	Check(app_usbd_init(&usbd_config));
	NRF_LOG_INFO("USBD initialised");

	// Add the virtual comm port instance for the sensor data
	Check(app_usbd_class_append(app_usbd_cdc_acm_class_inst_get(&m_app_cdc_acm)));

	// Add the virtual comm port instance for the CLI
	#if CLI_OVER_USB_CDC_ACM
	Check(app_usbd_class_append(app_usbd_cdc_acm_class_inst_get(&nrf_cli_cdc_acm)));
	#endif

	enum { USBD_POWER_DETECTION = true };
	if (USBD_POWER_DETECTION)
	{
		// Use power events to enable/disable USB
		Check(app_usbd_power_events_enable());
	}
	else
	{
		app_usbd_enable();
		app_usbd_start();
		NRF_LOG_INFO("USB Started");
	}

	// Give some time for the host to enumerate and connect to the USB CDC port
	nrf_delay_ms(1000);
}

// Pump the USB event queue
void USB_Process()
{
}

// Write data to the USB port
ret_code_t USB_Write(void const *data, size_t size)
{
	return app_usbd_cdc_acm_write(&m_app_cdc_acm, data, size);
}