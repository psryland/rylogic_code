#include "ir_sensor.h"
#include "utils/ring_buffer.h"
#include "utils/stack_monitor.h"

namespace lightz
{
	constexpr uint32_t IRSensorRecvStackSize = 2048;
	constexpr auto IRSensorInputPin = gpio_num_t::GPIO_NUM_4;

	IRSensor::IRSensor()
		: m_channel(rmt_channel_t::RMT_CHANNEL_4)
		, m_buffer()
	{}

	// This function is empty
	void IRSensor::Setup()
	{
		esp_err_t res;

		// Configure the RMT peripheral
		rmt_config_t config = RMT_DEFAULT_CONFIG_RX(IRSensorInputPin, m_channel);
		if ((res = rmt_config(&config)) != ESP_OK)
		{
			Serial.printf("Failed to configure RMT. Code: %d\r\n", static_cast<int>(res));
			return;
		}

		// Install the system ISR 
		if ((res = rmt_driver_install(m_channel, 1000, ESP_INTR_FLAG_IRAM)) != ESP_OK)
		{
			Serial.printf("Failed to install RMT driver. Code: %d", res);
			return;
		}

		// Get the ring buffer used by the RMT
		if ((res = rmt_get_ringbuf_handle(m_channel, &m_buffer)) != ESP_OK || m_buffer == nullptr)
		{
			Serial.printf("Failed to get RMT ring buffer handle. Code: %d", res);
			return;
		}

		 // Create a task to process received codes
		xTaskCreate([](void* ctx){ static_cast<IRSensor*>(ctx)->IRSensorRecv(); }, "IRSensorRecv", IRSensorRecvStackSize, this, 1, nullptr);

		// Start the receiver
		rmt_rx_start(m_channel, true);
		Serial.printf("RMT RX started\r\n");
	}

	// Read codes from the ring buffer
	void IRSensor::IRSensorRecv()
	{
		StackMonitor stack_mon("IRSensor", IRSensorRecvStackSize);

		for (;;)
		{
			stack_mon();

			RingBufferItem<rmt_item32_t> items(m_buffer);
			for (size_t i = 0; i != items.size(); ++i)
			{
				// Process each item here. For example, log the high and low duration.
				Serial.printf("Item %d: LEVEL0: %d, DURATION0: %d, LEVEL1: %d, DURATION1: %d\r\n",
						i, items[i].level0, items[i].duration0, items[i].level1, items[i].duration1);
			}
		}
	}
}
