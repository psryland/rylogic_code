#pragma once

#include <iostream>
#include <exception>
#include <cassert>
#include <Windows.h>
#include <wininet.h>

#include "quickmail/include/quickmail.h"

#include "pr/common/min_max_fix.h"
#include "pr/common/fmt.h"
#include "pr/common/flags_enum.h"
#include "pr/maths/bit_fields.h"
#include "pr/str/string_core.h"
#include "pr/storage/zip_file.h"
#include "pr/gui/wingui.h"

#pragma comment(lib, "wininet.lib")