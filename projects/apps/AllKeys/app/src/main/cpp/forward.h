#pragma once

#include <set>
#include <memory>
#include <string_view>
#include <concepts>
#include <type_traits>

#include <jni.h>
#include <android/log.h>

// If missing, you need to run the project setup script first. That will download the
// fluidsynth SDK and copy files into the correct location.
#include "fluidsynth.h"
#include "jni_string.h"

namespace allkeys
{
}