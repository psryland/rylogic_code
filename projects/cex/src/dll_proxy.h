//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// nicked from: http://www.codeproject.com/Articles/16541/Create-your-Proxy-DLLs-automatically
// needs work...
//
// Generates a cpp source file that can be compiled into a wrapper dll
// You should replace functions that you know how to use. For example, If you want to spy on Wsock32.send():
// send, created by wrappit
// extern "C" __declspec(naked) void __stdcall __E__69__()
// {
//    __asm
//    {
//       jmp p[69*4];
//     }
// }
// If you want to manipulate it, change to:
// extern "C" int __stdcall __E__69__(SOCKET x,char* b,int l,int pr)
// {
//    // manipulate here parameters.....
//    // call original send
//    typedef int (__stdcall *pS)(SOCKET,char*,int,int);
//    pS pps = (pS)p[63*4];
//    int rv = pps(x,b,l,pr);
//    return rv;
// }


#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct DllProxy :ICex
	{
		std::string m_ifile;      // Input dll filepath
		std::string m_ofile;      // Output dll filepath
		std::string m_exports;    // The text file of function exports
		std::string m_convention; // Calling convention
		std::string m_cppfile;    // CPP filepath
		std::string m_deffile;    // DEF filepath
		bool        m_compile;
		
		DllProxy()
			:m_ifile()
			,m_ofile()
			,m_exports()
			,m_convention()
			,m_cppfile()
			,m_deffile()
			,m_compile(false)
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"Dll Proxy Generator\n"
				" Syntax: Cex -dllproxy -fi \"dll to proxy\" -exports \"function_list.txt\" [-convention \"conv\"] [-fo \"proxy dll name\"] [-cpp \"cpp filepath\"] [-def \"def filepath\"] [-compile]\n"
				"  -fi         : the input dll filepath to create a proxy for\n"
				"  -exports    : a text file containing the function signitures of the functions to proxy\n"
				"  -convention : the calling convention to use (default: __stdcall)\n"
				"  -fo         : the name of the created proxy dll (default: ifile.proxy.dll)\n"
				"  -cpp        : the generated cpp filepath (default: <local dir>\\ifile.cpp)\n"
				"  -def        : the generated def filepath (default: <local dir>\\ifile.def)\n"
				"  -compile    : attempt to compile the proxy dll using Cl.exe and Link.exe\n (default:false)"
				"\n";
		}
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-dllproxy"))
			{
				return true;
			}
			if (pr::str::EqualI(option, "-fi") && arg != arg_end)
			{
				m_ifile = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-exports") && arg != arg_end)
			{
				m_exports = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-convention") && arg != arg_end)
			{
				m_convention = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-fo") && arg != arg_end)
			{
				m_ofile = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-cpp") && arg != arg_end)
			{
				m_cppfile = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-def") && arg != arg_end)
			{
				m_deffile = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-compile"))
			{
				m_compile = true;
				return true;
			}
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		void ValidatedInput() override
		{
			if (m_ifile.empty())
			{
				throw std::exception("An input dll filepath must be given");
			}
			if (m_ofile.empty())
			{
				m_ofile = pr::filesys::GetFiletitle(m_ifile) + std::string(".proxy.dll");
			}
			if (m_exports.empty())
			{
				throw std::exception("An exports text filepath must be given");
			}
			if (m_convention.empty())
			{
				m_convention = "__stdcall";
			}
			if (m_cppfile.empty())
			{
				m_cppfile= pr::filesys::GetFiletitle(m_ifile) + std::string(".cpp");
			}
			if (m_deffile.empty())
			{
				m_deffile = pr::filesys::GetFiletitle(m_ifile) + std::string(".def");
			}
		}

		int Run() override
		{
			throw std::exception("Needs debugging, this currently deletes the source dll!");

			// Read exports
			std::cout << "Parsing " << m_ifile << "...\n";
			FILE* fp = _tfopen(m_ifile.c_str(), _T("rb"));
			if (!fp)
				throw std::exception("Failed to open input dll file");

			struct ExportItem
			{
				bool IsOnlyOrdinal;
				char in[300];
				char en[300];
				int o;
				int h;
			};
			std::vector<ExportItem> v;

			int MSStart = 0;
			for (int i = 0;;)
			{
				char x[1000] = {};
				ExportItem e = {};

				if (!fgets(x, 1000, fp))
					break;

				if (_strnicmp(x, "EXPORT ord", 10) == 0) // tdump
				{
					//EXPORT ord:1141='AcceptEx' => 'MSWSOCK.AcceptEx'
					e.o = atoi(x + 11);
					sprintf(e.in, "__E__%u__", i);
					char* y = strchr(x, '\'');
					if (y)
					{
						y++;
						char* y2 = strchr(y, '\'');
						if (y2)
							*y2 = 0;
						strcpy(e.en, y);
						e.IsOnlyOrdinal = false;
					}
					else
					{
						e.IsOnlyOrdinal = true;
						sprintf(e.en, "___XXX___%u", i);
					}
					v.insert(v.end(), e);
				}
				else
				{
					if (strstr(x, "ordinal") != 0 && strstr(x, "hint") != 0 && strstr(x, "RVA") != 0)
					{
						MSStart = 1;
						continue;
					}
					if (!MSStart)
						continue;
					char* a1 = x;
					while (*a1 == ' ')
						a1++;
					if (*a1 == '\r' || *a1 == '\n')
					{
						if (MSStart == 1)
						{
							MSStart = 2;
							continue;
						}
						break;
					}
					e.o = atoi(a1);
					while (*a1 != ' ')
						a1++;
					while (*a1 == ' ')
						a1++;
					if (*a1 == '\r' || *a1 == '\n')
					{
						if (MSStart == 1)
						{
							MSStart = 2;
							continue;
						}
						break;
					}
					e.h = atoi(a1);
					while (*a1 != ' ')
						a1++;
					while (*a1 == ' ')
						a1++;
					if (*a1 == '\r' || *a1 == '\n')
					{
						if (MSStart == 1)
						{
							MSStart = 2;
							continue;
						}
						break;
					}
					if (*a1 >= 0x30 && *a1 <= 0x39) // RVA exists
					{
						while (*a1 != ' ')
							a1++;
						while (*a1 == ' ')
							a1++;
						if (*a1 == '\r' || *a1 == '\n')
							break;
					}

					sprintf(e.in, "__E__%u__", i++);
					e.IsOnlyOrdinal = false;
					if (_strnicmp(a1, "[NONAME]", 8) == 0)
					{
						e.IsOnlyOrdinal = true;
						sprintf(e.en, "___XXX___%u", i);
					}
					else
					{
						for (int y = 0;; y++)
						{
							if (*a1 == ' ' || *a1 == '\r' || *a1 == '\n' || *a1 == '\t')
								break;
							e.en[y] = *a1++;
						}
					}
					v.insert(v.end(), e);
				}
			}
			fclose(fp);

			std::cout << v.size() << " exported functions parsed.\n";
			std::cout << "Generating .DEF file " << m_deffile << "...\n";

			FILE* fdef = _tfopen(m_deffile.c_str(), _T("wb"));
			if (!fdef)
				throw std::exception("DEF file cannot be created.");

			fprintf(fdef, "EXPORTS\r\n");
			for (unsigned int i = 0; i < v.size(); i++)
			{
				if (v[i].IsOnlyOrdinal == false)
					fprintf(fdef, "%s=%s @%u\r\n", v[i].en, v[i].in, v[i].o);
				else
					fprintf(fdef, "%s=%s @%u NONAME\r\n", v[i].en, v[i].in, v[i].o);
			}
			fclose(fdef);

			std::cout << v.size() << " exported functions written to DEF.\n";
			std::cout << "Generating .CPP file " << m_cppfile << "...\n";

			FILE* fcpp = _tfopen(m_cppfile.c_str(), _T("wb"));
			if (!fcpp)
				throw std::exception("CPP file cannot be created.");

			// Write headers
			fprintf(fcpp, "#include <windows.h>\r\n");
			fprintf(fcpp, "#pragma pack(1)\r\n\r\n\r\n");

			// Write variables
			fprintf(fcpp, "HINSTANCE hLThis = 0;\r\n");
			fprintf(fcpp, "HINSTANCE hL = 0;\r\n");
			fprintf(fcpp, "FARPROC p[%u] = {0};\r\n\r\n", v.size());

			// Write DllMain
			fprintf(fcpp, "BOOL WINAPI DllMain(HINSTANCE hInst,DWORD reason,LPVOID)\r\n");
			fprintf(fcpp, "\t{\r\n");
			fprintf(fcpp, "\tif (reason == DLL_PROCESS_ATTACH)\r\n");
			fprintf(fcpp, "\t\t{\r\n");

			fprintf(fcpp, "\t\thLThis = hInst;\r\n");
			fprintf(fcpp, "\t\thL = LoadLibrary(\"%s\");\r\n", pr::str::StringToCString<std::string>(m_ifile).c_str());
			fprintf(fcpp, "\t\tif (!hL) return false;\r\n");

			fprintf(fcpp, "\r\n\r\n");
			for (unsigned int i = 0; i < v.size(); i++)
			{
				if (v[i].IsOnlyOrdinal == true)
					fprintf(fcpp, "\t\tp[%u] = GetProcAddress(hL,(LPCSTR)\"%u\");\r\n", i, v[i].o);
				else
					fprintf(fcpp, "\t\tp[%u] = GetProcAddress(hL,\"%s\");\r\n", i, v[i].en);
			}

			fprintf(fcpp, "\r\n\r\n");
			fprintf(fcpp, "\t\t}\r\n");

			fprintf(fcpp, "\tif (reason == DLL_PROCESS_DETACH)\r\n");
			fprintf(fcpp, "\t\t{\r\n");
			fprintf(fcpp, "\t\tFreeLibrary(hL);\r\n");
			fprintf(fcpp, "\t\t}\r\n\r\n");

			fprintf(fcpp, "\treturn 1;\r\n");
			fprintf(fcpp, "\t}\r\n\r\n");

			// Write function to be exported
			for (unsigned int i = 0; i < v.size(); i++)
			{
				fprintf(fcpp, "// %s\r\nextern \"C\" __declspec(naked) void %s %s()\r\n", v[i].en, m_ofile.c_str(), v[i].in);
				fprintf(fcpp, "\t{\r\n");
				fprintf(fcpp, "\t__asm\r\n");
				fprintf(fcpp, "\t\t{\r\n");

				fprintf(fcpp, "\t\tjmp p[%u*%u];\r\n", i, sizeof(void*));

				fprintf(fcpp, "\t\t}\r\n");
				fprintf(fcpp, "\t}\r\n\r\n");
			}
			fclose(fcpp);

			if (m_compile)
			{
				TCHAR ay[1000] = { 0 };
				_stprintf(ay,_T("CL.EXE /O2 /GL /I \".\" /D \"WIN32\" /D \"NDEBUG\" /D \"_WINDOWS\" /D \"_WINDLL\" /FD /EHsc /MT /Fo\".\\%s.obj\" /Fd\".\\vc80.pdb\" /W3 /nologo /c /Wp64 /TP /errorReport:prompt %s\r\n"),m_cppfile.c_str(),m_cppfile.c_str());
				system(ay);
				_stprintf(ay,_T("LINK.EXE /OUT:\"%s\" /INCREMENTAL:NO /NOLOGO /DLL /MANIFEST /DEF:\"%s\" /SUBSYSTEM:WINDOWS /OPT:REF /OPT:ICF /LTCG /MACHINE:X86 /ERRORREPORT:PROMPT %s.obj kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib\r\n"),m_ifile.c_str(),m_deffile.c_str(),m_cppfile.c_str());
				system(ay);

				//	_stprintf(ay,_T("BCC32 -o%s.obj -c %s\r\n"),argv[5],argv[5]);
				//	_stprintf(ay,_T("ILINK32 -c -Tpd %s.obj,%s,,,%s\r\n"),argv[5],argv[1],argv[6]);
				system("pause");
			}
			return 0;
		}
	};
}
