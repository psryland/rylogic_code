#pragma once

#include <d3d11.h>

#include <pr/common/d3dptr.h>
#include <pr/gui/wingui.h>
#include <pr/renderer11/renderer.h>
#include <pr/storage/settings.h>
#include <pr/storage/sqlite.h>

#include "SpaceTrucker/res/resources.h"

#pragma warning (disable:4355)

namespace st
{
	using namespace pr::wingui;
	using namespace pr::sqlite;
	inline char const* AppVersionString() { return "v1.0"; }
}