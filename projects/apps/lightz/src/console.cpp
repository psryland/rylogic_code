#include "console.h"
#include "config.h"
#include "utils/utils.h"

namespace lightz
{
	// Although filesys prepends '/root' to the file path, the console component doesn't.
	// If you use an invalid path, the file system fails to mount
	char const* Console::HistoryFilePath = "/root/.history.txt";
	
	Console::Console()
		: ESP32Console::Console()
	{}

	void Console::Setup()
	{
		begin(SerialBaudRate);
		registerCoreCommands();
		registerVFSCommands();
		registerSystemCommands();
		registerNetworkCommands();
		//registerGPIOCommands();

		registerCommand(ESP32Console::ConsoleCommand("config", &Console::Config, "Set configuration options", "<section.key>[=<value>] | <show> | <save> | <load>"));

		enablePersistentHistory(HistoryFilePath);
		setPrompt("%pwd%> ");
	}

	// Usage: config <key> <value>
	int Console::Config(int argc, char **argv)
	{
		auto arg1 = argc >= 2 ? std::string_view{argv[1]} : std::string_view{};
		auto arg2 = argc >= 3 ? std::string_view{argv[2]} : std::string_view{};

		if (argc == 1 || arg1 == "show")
		{
			config.Print();
			return EXIT_SUCCESS;
		}
		if (arg1 == "save")
		{
			config.Save();
			return EXIT_SUCCESS;
		}
		if (arg1 == "load")
		{
			config.Load();
			return EXIT_SUCCESS;
		}
		if (argc == 2)
		{
			auto eq = arg1.find('=');
			if (eq != std::string_view::npos)
			{
				arg2 = arg1.substr(eq + 1);
				arg1 = arg1.substr(0, eq);
				if (config.Set(arg1, arg2))
				{
					printf("%.*s updated to %.*s.\nRemember to save config\n", PRINTF_SV(arg1), PRINTF_SV(arg2));
					return EXIT_SUCCESS;
				}
				else
				{
					printf("Unknown setting: %.*s\n", PRINTF_SV(arg1));
					return EXIT_FAILURE;
				}
			}
			else
			{
				printf("%.*s: %s\n", PRINTF_SV(arg1), config.Get(arg1).c_str());
				return EXIT_SUCCESS;
			}
		}

		printf("Usage: config <section.key>[=<value>] | <show> | <save> | <load>\n");
		return EXIT_FAILURE;
	}
}
