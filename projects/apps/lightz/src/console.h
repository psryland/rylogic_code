#pragma once
#include "forward.h"

namespace lightz
{
	struct Console : ESP32Console::Console
	{
		static char const* HistoryFilePath;

		Console();
		void Setup();

		// Console commands
		static int Config(int argc, char **argv);
	};

	// Singleton instance of the console
	extern Console console;
}
