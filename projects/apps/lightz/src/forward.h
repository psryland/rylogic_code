#pragma once

#include <vector>
#include <deque>
#include <string>
#include <string_view>
#include <unordered_map>
#include <algorithm>
#include <optional>

#include <Arduino.h>
#include <Stream.h>
#include <WString.h>
#include <LittleFS.h>
#include <ESP32Console.h>
#include <FastLED.h>
#include <WiFi.h>
#include <rtc.h>

// This is wrong on the ESP32-S3-DevKitC-1-n16r8v board
#undef RGB_BUILTIN

namespace lightz
{
	extern int const SerialBaudRate;
	extern uint8_t const BuiltInLED;
}
