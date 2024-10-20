//*****************************************************************************************
// VT100 Termianl colours
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
#pragma once

namespace pr::gui
{
	// Terminal colors
	struct TermColours
	{
		inline static constexpr char const* Reset = "\033[0m";

		// Regular Colors
		inline static constexpr char const* Red = "\033[31m";
		inline static constexpr char const* Green = "\033[32m";
		inline static constexpr char const* Yellow = "\033[33m";
		inline static constexpr char const* Blue = "\033[34m";
		inline static constexpr char const* Magenta = "\033[35m";
		inline static constexpr char const* Cyan = "\033[36m";
		inline static constexpr char const* White = "\033[37m";
		inline static constexpr char const* Gray = "\033[90m";

		// Bright Colors
		inline static constexpr char const* BrightBlack = "\033[90m";
		inline static constexpr char const* BrightRed = "\033[91m";
		inline static constexpr char const* BrightGreen = "\033[92m";
		inline static constexpr char const* BrightYellow = "\033[93m";
		inline static constexpr char const* BrightBlue = "\033[94m";
		inline static constexpr char const* BrightMagenta = "\033[95m";
		inline static constexpr char const* BrightCyan = "\033[96m";
		inline static constexpr char const* BrightWhite = "\033[97m";

		// Background Colors
		inline static constexpr char const* Bgrd_Black = "\033[40m";
		inline static constexpr char const* Bgrd_Red = "\033[41m";
		inline static constexpr char const* Bgrd_Green = "\033[42m";
		inline static constexpr char const* Bgrd_Yellow = "\033[43m";
		inline static constexpr char const* Bgrd_Blue = "\033[44m";
		inline static constexpr char const* Bgrd_Magenta = "\033[45m";
		inline static constexpr char const* Bgrd_Cyan = "\033[46m";
		inline static constexpr char const* Bgrd_White = "\033[47m";

		// Bright Background Colors
		inline static constexpr char const* Bgrd_BrightBlack = "\033[100m";
		inline static constexpr char const* Bgrd_BrightRed = "\033[101m";
		inline static constexpr char const* Bgrd_BrightGreen = "\033[102m";
		inline static constexpr char const* Bgrd_BrightYellow = "\033[103m";
		inline static constexpr char const* Bgrd_BrightBlue = "\033[104m";
		inline static constexpr char const* Bgrd_BrightMagenta = "\033[105m";
		inline static constexpr char const* Bgrd_BrightCyan = "\033[106m";
		inline static constexpr char const* Bgrd_BrightWhite = "\033[107m";

		// All colours as an array
		inline static constexpr char const* AllColours[] =
		{
			Red,
			Green,
			Yellow,
			Blue,
			Magenta,
			Cyan,
			White,
			Gray,
			BrightBlack,
			BrightRed,
			BrightGreen,
			BrightYellow,
			BrightBlue,
			BrightMagenta,
			BrightCyan,
			BrightWhite,
			Bgrd_Black,
			Bgrd_Red,
			Bgrd_Green,
			Bgrd_Yellow,
			Bgrd_Blue,
			Bgrd_Magenta,
			Bgrd_Cyan,
			Bgrd_White,
			Bgrd_BrightBlack,
			Bgrd_BrightRed,
			Bgrd_BrightGreen,
			Bgrd_BrightYellow,
			Bgrd_BrightBlue,
			Bgrd_BrightMagenta,
			Bgrd_BrightCyan,
			Bgrd_BrightWhite,
		};
	};
}
