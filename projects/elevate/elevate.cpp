#include <iostream>
#include <windows.h>
#include "pr/str/tostring.h"
#include "pr/common/registrykey.h"

// Create a process from the given command line
int Execute(std::string& cmdline)
{
	SECURITY_ATTRIBUTES attr = {sizeof(SECURITY_ATTRIBUTES), 0};
	attr.bInheritHandle = TRUE;
	attr.lpSecurityDescriptor = nullptr;

	PROCESS_INFORMATION pi = {0};
	STARTUPINFOA info = {sizeof(STARTUPINFOA), 0};
	auto creation_flags = NORMAL_PRIORITY_CLASS;//|CREATE_NO_WINDOW;
	auto r = CreateProcessA(nullptr, &cmdline[0], &attr, &attr, FALSE, creation_flags, NULL, NULL, &info, &pi);
	if (!r)
	{
		auto hr = GetLastError();
		wchar_t msg[256];
		FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), msg, 256, NULL);
		std::cerr << "Elevate: CreateProcess() failed for [" << cmdline << L"]\nReason: " << msg << "\n";
		std::cerr << "Ensure the \"Run As Administrator\" option is checked under the compatibility tab in Properties.\n";
		return -1;
	}

	// Successfully created the process.  Wait for it to finish.
	WaitForSingleObject(pi.hProcess, INFINITE);

	// Get the exit code.
	DWORD exit_code;
	r = GetExitCodeProcess(pi.hProcess, &exit_code);
	if (!r)
	{
		// Could not get exit code.
		std::cerr << "Elevate: Executed progress but couldn't get exit code for [" << cmdline << "]\n";
		return -1;
	}

	// Close the handles.
	CloseHandle(pi.hProcess);
	CloseHandle(pi.hThread);

	return int(exit_code);
}

int main(int argc, char* argv[])
{
	// Compile the command line
	std::string cmdline;
	for (int i = 1; i < argc; ++i) cmdline.append(argv[i]).append(" ");
	if (cmdline.empty())
	{
		std::cout
			<< "Elevate: This program is used to run other programs with elevated permissions\n"
			<< "Use:  Elevate.exe <program> <arguments>\n";
		return -1;
	}

	try
	{
		// Get the full path to this executable
		std::string elevate_path;
		{
			std::string mod_name;
			mod_name.resize(MAX_PATH);
			mod_name.resize(::GetModuleFileNameA(nullptr, &mod_name[0], DWORD(mod_name.size())));
			if (mod_name.empty())
			{
				std::cerr << "Failed to read the full path of elevate.exe\nError code" << GetLastError();
				return -1;
			}

			elevate_path.resize(MAX_PATH);
			elevate_path.resize(::GetFullPathNameA(mod_name.c_str(), DWORD(elevate_path.size()), &elevate_path[0], nullptr));
			if (elevate_path.empty())
			{
				std::cerr << "Failed to read the full path of elevate.exe\nError code" << GetLastError();
				return -1;
			}
		}

		auto subkey = "Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";

		// Check that this program has the reg option set for 'Run As Admin'
		// If the "Run As Admin" option is not set, prompt to set it, then recursively run this program
		// Don't do this automatically because if something causes this not to work unlimited processes will start
		bool reg_value_found = false;
		{
			auto key = pr::RegistryKey(HKEY_CURRENT_USER, subkey, pr::registry::EAccess::KeyRead);
			reg_value_found = key.HasValue(elevate_path.c_str());
		}
		if (!reg_value_found)
		{
			std::cerr << "Elevate: 'Run As Administrator' property has not been set.\nSet it now (you should only need to do this once) (Y/N)? ";
			auto ch = std::getchar();
			if (ch == 'y' || ch == 'Y')
			{
				auto key = pr::RegistryKey(HKEY_CURRENT_USER, subkey, pr::registry::EAccess::KeyWrite);
				key.Write(elevate_path.c_str(), "~ RUNASADMIN");
			
				std::cerr << "You will need to re-run this process for the changes to take effect\n";
				return -1;
			}
			else
			{
				std::cerr << " ...Aborted.\n";
				return -1;
			}
		}

		// Otherwise, start the process from the command line
		return Execute(cmdline);
	}
	catch (std::exception const& ex)
	{
		std::cerr << "Elevate: Failed to run [" << cmdline << L"] as Administrator\n" << ex.what() << "\n";
		return -1;
	}
}