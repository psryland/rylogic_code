#include "console.h"
#include "config.h"

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

		registerCommand(ESP32Console::ConsoleCommand("config", &Console::Config, "Set configuration options", "<key> <value> | <show> | <save>"));

		enablePersistentHistory(HistoryFilePath);
		setPrompt("%pwd%> ");
	}

	// Usage: config <key> <value>
	int Console::Config(int argc, char **argv)
	{
		auto arg1 = argc >= 2 ? String{argv[1]} : String{};
		auto arg2 = argc >= 3 ? String{argv[2]} : String{};

		if (arg1 == "show")
		{
			config.Print();
			return EXIT_SUCCESS;
		}
		if (arg1 == "save")
		{
			config.Save();
			return EXIT_SUCCESS;
		}
		if (argc == 2)
		{
			printf("%s: %s\n", arg1.c_str(), config.Get(arg1).c_str());
			return EXIT_SUCCESS;
		}
		if (argc == 3)
		{
			return config.Set(arg1, arg2) ? EXIT_SUCCESS : EXIT_FAILURE;
		}

		printf("Usage: config <key> <value> | <show> | <save>\n");
		return EXIT_FAILURE;
	}
}
