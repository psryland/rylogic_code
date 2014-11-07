//******************************************
// constexpr compatibility
//  Copyright (c) Rylogic Ltd 2014
//******************************************
#pragma once

// constexpr supported in version ???
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER <= 180030723
#define constexpr
#endif


