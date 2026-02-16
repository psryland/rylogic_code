//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// FindElement: Find a UI element by name and return its bounding rectangle.
#include "src/forward.h"
#include "src/commands/process_util.h"

#include <UIAutomation.h>

namespace cex
{
	namespace
	{
		// Convert BSTR to UTF-8 std::string
		std::string BstrToUtf8(BSTR bstr)
		{
			if (!bstr) return {};
			auto len = SysStringLen(bstr);
			if (len == 0) return {};
			auto needed = WideCharToMultiByte(CP_UTF8, 0, bstr, static_cast<int>(len), nullptr, 0, nullptr, nullptr);
			std::string result(needed, '\0');
			WideCharToMultiByte(CP_UTF8, 0, bstr, static_cast<int>(len), result.data(), needed, nullptr, nullptr);
			return result;
		}

		// Convert UTF-8 to wide string
		std::wstring Utf8ToWide(std::string const& str)
		{
			if (str.empty()) return {};
			auto needed = MultiByteToWideChar(CP_UTF8, 0, str.data(), static_cast<int>(str.size()), nullptr, 0);
			std::wstring result(needed, L'\0');
			MultiByteToWideChar(CP_UTF8, 0, str.data(), static_cast<int>(str.size()), result.data(), needed);
			return result;
		}

		struct FoundElement
		{
			std::string name;
			std::string type;
			RECT rect;
			RECT client_rect; // relative to the target window's client area
		};

		// Map control type to readable string
		char const* TypeName(CONTROLTYPEID id)
		{
			switch (id)
			{
			case UIA_ButtonControlTypeId:    return "Button";
			case UIA_TextControlTypeId:      return "Text";
			case UIA_EditControlTypeId:      return "Edit";
			case UIA_ListControlTypeId:      return "List";
			case UIA_ListItemControlTypeId:  return "ListItem";
			case UIA_MenuControlTypeId:      return "Menu";
			case UIA_MenuItemControlTypeId:  return "MenuItem";
			case UIA_TabControlTypeId:       return "Tab";
			case UIA_TabItemControlTypeId:   return "TabItem";
			case UIA_TreeControlTypeId:      return "Tree";
			case UIA_TreeItemControlTypeId:  return "TreeItem";
			case UIA_CheckBoxControlTypeId:  return "CheckBox";
			case UIA_ComboBoxControlTypeId:  return "ComboBox";
			case UIA_WindowControlTypeId:    return "Window";
			case UIA_PaneControlTypeId:      return "Pane";
			case UIA_ToolBarControlTypeId:   return "ToolBar";
			case UIA_StatusBarControlTypeId: return "StatusBar";
			case UIA_DocumentControlTypeId:  return "Document";
			case UIA_GroupControlTypeId:     return "Group";
			case UIA_HyperlinkControlTypeId: return "Hyperlink";
			case UIA_ImageControlTypeId:     return "Image";
			default:                         return "Other";
			}
		}

		// Recursively search for elements matching a name
		void SearchElements(IUIAutomation* uia, IUIAutomationElement* element, std::string const& search_lower, HWND hwnd, std::vector<FoundElement>& results, int depth, int max_depth)
		{
			if (depth > max_depth) return;

			BSTR name = nullptr;
			element->get_CurrentName(&name);
			auto name_str = BstrToUtf8(name);
			if (name) SysFreeString(name);

			CONTROLTYPEID type_id = 0;
			element->get_CurrentControlType(&type_id);

			// Case-insensitive substring match
			auto name_lower = name_str;
			std::transform(name_lower.begin(), name_lower.end(), name_lower.begin(), [](char c) { return static_cast<char>(tolower(c)); });

			if (!name_str.empty() && name_lower.find(search_lower) != std::string::npos)
			{
				RECT rc = {};
				element->get_CurrentBoundingRectangle(&rc);

				// Convert screen rect to client-area coords
				POINT top_left = { rc.left, rc.top };
				POINT bot_right = { rc.right, rc.bottom };
				ScreenToClient(hwnd, &top_left);
				ScreenToClient(hwnd, &bot_right);

				FoundElement fe;
				fe.name = name_str;
				fe.type = TypeName(type_id);
				fe.rect = rc;
				fe.client_rect = { top_left.x, top_left.y, bot_right.x, bot_right.y };
				results.push_back(fe);
			}

			// Recurse
			IUIAutomationElementArray* children = nullptr;
			IUIAutomationCondition* true_cond = nullptr;
			uia->CreateTrueCondition(&true_cond);
			if (true_cond)
			{
				if (SUCCEEDED(element->FindAll(TreeScope_Children, true_cond, &children)) && children)
				{
					int count = 0;
					children->get_Length(&count);
					for (int i = 0; i != count; ++i)
					{
						IUIAutomationElement* child = nullptr;
						if (SUCCEEDED(children->GetElement(i, &child)) && child)
						{
							SearchElements(uia, child, search_lower, hwnd, results, depth + 1, max_depth);
							child->Release();
						}
					}
					children->Release();
				}
				true_cond->Release();
			}
		}
	}

	struct Cmd_FindElement
	{
		void ShowHelp() const
		{
			std::cout <<
				"FindElement: Find a UI element by name and return its bounding rectangle\n"
				" Syntax: Cex -find_element -name <text> -p <process-name> [-w <window-name>] [-depth N]\n"
				"  -name  : Text to search for (case-insensitive substring match)\n"
				"  -p     : Name (or partial name) of the target process\n"
				"  -w     : Title (or partial title) of the target window (default: largest)\n"
				"  -depth : Maximum tree depth to traverse (default: 8)\n"
				"\n"
				"  Searches the UI Automation tree for elements whose name contains the\n"
				"  given text. Outputs the control type, name, and bounding rectangle\n"
				"  in both screen and client-area coordinates.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string search_text;
			if (args.count("name") != 0) { search_text = args("name").as<std::string>(); }

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			int max_depth = 8;
			if (args.count("depth") != 0) { max_depth = args("depth").as<int>(); }

			if (search_text.empty())  { std::cerr << "No search text provided (-name)\n"; return ShowHelp(), -1; }
			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }

			auto hwnd = FindWindow(process_name, window_name);
			if (!hwnd)
			{
				auto target = window_name.empty() ? process_name : std::format("{}:{}", process_name, window_name);
				std::cerr << std::format("No window found for '{}'\n", target);
				return -1;
			}

			// COM is already initialised by main.cpp (COINIT_APARTMENTTHREADED)
			IUIAutomation* uia = nullptr;
			auto hr = CoCreateInstance(CLSID_CUIAutomation, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&uia));
			if (FAILED(hr) || !uia) { std::cerr << "Failed to create UI Automation instance\n"; return -1; }

			IUIAutomationElement* element = nullptr;
			hr = uia->ElementFromHandle(hwnd, &element);
			if (FAILED(hr) || !element) { std::cerr << "Failed to get UI element for window\n"; uia->Release(); return -1; }

			auto search_lower = search_text;
			std::transform(search_lower.begin(), search_lower.end(), search_lower.begin(), [](char c) { return static_cast<char>(tolower(c)); });

			std::vector<FoundElement> results;
			SearchElements(uia, element, search_lower, hwnd, results, 0, max_depth);

			if (results.empty())
			{
				std::cerr << std::format("No elements matching '{}' found\n", search_text);
				element->Release();
				uia->Release();
				return 1;
			}

			std::cout << std::format("{} element(s) matching '{}':\n", results.size(), search_text);
			for (auto const& fe : results)
			{
				auto cw = fe.client_rect.right - fe.client_rect.left;
				auto ch = fe.client_rect.bottom - fe.client_rect.top;
				auto ccx = (fe.client_rect.left + fe.client_rect.right) / 2;
				auto ccy = (fe.client_rect.top + fe.client_rect.bottom) / 2;

				std::cout << std::format("  [{}] '{}'\n", fe.type, fe.name);
				std::cout << std::format("    client: ({},{}) {}x{} center: ({},{})\n",
					fe.client_rect.left, fe.client_rect.top, cw, ch, ccx, ccy);
			}

			element->Release();
			uia->Release();
			return 0;
		}
	};

	int FindElement(pr::CmdLine const& args)
	{
		Cmd_FindElement cmd;
		return cmd.Run(args);
	}
}
