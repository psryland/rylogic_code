#include "forward.h"
#include "console.h"
#include "utils/utils.h"

namespace lightz
{
	static const char* TAG = "lightz";
	constexpr gpio_num_t BuiltInLED = gpio_num_t::GPIO_NUM_47;

	void main()
	{
		ESP_LOGI(TAG, "Starting...");
		console.Start();

		auto conf = gpio_config_t {
			.pin_bit_mask = 1ULL << BuiltInLED,
			.mode = gpio_mode_t::GPIO_MODE_OUTPUT,
			.pull_up_en = gpio_pullup_t::GPIO_PULLUP_DISABLE,
			.pull_down_en = gpio_pulldown_t::GPIO_PULLDOWN_DISABLE,
			.intr_type = gpio_int_type_t::GPIO_INTR_DISABLE,
		};
		Check(gpio_config(&conf));

		constexpr int period = 100 / portTICK_PERIOD_MS;
		int state = 0;
		for (;;)
		{
			Check(gpio_set_level(BuiltInLED, state), {{{}, "Failed to set LED state"}});
			state = !state;
			//printf("LED %s\n", state ? "ON" : "OFF");
			vTaskDelay(period);
		}
	}
}

extern "C" void app_main(void)
{
	lightz::main();
}
