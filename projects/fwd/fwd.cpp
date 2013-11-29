#include <iostream>
#include <windows.h>

int wmain(int argc, wchar_t* argv[])
{
	std::wstring s;
	for (int i = 1; i < argc; ++i)
		s.append(argv[i]).append(L" ");

	SECURITY_ATTRIBUTES attr = {sizeof(SECURITY_ATTRIBUTES), 0};
	attr.bInheritHandle = TRUE;
	attr.lpSecurityDescriptor = nullptr;

	PROCESS_INFORMATION pi = {0};
	STARTUPINFOW info = {sizeof(STARTUPINFOW), 0};
	auto creation_flags = NORMAL_PRIORITY_CLASS;//|CREATE_NO_WINDOW;
	auto r = CreateProcessW(nullptr, &s[0], &attr, &attr, FALSE, creation_flags, NULL, NULL, &info, &pi);
	if (!r)
	{
		auto hr = GetLastError();
		wchar_t msg[256];
		FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), msg, 256, NULL);
		std::wcerr << L"Fwd: CreateProcess() failed for [" << s.c_str() << L"]\nReason: " << msg << "\n";
		return 1;
	}

	// Successfully created the process.  Wait for it to finish.
	WaitForSingleObject(pi.hProcess, INFINITE);

	// Get the exit code.
	DWORD exit_code;
	r = GetExitCodeProcess(pi.hProcess, &exit_code);

	// Close the handles.
	CloseHandle(pi.hProcess);
	CloseHandle(pi.hThread);

	if (!r)
	{
		// Could not get exit code.
		std::wcerr << L"Fwd: Executed progress but couldn't get exit code for [" << s.c_str() << L"]\n";
		return 1;
	}

	// We succeeded.
	return exit_code;
}