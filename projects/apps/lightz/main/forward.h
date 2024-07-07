#pragma once

#include <span>
#include <vector>
#include <deque>
#include <string>
#include <string_view>
#include <unordered_map>
#include <initializer_list>
#include <algorithm>
#include <type_traits>
#include <thread>
#include <chrono>
#include <optional>
#include <stdexcept>
#include <cstring>

#include "../build/config/sdkconfig.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "driver/gpio.h"
#include "esp_chip_info.h"
#include "esp_console.h"
#include "esp_err.h"
#include "esp_event.h"
#include "esp_flash.h"
#include "esp_log.h"
#include "esp_spiffs.h"
#include "esp_system.h"
#include "esp_wifi.h"
#include "nvs_flash.h"

namespace lightz
{
}
