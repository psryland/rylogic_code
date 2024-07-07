#pragma once
#include "forward.h"

namespace lightz
{
	struct Console
	{
		esp_console_repl_t* m_repl;

		Console();
		void Start();

	private:

		// Register a console command
		void Register(esp_console_cmd_t const& cmd);
		static int Cmd_Version(int argc, char **argv);
	};

	// Singleton instance of the console
	extern Console console;

	#if 0
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
	#endif
}
