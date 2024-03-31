// Copyright (c) Rex Bionics 2020
#ifndef APP_CONFIG_H
#define APP_CONFIG_H

// Driver modules
// 'nrfx' is new, 'nrf_drv' is old. Prefer 'nrfx'
// However:
//   - nrf_drv_clock, nrf_drv_power, and nrf_drv_rng are not supported by the
//     newer drivers yet.  Watch out for 'apply_old_config.h' which silently
//     redefines things.
//   - nrf_drv_clock is actually implemented using nrfx_clock...
//
#define RTC_ENABLED 1
#define RTC2_ENABLED 1
#define GPIOTE_ENABLED 1
#define NRFX_USBD_ENABLED 1

// Library modules
#define NRF_RTC_ENABLED 1
#define NRF_CLI_ENABLED 1
#define NRF_CLOCK_ENABLED 1
#define NRF_QUEUE_ENABLED 1
#define NRF_QUEUE_CLI_CMDS 1
#define NRF_BALLOC_ENABLED 1
#define NRF_PWR_MGMT_ENABLED 1

// Logging
#define NRF_LOG_ENABLED 1
#define NRF_LOG_CLI_CMDS 1
#define NRF_LOG_DEFAULT_LEVEL 4 // debug
#define NRF_LOG_STR_PUSH_BUFFER_SIZE 512

// Soft device modules
#define NRF_SDH_ENABLED 1
#define NRF_SDH_BLE_ENABLED 1
#define NRF_SDH_SOC_ENABLED 1

// Application modules
#define APP_TIMER_ENABLED 1
#define APP_TIMER_KEEPS_RTC_ACTIVE 1
#define APP_TIMER_CONFIG_RTC_FREQUENCY 0
#define APP_TIMER_V2 1
#define APP_TIMER_V2_RTC1_ENABLED 1
#define BUTTON_ENABLED 1
#define CRC32_ENABLED 1

// USB
#define APP_USBD_ENABLED 1
#define APP_USBD_CDC_ACM_ENABLED 1
#define APP_USBD_CONFIG_EVENT_QUEUE_ENABLE 0
#define APP_USBD_CONFIG_SELF_POWERED 0

// RTT CLI
#define NRF_CLI_RTT_ENABLED 1
#define NRF_CLI_RTT_TERMINAL_ID 0
#define NRF_CLI_RTT_TX_RETRY_DELAY_MS 10
#define NRF_CLI_RTT_TX_RETRY_CNT 5

// USB CLI
#define NRF_CLI_CDC_ACM_ENABLED 1
#define NRF_CLI_CDC_ACM_COMM_INTERFACE 2
#define NRF_CLI_CDC_ACM_DATA_INTERFACE 3
#define NRF_CLI_CDC_ACM_COMM_EPIN  NRF_DRV_USBD_EPIN4
#define NRF_CLI_CDC_ACM_DATA_EPIN  NRF_DRV_USBD_EPIN3
#define NRF_CLI_CDC_ACM_DATA_EPOUT NRF_DRV_USBD_EPOUT3
#define CLI_OVER_USB_CDC_ACM 1

// SEGGER RTT
#define SEGGER_RTT_CONFIG_BUFFER_SIZE_UP 512
#define SEGGER_RTT_CONFIG_MAX_NUM_UP_BUFFERS 2
#define SEGGER_RTT_CONFIG_BUFFER_SIZE_DOWN 16
#define SEGGER_RTT_CONFIG_MAX_NUM_DOWN_BUFFERS 2
#define SEGGER_RTT_CONFIG_DEFAULT_MODE 0

// BLE
#define NRF_BLE_SCAN_ENABLED 1
#define NRF_BLE_SCAN_BUFFER 128                  // Data length for an advertising set. 
#define NRF_BLE_SCAN_NAME_MAX_LEN 32             // Maximum size for the name to search in the advertisement report. 
#define NRF_BLE_SCAN_SHORT_NAME_MAX_LEN 32       // Maximum size of the short name to search for in the advertisement report. 
#define NRF_BLE_SCAN_SCAN_INTERVAL 160           // Scanning interval. Determines the scan interval in units of 0.625 millisecond. 
#define NRF_BLE_SCAN_SCAN_DURATION 0             // Duration of a scanning session in units of 10 ms. Range: 0x0001 - 0xFFFF (10 ms to 10.9225 ms). If set to 0x0000, the scanning continues until it is explicitly disabled. 
#define NRF_BLE_SCAN_SCAN_WINDOW 80              // Scanning window. Determines the scanning window in units of 0.625 millisecond. 
#define NRF_BLE_SCAN_MIN_CONNECTION_INTERVAL 7.5 // Determines minimum connection interval in milliseconds. 
#define NRF_BLE_SCAN_MAX_CONNECTION_INTERVAL 30  // Determines maximum connection interval in milliseconds. 
#define NRF_BLE_SCAN_SLAVE_LATENCY 0             // Determines the slave latency in counts of connection events. 
#define NRF_BLE_SCAN_SUPERVISION_TIMEOUT 4000    // Determines the supervision time-out in units of 10 millisecond. 
#define NRF_BLE_SCAN_SCAN_PHY 1                  // PHY to scan on. <0=> BLE_GAP_PHY_AUTO  <1=> BLE_GAP_PHY_1MBPS  <2=> BLE_GAP_PHY_2MBPS  <4=> BLE_GAP_PHY_CODED  <255=> BLE_GAP_PHY_NOT_SET 

// BLE Scan Filter
#define NRF_BLE_SCAN_FILTER_ENABLE 1
#define NRF_BLE_SCAN_UUID_CNT 0       // Number of filters for UUIDs. 
#define NRF_BLE_SCAN_NAME_CNT 1       // Number of name filters. 
#define NRF_BLE_SCAN_SHORT_NAME_CNT 0 // Number of short name filters. 
#define NRF_BLE_SCAN_ADDRESS_CNT 0    // Number of address filters. 
#define NRF_BLE_SCAN_APPEARANCE_CNT 0 // Number of appearance filters. 

// USB device config
#include "usbd_config.h"

// This "sdk_config.h" is supposed to be the "global-contains-everything-reference" sdk config
// file, except it doesn't contain everything due to Nordic's shoddy software development.
// Defines can be copied from various example projects and pasted into this 'app_config'. If
// Nordic ever get their act together and add these to the global 'sdk_config' then we should
// have already overridden them.
#include "sdk_config.h"

#endif