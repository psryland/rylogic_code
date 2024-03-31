// Copyright (c) Rex Bionics 2020
#include "ui/user_interface.h"
#include "config/custom_board.h"

// Device heartbeat
static bool m_heartbeat_pending;
APP_TIMER_DEF(m_timer_heartbeat);
static void HandleHeartbeat(void* context)
{
	UNUSED_PARAMETER(context);
	m_heartbeat_pending = true;
}

// Initialise user interface support (i.e. board specific LEDs/Buttons/etc)
void UserInterface_Init()
{
	// If nRF52 USB Dongle is powered from USB (high voltage mode),
	// GPIO output voltage is set to 1.8 V by default, which is not
	// enough to turn on green and blue LEDs. Therefore, GPIO voltage
	// needs to be increased to 3.0 V by configuring the UICR register.
	if (NRF_POWER->MAINREGSTATUS & (POWER_MAINREGSTATUS_MAINREGSTATUS_High << POWER_MAINREGSTATUS_MAINREGSTATUS_Pos))
	{
		// Configure UICR_REGOUT0 register only if it is set to default value.
		if ((NRF_UICR->REGOUT0 & UICR_REGOUT0_VOUT_Msk) == (UICR_REGOUT0_VOUT_DEFAULT << UICR_REGOUT0_VOUT_Pos))
		{
			NRF_NVMC->CONFIG = NVMC_CONFIG_WEN_Wen;
			for (; NRF_NVMC->READY == NVMC_READY_READY_Busy;){}

			NRF_UICR->REGOUT0 = (NRF_UICR->REGOUT0 & ~((uint32_t)UICR_REGOUT0_VOUT_Msk)) | (UICR_REGOUT0_VOUT_3V0 << UICR_REGOUT0_VOUT_Pos);
			NRF_NVMC->CONFIG = NVMC_CONFIG_WEN_Ren;
			for (; NRF_NVMC->READY == NVMC_READY_READY_Busy;){}

			// System reset is needed to update UICR registers.
			NVIC_SystemReset();
		}
	}

	Check(nrf_drv_gpiote_init());

	// Set up LEDs
	nrf_gpio_cfg_output(LED_GREEN);
	//nrf_gpio_cfg_output(LED_RED);
	nrf_gpio_cfg_output(LED_BLUE);
	{
		// Light the LEDs for a second as a start up indication
		//nrf_gpio_pin_write(LED_RED, LED_ON);
		nrf_gpio_pin_write(LED_BLUE, LED_ON);
		nrf_gpio_pin_write(LED_GREEN, LED_ON);
		nrf_delay_ms(1000);
		//nrf_gpio_pin_write(LED_RED, LED_OFF);
		nrf_gpio_pin_write(LED_BLUE, LED_OFF);
		nrf_gpio_pin_write(LED_GREEN, LED_OFF);
	}

	// Create a heartbeat timer to wake the process up so we can report stability state.
	Check(app_timer_create(&m_timer_heartbeat, APP_TIMER_MODE_REPEATED, &HandleHeartbeat));
	Check(app_timer_start(m_timer_heartbeat, APP_TIMER_TICKS(50), NULL));
}

// Main loop processing for the UI
void UserInterface_Process()
{
	// Only update every heartbeat timer tick, no need to run any faster
	if (!m_heartbeat_pending)
		return;

	uint32_t count_ms = APP_TIMER_MS(app_timer_cnt_get());

	// Give heartbeat feedback via the LEDs
	{
		uint32_t count = count_ms & 0x7ff; // millisecond count modulus ~2s
		nrf_gpio_pin_write(LED_GREEN, count < 1000 ? LED_ON : LED_OFF);
	}
	m_heartbeat_pending = false;
}