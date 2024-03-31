// Copyright (c) Rex Bionics 2020
#include "forward.h"
#include "device/millisecond_ticker.h"

// Declare an instance of nrf_drv_rtc for RTC2
const nrf_drv_rtc_t rtc = NRF_DRV_RTC_INSTANCE(2);

// Millisecond resolution rolling counter
static volatile uint32_t m_ms_ticker;

// Function for handling the RTC0 interrupts.
static void rtc_handler(nrf_drv_rtc_int_type_t int_type)
{
	if (int_type == NRF_DRV_RTC_INT_TICK)
	{
		// This handler is called every 1024 times/second.
		// For each call, add 1000/1024 to 'tick1024' and increment
		// 'm_ms_ticker' whenever 'tick1024' is greater than 1.
		static uint16_t tick1024;
		tick1024 += 1000;
		if (tick1024 >= 1024)
		{
			tick1024 -= 1024;
			++m_ms_ticker;
		}
	}
	if (int_type == NRF_DRV_RTC_INT_COMPARE0)
	{
		// Not using output compare currently
	}
}

// Set up the RTC and app timers
void Ticker_Init()
{
	// Initialize 32kHz lf RTC instance
	//  Xtal => 32768 ticks/second
	//  *1/32 => 1024 ticks/second
	nrf_drv_rtc_config_t config = NRF_DRV_RTC_DEFAULT_CONFIG;
	config.prescaler = 32;
	Check(nrf_drv_rtc_init(&rtc, &config, &rtc_handler));

	// Enable tick event & interrupt
	nrf_drv_rtc_tick_enable(&rtc, true);

	// // Set compare channel to trigger interrupt after X seconds
	// Check(nrf_drv_rtc_cc_set(&rtc, 0, X * 1024, true));

	// Power on RTC instance
	nrf_drv_rtc_enable(&rtc);
}

// Return the ticker value
uint32_t Ticker_Get()
{
	return m_ms_ticker;
}
