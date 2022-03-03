//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************
#pragma once

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0400
#endif

#include <algorithm>
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <tchar.h>

#include "pr/common/fmt.h"
#include "pr/common/cast.h"
#include "pr/common/hresult.h"
#include "pr/common/pe_file.h"

using std::min;
using std::max;

class CTableData;
class CMetadataTables;
