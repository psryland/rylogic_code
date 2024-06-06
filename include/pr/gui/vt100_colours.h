#pragma once

namespace pr::vt100::colour
{
	static constexpr char const* RESET = "\033[0m";

	// Regular Colors
	static constexpr char const* RED = "\033[31m";
	static constexpr char const* GREEN = "\033[32m";
	static constexpr char const* YELLOW = "\033[33m";
	static constexpr char const* BLUE = "\033[34m";
	static constexpr char const* MAGENTA = "\033[35m";
	static constexpr char const* CYAN = "\033[36m";
	static constexpr char const* WHITE = "\033[37m";
	static constexpr char const* GRAY = "\033[90m";
	
	// Bright Colors
	static constexpr char const* BRIGHT_BLACK = "\033[90m";
	static constexpr char const* BRIGHT_RED = "\033[91m";
	static constexpr char const* BRIGHT_GREEN = "\033[92m";
	static constexpr char const* BRIGHT_YELLOW = "\033[93m";
	static constexpr char const* BRIGHT_BLUE = "\033[94m";
	static constexpr char const* BRIGHT_MAGENTA = "\033[95m";
	static constexpr char const* BRIGHT_CYAN = "\033[96m";
	static constexpr char const* BRIGHT_WHITE = "\033[97m";
	
	// Background Colors
	static constexpr char const* BG_BLACK = "\033[40m";
	static constexpr char const* BG_RED = "\033[41m";
	static constexpr char const* BG_GREEN = "\033[42m";
	static constexpr char const* BG_YELLOW = "\033[43m";
	static constexpr char const* BG_BLUE = "\033[44m";
	static constexpr char const* BG_MAGENTA = "\033[45m";
	static constexpr char const* BG_CYAN = "\033[46m";
	static constexpr char const* BG_WHITE = "\033[47m";
	
	// Bright Background Colors
	static constexpr char const* BG_BRIGHT_BLACK = "\033[100m";
	static constexpr char const* BG_BRIGHT_RED = "\033[101m";
	static constexpr char const* BG_BRIGHT_GREEN = "\033[102m";
	static constexpr char const* BG_BRIGHT_YELLOW = "\033[103m";
	static constexpr char const* BG_BRIGHT_BLUE = "\033[104m";
	static constexpr char const* BG_BRIGHT_MAGENTA = "\033[105m";
	static constexpr char const* BG_BRIGHT_CYAN = "\033[106m";
	static constexpr char const* BG_BRIGHT_WHITE = "\033[107m";

	#if PR_VT100_COLOUR_DEFINES

		#define TC_RESET "\033[0m"

		// Regular Colors
		#define TC_RED "\033[31m"
		#define TC_GREEN "\033[32m"
		#define TC_YELLOW "\033[33m"
		#define TC_BLUE "\033[34m"
		#define TC_MAGENTA "\033[35m"
		#define TC_CYAN "\033[36m"
		#define TC_WHITE "\033[37m"
		#define TC_GRAY "\033[90m"
		
		// Bright Colors
		#define TC_BRIGHT_BLACK "\033[90m"
		#define TC_BRIGHT_RED "\033[91m"
		#define TC_BRIGHT_GREEN "\033[92m"
		#define TC_BRIGHT_YELLOW "\033[93m"
		#define TC_BRIGHT_BLUE "\033[94m"
		#define TC_BRIGHT_MAGENTA "\033[95m"
		#define TC_BRIGHT_CYAN "\033[96m"
		#define TC_BRIGHT_WHITE "\033[97m"
		
		// Background Colors
		#define TC_BG_BLACK "\033[40m"
		#define TC_BG_RED "\033[41m"
		#define TC_BG_GREEN "\033[42m"
		#define TC_BG_YELLOW "\033[43m"
		#define TC_BG_BLUE "\033[44m"
		#define TC_BG_MAGENTA "\033[45m"
		#define TC_BG_CYAN "\033[46m"
		#define TC_BG_WHITE "\033[47m"
		
		// Bright Background Colors
		#define TC_BG_BRIGHT_BLACK "\033[100m"
		#define TC_BG_BRIGHT_RED "\033[101m"
		#define TC_BG_BRIGHT_GREEN "\033[102m"
		#define TC_BG_BRIGHT_YELLOW "\033[103m"
		#define TC_BG_BRIGHT_BLUE "\033[104m"
		#define TC_BG_BRIGHT_MAGENTA "\033[105m"
		#define TC_BG_BRIGHT_CYAN "\033[106m"
		#define TC_BG_BRIGHT_WHITE "\033[107m"

	#endif
}