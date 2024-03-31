#ifndef CUSTOM_BOARD_H
#define CUSTOM_BOARD_H

#ifdef __cplusplus
extern "C" {
#endif

	#include "nrf_gpio.h"

	// Pin assignments
	enum
	{
		// 
		LED_GREEN = NRF_GPIO_PIN_MAP(0,6),

		// Not using this LED because it is the same pin as the kill
		// pin on the RexNode sensor. If you accidentally flash the sensor
		// with the dongle code, the sensor won't start up.
		//LED_RED = NRF_GPIO_PIN_MAP(0,8),

		// 
		LED_BLUE = NRF_GPIO_PIN_MAP(0,12),

		// 
		LED_GREEN2 = NRF_GPIO_PIN_MAP(1,9),
	};

	enum
	{
		// LEDs are active low
		LED_ON = 0,
		LED_OFF = 1,
	};

#ifdef __cplusplus
}
#endif
#endif
