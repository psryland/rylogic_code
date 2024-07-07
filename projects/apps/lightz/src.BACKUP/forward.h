#pragma once

#include <span>
#include <vector>
#include <deque>
#include <string>
#include <string_view>
#include <unordered_map>
#include <algorithm>
#include <type_traits>
#include <thread>
#include <chrono>
#include <optional>
#include <stdexcept>
#include <cstring>

#include "build/config/sdkconfig.h"
#include "freertos/FreeRTOS.h"
#include "driver/gpio.h"
//#include "esp_littlefs.h"
// #include <Arduino.h>
// #include <Stream.h>
// #include <WString.h>
// #include <LittleFS.h>
//#include <ESP32Console.h>
// #include <FastLED.h>
// #include <WiFi.h>
// #include <driver/rmt.h>
// #include <esp_intr_alloc.h>

// This is wrong on the ESP32-S3-DevKitC-1-n16r8v board
#undef RGB_BUILTIN

namespace lightz
{
	// Constants
	//constexpr auto SerialBaudRate = 921600;
	constexpr auto BuiltInLED = gpio_num_t::GPIO_NUM_47;
}
