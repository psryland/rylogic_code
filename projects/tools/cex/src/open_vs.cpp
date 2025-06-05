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
	template <typename T> using Ptr = pr::RefPtr<T>;

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
		volatile bool todo = true;
		if (todo)
			throw std::runtime_error("This needs updating... if it's being used..");

		HRESULT result;
		CLSID clsid;
		for (;;)
		{
			result = ::CLSIDFromProgID(L"VisualStudio.DTE.8.0", &clsid);
			if (FAILED(result)) break;

			Ptr<IUnknown> punk;
			result = ::GetActiveObject(clsid, NULL, punk.address_of());
			if (FAILED(result)) break;

			Ptr<EnvDTE::_DTE> DTE;
			DTE = punk;

			Ptr<EnvDTE::ItemOperations> item_ops;
			result = DTE->get_ItemOperations(item_ops.address_of());
			if (FAILED(result)) break;

			auto filepath = m_file.wstring();
			auto view_kind = pr::Widen(EnvDTE::vsViewKindTextView);
			BSTR bstrFileName = ::SysAllocStringLen(filepath.c_str(), UINT(filepath.size()));
			BSTR bstrKind = ::SysAllocStringLen(view_kind.c_str(), UINT(view_kind.size()));
			Ptr<EnvDTE::Window> window;
			result = item_ops->OpenFile(bstrFileName, bstrKind, window.address_of());
			if (FAILED(result))
				break;

			Ptr<EnvDTE::Document> doc;
			result = DTE->get_ActiveDocument(doc.address_of());
			if (FAILED(result)) break;

			Ptr<IDispatch> selection_dispatch;
			result = doc->get_Selection(selection_dispatch.address_of());
			if (FAILED(result)) break;

			Ptr<EnvDTE::TextSelection> selection;
			result = selection_dispatch->QueryInterface(selection.address_of());
			if (FAILED(result)) break;

			result = selection->GotoLine(m_line, TRUE);
			if (FAILED(result)) break;

			return 0;
		}
		std::cerr << "Failed to open file in VS.\nReason: " << pr::To<std::string>(result) << "\n";
		return -1;
	}
}
