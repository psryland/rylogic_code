//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/open_vs.h"

// import EnvDTE
#pragma warning(disable : 4278 4146 4471)
#import "libid:80cc9f66-e7d8-4ddd-85b6-d9e6cd0e93e2" version("8.0") lcid("0") raw_interfaces_only named_guids
#pragma warning(default : 4278 4146 4471)

#pragma comment(lib, "Rpcrt4.lib")

namespace cex
{
	OpenVS::OpenVS()
		:m_file()
		,m_line(0)
	{}

	void OpenVS::ShowHelp() const
	{
		std::cout <<
			"OpenVS: Open a file in an existing instance of visual studio\n"
			" Syntax: Cex -openvs \"filename\":line_number\n";
	}

	bool OpenVS::CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
	{
		if (pr::str::EqualI(option, "-openvs"))
		{
			std::size_t end = arg->find_last_of(':');
			if (end != 1 && end != std::string::npos) // not a drive colon
			{
				m_file = arg->substr(0, end);
				m_line = strtoul(arg->substr(end+1).c_str(), 0, 10);
			}
			else
			{
				m_file = *arg;
			}
			arg = arg_end;
			return true;
		}
		return ICex::CmdLineOption(option, arg, arg_end);
	}

	int OpenVS::Run()
	{
		using namespace ATL;

		HRESULT result;
		CLSID clsid;
		for (;;)
		{
			result = ::CLSIDFromProgID(L"VisualStudio.DTE.8.0", &clsid);
			if (FAILED(result)) break;

			CComPtr<IUnknown> punk;
			result = ::GetActiveObject(clsid, NULL, &punk);
			if (FAILED(result)) break;

			CComPtr<EnvDTE::_DTE> DTE;
			DTE = punk;

			CComPtr<EnvDTE::ItemOperations> item_ops;
			result = DTE->get_ItemOperations(&item_ops);
			if (FAILED(result)) break;

			CComBSTR bstrFileName(m_file.c_str());
			CComBSTR bstrKind(EnvDTE::vsViewKindTextView);
			CComPtr<EnvDTE::Window> window;
			result = item_ops->OpenFile(bstrFileName, bstrKind, &window);
			if (FAILED(result)) break;

			CComPtr<EnvDTE::Document> doc;
			result = DTE->get_ActiveDocument(&doc);
			if (FAILED(result)) break;

			CComPtr<IDispatch> selection_dispatch;
			result = doc->get_Selection(&selection_dispatch);
			if (FAILED(result)) break;

			CComPtr<EnvDTE::TextSelection> selection;
			result = selection_dispatch->QueryInterface(&selection);
			if (FAILED(result)) break;

			result = selection->GotoLine(m_line, TRUE);
			if (FAILED(result)) break;

			return 0;
		}
		std::cerr << "Failed to open file in VS.\nReason: " << pr::To<std::string>(result) << "\n";
		return -1;
	}
}