// Copyright (c) Rex Bionics 2020
#include "ble/ble.h"

// Notes:
//   - There seems to be multiple ways to set up the BLE stack in the soft device.
//     The 'sd_ble_...' calls are the lowest level which actually talk to the soft device.
//     Other calls, such has 'ble_advertising_start', are intended (I think) to be a wrapper
//     around the soft device calls. They seem to be set up for standard BLE connection/pairing.
//   - The 'ble_advertising' module says is for **connectable** advertising. We're using
//     non-connectable, so the 'ble_advertising' module isn't being used. This code is modelled
//     off it though
//   - The Mesh SDK has another flavour of BLE advertising support.

enum
{
	// Tag that identifies the SoftDevice BLE configuration.
	APP_BLE_CONN_CFG_TAG = 1,

	// Priority of the application BLE event handler. There is no need to modify this value.
	APP_BLE_OBSERVER_PRIO = 1,

	// Scan interval and window in 625 us units.
	// If scan_phys contains both @ref BLE_GAP_PHY_1MBPS and BLE_GAP_PHY_CODED interval shall be larger than or equal to twice the scan window.
	// Make 'SCAN_INTERVAL' == 'SCAN_WINDOW' for 100% advertising duty cycle
	SCAN_INTERVAL = MSEC_TO_UNITS(500, UNIT_0_625_MS),
	SCAN_WINDOW = SCAN_INTERVAL,

	// The company id used in manufacturer specific data (0xFFFF = unknown)
	RexBionicsCompanyId = 0xFFFF,
};

// Scanning module instance.
NRF_BLE_SCAN_DEF(m_scan);

// A queue of received sensor data
NRF_QUEUE_DEF(rex_node_sensor_packet_t, m_sensor_data, 100, NRF_QUEUE_MODE_NO_OVERFLOW);

// Convert BLE events to strings
static char const* BLEEventToString(uint16_t evt)
{
	#if NRF_LOG_ENABLED
	switch (evt)
	{
	case BLE_GAP_EVT_CONNECTED:                  return "BLE_GAP_EVT_CONNECTED";                  // Connected to peer.                                   \n See @ref ble_gap_evt_connected_t
	case BLE_GAP_EVT_DISCONNECTED:               return "BLE_GAP_EVT_DISCONNECTED";               // Disconnected from peer.                              \n See @ref ble_gap_evt_disconnected_t.
	case BLE_GAP_EVT_CONN_PARAM_UPDATE:          return "BLE_GAP_EVT_CONN_PARAM_UPDATE";          // Connection Parameters updated.                       \n See @ref ble_gap_evt_conn_param_update_t.
	case BLE_GAP_EVT_SEC_PARAMS_REQUEST:         return "BLE_GAP_EVT_SEC_PARAMS_REQUEST";         // Request to provide security parameters.              \n Reply with @ref sd_ble_gap_sec_params_reply.  \n See @ref ble_gap_evt_sec_params_request_t
	case BLE_GAP_EVT_SEC_INFO_REQUEST:           return "BLE_GAP_EVT_SEC_INFO_REQUEST";           // Request to provide security information.             \n Reply with @ref sd_ble_gap_sec_info_reply.    \n See @ref ble_gap_evt_sec_info_request_t.
	case BLE_GAP_EVT_PASSKEY_DISPLAY:            return "BLE_GAP_EVT_PASSKEY_DISPLAY";            // Request to display a passkey to the user.            \n In LESC Numeric Comparison, reply with @ref sd_ble_gap_auth_key_reply. \n See @ref ble_gap_evt_passkey_display_t
	case BLE_GAP_EVT_KEY_PRESSED:                return "BLE_GAP_EVT_KEY_PRESSED";                // Notification of a keypress on the remote device.     \n See @ref ble_gap_evt_key_pressed_t
	case BLE_GAP_EVT_AUTH_KEY_REQUEST:           return "BLE_GAP_EVT_AUTH_KEY_REQUEST";           // Request to provide an authentication key.            \n Reply with @ref sd_ble_gap_auth_key_reply.    \n See @ref ble_gap_evt_auth_key_request_t.
	case BLE_GAP_EVT_LESC_DHKEY_REQUEST:         return "BLE_GAP_EVT_LESC_DHKEY_REQUEST";         // Request to calculate an LE Secure Connections DHKey. \n Reply with @ref sd_ble_gap_lesc_dhkey_reply.  \n See @ref 
	case BLE_GAP_EVT_AUTH_STATUS:                return "BLE_GAP_EVT_AUTH_STATUS";                // Authentication procedure completed with status.      \n See @ref ble_gap_evt_auth_status_t.
	case BLE_GAP_EVT_CONN_SEC_UPDATE:            return "BLE_GAP_EVT_CONN_SEC_UPDATE";            // Connection security updated.                         \n See @ref ble_gap_evt_conn_sec_update_t.
	case BLE_GAP_EVT_TIMEOUT:                    return "BLE_GAP_EVT_TIMEOUT";                    // Timeout expired.                                     \n See @ref ble_gap_evt_timeout_t.
	case BLE_GAP_EVT_RSSI_CHANGED:               return "BLE_GAP_EVT_RSSI_CHANGED";               // RSSI report.                                         \n See @ref ble_gap_evt_rssi_changed_t.
	case BLE_GAP_EVT_ADV_REPORT:                 return "BLE_GAP_EVT_ADV_REPORT";                 // Advertising report.                                  \n See @ref ble_gap_evt_adv_report_t.
	case BLE_GAP_EVT_SEC_REQUEST:                return "BLE_GAP_EVT_SEC_REQUEST";                // Security Request.                                    \n See @ref ble_gap_evt_sec_request_t.
	case BLE_GAP_EVT_CONN_PARAM_UPDATE_REQUEST:  return "BLE_GAP_EVT_CONN_PARAM_UPDATE_REQUEST";  // Connection Parameter Update Request.                 \n Reply with @ref sd_ble_gap_conn_param_update. \n See @ref ble_gap_evt_conn_param_update_request_t
	case BLE_GAP_EVT_SCAN_REQ_REPORT:            return "BLE_GAP_EVT_SCAN_REQ_REPORT";            // Scan request report.                                 \n See @ref ble_gap_evt_scan_req_report_t
	case BLE_GAP_EVT_PHY_UPDATE_REQUEST:         return "BLE_GAP_EVT_PHY_UPDATE_REQUEST";         // PHY Update Request.                                  \n Reply with @ref sd_ble_gap_phy_update. \n See @ref ble_gap_evt_phy_update_request_t
	case BLE_GAP_EVT_PHY_UPDATE:                 return "BLE_GAP_EVT_PHY_UPDATE";                 // PHY Update Procedure is complete.                    \n See @ref ble_gap_evt_phy_update_t.
	case BLE_GAP_EVT_DATA_LENGTH_UPDATE_REQUEST: return "BLE_GAP_EVT_DATA_LENGTH_UPDATE_REQUEST"; // Data Length Update Request.                          \n Reply with @ref sd_ble_gap_data_length_update. \n See @ref ble_gap_evt_data_length_update_request_t
	case BLE_GAP_EVT_DATA_LENGTH_UPDATE:         return "BLE_GAP_EVT_DATA_LENGTH_UPDATE";         // LL Data Channel PDU payload length updated.          \n See @ref ble_gap_evt_data_length_update_t
	case BLE_GAP_EVT_QOS_CHANNEL_SURVEY_REPORT:  return "BLE_GAP_EVT_QOS_CHANNEL_SURVEY_REPORT";  // Channel survey report.                               \n See @ref ble_gap_evt_qos_channel_survey_report_t
	case BLE_GAP_EVT_ADV_SET_TERMINATED:         return "BLE_GAP_EVT_ADV_SET_TERMINATED";         // Advertising set terminated.                          \n See @ref ble_gap_evt_adv_set_terminated_t
	}
	#endif
	UNUSED_PARAMETER(evt);
	return "";
}
#if 0
static char const* BLEScanEventToString(nrf_ble_scan_evt_t evt)
{
	#if NRF_LOG_ENABLED
	switch (evt)
	{
		case NRF_BLE_SCAN_EVT_FILTER_MATCH: return "NRF_BLE_SCAN_EVT_FILTER_MATCH";                 // A filter is matched or all filters are matched in the multifilter mode
		case NRF_BLE_SCAN_EVT_WHITELIST_REQUEST: return "NRF_BLE_SCAN_EVT_WHITELIST_REQUEST";       // Request the whitelist from the main application. For whitelist scanning to work, the whitelist must be set when this event occurs
		case NRF_BLE_SCAN_EVT_WHITELIST_ADV_REPORT: return "NRF_BLE_SCAN_EVT_WHITELIST_ADV_REPORT"; // Send notification to the main application when a device from the whitelist is found
		case NRF_BLE_SCAN_EVT_NOT_FOUND: return "NRF_BLE_SCAN_EVT_NOT_FOUND";                       // The filter was not matched for the scan data
		case NRF_BLE_SCAN_EVT_SCAN_TIMEOUT: return "NRF_BLE_SCAN_EVT_SCAN_TIMEOUT";                 // Scan timeout
		case NRF_BLE_SCAN_EVT_CONNECTING_ERROR: return "NRF_BLE_SCAN_EVT_CONNECTING_ERROR";         // Error occurred when establishing the connection. In this event, an error is passed from the function call @ref sd_ble_gap_connect
		case NRF_BLE_SCAN_EVT_CONNECTED: return "NRF_BLE_SCAN_EVT_CONNECTED";                       // Connected to device
	}
	#endif
	UNUSED_PARAMETER(evt);
	return "";
}
#endif

// Find the next instance of the given advertising data type in the range [ptr,end)
// Returned pointer points to the start of the adv data (i.e. the length:u8) or null if not found.
static uint8_t const* FindAdvData(uint8_t ad_type, uint8_t const* ptr, uint8_t const* end)
{
	// Advertising Data Format:
	//  length:u8, ad_type:u8, data:u8[length-1]
	for (; end - ptr >= 2 && ptr[1] != ad_type; ptr += ptr[0] + 1) {}
	return ptr < end ? ptr : NULL;
}

// Handle BLE events
static void HandleBLEEvents(ble_evt_t const* evt, void* context)
{
	UNUSED_PARAMETER(context);
	switch (evt->header.evt_id)
	{
		case BLE_GAP_EVT_ADV_REPORT:
		{
			ble_gap_evt_t const* gap_evt = &evt->evt.gap_evt;
			ble_data_t const* gap_data = &gap_evt->params.adv_report.data;
			ble_gap_addr_t const* peer_addr = &gap_evt->params.adv_report.peer_addr;
			uint8_t const* ptr = gap_data->p_data;
			uint8_t const* end = gap_data->p_data + gap_data->len;

			// Advertising data has the format:
			//   [<ad_data><ad_data>...] // max 31 bytes
			// where <ad_data> has the format:
			//   length:u8, ad_type:u8, data:u8[length-1]
			// For manufacturer specific data, the 'data' has the format:
			//   company_identifier:u16, data:u8[length-3]
			//NRF_LOG_INFO("\n");
			//NRF_LOG_HEXDUMP_INFO(gap_data->p_data, gap_data->len);

			// Watch for 'RexNode's
			static char const RexNodeName[] = {'R','e','x','N','o','d','e'};
			uint8_t const* name = FindAdvData(BLE_GAP_AD_TYPE_SHORT_LOCAL_NAME, ptr, end);
			if (name == NULL ||
				name[0] < 1 + sizeof(RexNodeName) ||
				memcmp(&RexNodeName[0], &name[2], sizeof(RexNodeName)) != 0)
				break;

			// Find the sensor data in the advertising packet
			uint8_t const* msd = FindAdvData(BLE_GAP_AD_TYPE_MANUFACTURER_SPECIFIC_DATA, ptr, end);
			if (msd == NULL ||
				msd[0] < 1 + sizeof(uint16_t) + sizeof(rex_node_sensor_data_t) ||
				uint16_decode(&msd[2]) != RexBionicsCompanyId)
				break;

			// Deserialise the sensor data
			rex_node_sensor_packet_t pkt;
			pkt.packet_start = DataStart;
			pkt.packet_length = sizeof(pkt);
			memcpy(&pkt.id, &peer_addr->addr[0], sizeof(pkt.id));
			memcpy(&pkt.data, &msd[4], sizeof(rex_node_sensor_data_t));
			pkt.crc = crc32_compute((uint8_t const*)&pkt, sizeof(pkt) - sizeof(pkt.crc), NULL);
			if (nrf_queue_push(&m_sensor_data, &pkt) != NRF_SUCCESS)
			{
				NRF_LOG_ERROR("Queue overflow!");
			}
			break;
		}
		default:
		{
			NRF_LOG_DEBUG("BLE: %s", BLEEventToString(evt->header.evt_id));
			break;
		}
	}
}

// Handle Scaning events.
#if 0
static void HandleBLEScanEvents(scan_evt_t const *evt)
{
	switch (evt->scan_evt_id)
	{
		case NRF_BLE_SCAN_EVT_NOT_FOUND:
		{
			break;
		}
		case NRF_BLE_SCAN_EVT_CONNECTING_ERROR:
		{
			Check(evt->params.connecting_err.err_code);
			break;
		}
		default:
		{
			NRF_LOG_DEBUG("Scan: %s", BLEScanEventToString(evt->scan_evt_id));
			break;
		}
	}
}
#endif

// Set up the BLE stack in the soft device as a non-connectable advertiser
ret_code_t BLE_Init()
{
	// Enable the soft device
	{
		// The softdevice contains the implementation of the bluetooth stack
		Check(nrf_sdh_enable_request());

		// Configure the BLE stack using the default settings
		// and fetch the start address of the application RAM.
		uint32_t ram_start = 0;
		Check(nrf_sdh_ble_default_cfg_set(APP_BLE_CONN_CFG_TAG, &ram_start));

		// Enable BLE stack.
		Check(nrf_sdh_ble_enable(&ram_start));

		// Register a handler for BLE events.
		NRF_SDH_BLE_OBSERVER(m_ble_observer, APP_BLE_OBSERVER_PRIO, HandleBLEEvents, NULL);
	}

	// Scanning setup
	{
		ble_gap_scan_params_t scan = {};
		scan.active = false;
		scan.filter_policy = BLE_GAP_SCAN_FP_ACCEPT_ALL;
		scan.interval = SCAN_INTERVAL;
		scan.window = SCAN_WINDOW;
		scan.timeout = BLE_GAP_SCAN_TIMEOUT_UNLIMITED;

		nrf_ble_scan_init_t init = {};
		init.p_scan_param = &scan;
		init.connect_if_match = false;
		init.conn_cfg_tag = APP_BLE_CONN_CFG_TAG;
		Check(nrf_ble_scan_init(&m_scan, &init, NULL));
	}

	// Enable scanning
	Check(nrf_ble_scan_start(&m_scan));
	return NRF_SUCCESS;
}

// Read received sensor data
bool BLE_ReceivedDataGet(rex_node_sensor_packet_t* data_out)
{
	ret_code_t r = nrf_queue_generic_pop(&m_sensor_data, data_out, false);
	if (r != NRF_SUCCESS && r != NRF_ERROR_NOT_FOUND) Check(r);
	return r == NRF_SUCCESS;
}
