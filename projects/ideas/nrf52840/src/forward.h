// Copyright (c) Rex Bionics 2020
#ifndef FORWARD_H
#define FORWARD_H

#include <stdbool.h>
#include <stdint.h>
#include <string.h>
#include <math.h>

// nRF5 SDK
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wunused-parameter"
#include "modules/nrfx/nrfx.h"
#include "modules/nrfx/drivers/include/nrfx_rtc.h"
#include "modules/nrfx/drivers/include/nrfx_usbd.h"
#include "modules/nrfx/drivers/include/nrfx_gpiote.h"
#include "modules/nrfx/hal/nrf_gpio.h"
#include "modules/nrfx/hal/nrf_gpiote.h"
#include "modules/nrfx/mdk/nrf.h"
#include "integration/nrfx/legacy/nrf_drv_clock.h"
#include "integration/nrfx/legacy/nrf_drv_gpiote.h"
#include "integration/nrfx/legacy/nrf_drv_power.h"
#include "integration/nrfx/legacy/nrf_drv_rtc.h"
#include "integration/nrfx/legacy/nrf_drv_usbd.h"
#include "components/boards/boards.h"
#include "components/ble/common/ble_srv_common.h"
#include "components/ble/common/ble_conn_params.h"
#include "components/ble/ble_advertising/ble_advertising.h"
#include "components/ble/nrf_ble_scan/nrf_ble_scan.h"
#include "components/libraries/bsp/bsp.h"
#include "components/libraries/cli/nrf_cli_vt100.h"
#include "components/libraries/cli/rtt/nrf_cli_rtt.h"
#include "components/libraries/cli/cdc_acm/nrf_cli_cdc_acm.h"
#include "components/libraries/crc32/crc32.h"
#include "components/libraries/delay/nrf_delay.h"
#include "components/libraries/log/nrf_log.h"
#include "components/libraries/log/nrf_log_ctrl.h"
#include "components/libraries/log/nrf_log_default_backends.h"
#include "components/libraries/pwr_mgmt/nrf_pwr_mgmt.h"
#include "components/libraries/timer/app_timer.h"
#include "components/libraries/usbd/app_usbd.h"
#include "components/libraries/usbd/app_usbd_core.h"
#include "components/libraries/usbd/app_usbd_serial_num.h"
#include "components/libraries/usbd/app_usbd_string_desc.h"
#include "components/libraries/usbd/class/cdc/acm/app_usbd_cdc_acm.h"
#include "components/libraries/util/app_error.h"
#include "components/softdevice/common/nrf_sdh.h"
#include "components/softdevice/common/nrf_sdh_ble.h"
#include "components/softdevice/common/nrf_sdh_soc.h"
#include "components/softdevice/s140/headers/ble.h"
#include "components/softdevice/s140/headers/ble_gap.h"
#include "components/softdevice/s140/headers/nrf_error.h"
#include "components/softdevice/s140/headers/nrf_sdm.h"
#pragma GCC diagnostic pop

// Project includes
#include "common/sensor_data.h"
#include "common/float_format.h"
#include "repo_revision.h"
#include "version.h"

// Magic bytes sent before each packet (Rex Bionics Zero Ref)
enum { DataStart = ('R'<<24) | ('B'<<16) | ('Z'<<8) | ('R'<<0) };

// Wrap the return code error check in an inline function so
// that the return code can be seen in the debugger.
__STATIC_INLINE void Check0(ret_code_t err_code, uint32_t line_number, char const * filename)
{
	if (err_code == NRF_SUCCESS) return;
	extern void app_error_handler(uint32_t error_code, uint32_t line_number, const uint8_t * filename);
	app_error_handler(err_code, line_number, (uint8_t const*)filename);
}
#define Check(status) Check0((status), __LINE__, __FILE__);

// Hide a few if (x != NRF_SUCCESS) {} statements
#define ReturnOnError(status)\
	do {\
		uint32_t err_code = (status);\
		if (err_code != NRF_SUCCESS)\
			return err_code;\
	} while (0)

// The mirror of APP_TIMER_TICKS(ms)
#define APP_TIMER_MS(ticks)\
	((uint32_t)ROUNDED_DIV(1000 * (ticks) * (APP_TIMER_CONFIG_RTC_FREQUENCY + 1),\
	(uint64_t)APP_TIMER_CLOCK_FREQ))

#endif
