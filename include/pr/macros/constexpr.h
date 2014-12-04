//******************************************
// constexpr compatibility
//  Copyright (c) Rylogic Ltd 2014
//******************************************
#pragma once

// constexpr supported in version ???
#if defined(_MSC_VER) && _MSC_VER <= 1800
#define constexpr
#endif


