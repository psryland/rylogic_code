#include "console.h"
#include "utils/utils.h"

namespace lightz
{
	// Singleton instance of the console
	Console console;

	Console::Console()
		:m_repl()
	{
		// Register commands
		Check(esp_console_register_help_command());
		Register({
			.command = "version",
			.help = "Get version of chip and SDK",
			.hint = NULL,
			.func = &Console::Cmd_Version,
			.argtable = nullptr,
		});

		// Setup the console
		esp_console_repl_config_t repl_config = {
			.max_history_len = 32,
			.history_save_path = nullptr, //"/root/history.txt",
			.task_stack_size = 4096,
			.task_priority = 2,
			.prompt = ">",
			.max_cmdline_length = 0,
		};
		esp_console_dev_uart_config_t hw_config = ESP_CONSOLE_DEV_UART_CONFIG_DEFAULT();
		Check(esp_console_new_repl_uart(&hw_config, &repl_config, &m_repl));
	}

	// Start the console
	void Console::Start()
	{
		Check(esp_console_start_repl(m_repl));
	}

	// Register a console command
	void Console::Register(esp_console_cmd_t const& cmd)
	{
		Check(esp_console_cmd_register(&cmd));
	}

	// 'version' command
	int Console::Cmd_Version(int argc, char **argv)
	{
		esp_chip_info_t info;
		esp_chip_info(&info);

		const char* model;
		switch(info.model)
		{
			case CHIP_ESP32:   model = "ESP32"; break;
			case CHIP_ESP32S2: model = "ESP32-S2"; break;
			case CHIP_ESP32S3: model = "ESP32-S3"; break;
			case CHIP_ESP32C3: model = "ESP32-C3"; break;
			case CHIP_ESP32H2: model = "ESP32-H2"; break;
			case CHIP_ESP32C2: model = "ESP32-C2"; break;
			default:           model = "Unknown"; break;
		}

		uint32_t flash_size;
		Check(esp_flash_get_size(nullptr, &flash_size), "Get flash size failed");

		printf("IDF Version:%s\r\n", esp_get_idf_version());
		printf("Chip info:\r\n");
		printf("\tmodel:%s\r\n", model);
		printf("\tcores:%d\r\n", info.cores);
		printf("\tfeature:%s%s%s%s%" PRIu32 "%s\r\n",
			info.features & CHIP_FEATURE_WIFI_BGN ? "/802.11bgn" : "",
			info.features & CHIP_FEATURE_BLE ? "/BLE" : "",
			info.features & CHIP_FEATURE_BT ? "/BT" : "",
			info.features & CHIP_FEATURE_EMB_FLASH ? "/Embedded-Flash:" : "/External-Flash:",
			flash_size / (1024 * 1024), " MB");
		printf("\trevision number:%d\r\n", info.revision);
		return 0;
	}

}





#if 0
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
#endif
