//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// ReadText: Read text content from a window using UI Automation.
#include "src/forward.h"
#include "src/commands/process_util.h"

#include <UIAutomation.h>

namespace cex
{
	namespace
	{
		// Recursively collect text from a UI Automation element tree
		void CollectText(IUIAutomation* uia, IUIAutomationElement* element, int depth, std::string& output, int max_depth)
		{
			if (depth > max_depth) return;

			auto indent = std::string(depth * 2, ' ');

			// Get element name
			BSTR name = nullptr;
			element->get_CurrentName(&name);

			// Get control type
			CONTROLTYPEID type_id = 0;
			element->get_CurrentControlType(&type_id);

			// Get the element's text content
			std::string text_content;

			// Try ValuePattern first (text boxes, editable fields)
			IUIAutomationValuePattern* val_pattern = nullptr;
			if (SUCCEEDED(element->GetCurrentPatternAs(UIA_ValuePatternId, IID_PPV_ARGS(&val_pattern))) && val_pattern)
			{
				BSTR val = nullptr;
				if (SUCCEEDED(val_pattern->get_CurrentValue(&val)) && val)
				{
					auto len = SysStringLen(val);
					if (len > 0)
					{
						auto needed = WideCharToMultiByte(CP_UTF8, 0, val, static_cast<int>(len), nullptr, 0, nullptr, nullptr);
						text_content.resize(needed);
						WideCharToMultiByte(CP_UTF8, 0, val, static_cast<int>(len), text_content.data(), needed, nullptr, nullptr);
					}
					SysFreeString(val);
				}
				val_pattern->Release();
			}

			// Try TextPattern (rich text controls, documents)
			if (text_content.empty())
			{
				IUIAutomationTextPattern* text_pattern = nullptr;
				if (SUCCEEDED(element->GetCurrentPatternAs(UIA_TextPatternId, IID_PPV_ARGS(&text_pattern))) && text_pattern)
				{
					IUIAutomationTextRange* range = nullptr;
					if (SUCCEEDED(text_pattern->get_DocumentRange(&range)) && range)
					{
						BSTR text = nullptr;
						if (SUCCEEDED(range->GetText(4096, &text)) && text)
						{
							auto len = SysStringLen(text);
							if (len > 0)
							{
								auto needed = WideCharToMultiByte(CP_UTF8, 0, text, static_cast<int>(len), nullptr, 0, nullptr, nullptr);
								text_content.resize(needed);
								WideCharToMultiByte(CP_UTF8, 0, text, static_cast<int>(len), text_content.data(), needed, nullptr, nullptr);
							}
							SysFreeString(text);
						}
						range->Release();
					}
					text_pattern->Release();
				}
			}

			// Convert the name to UTF-8
			std::string name_str;
			if (name)
			{
				auto len = SysStringLen(name);
				if (len > 0)
				{
					auto needed = WideCharToMultiByte(CP_UTF8, 0, name, static_cast<int>(len), nullptr, 0, nullptr, nullptr);
					name_str.resize(needed);
					WideCharToMultiByte(CP_UTF8, 0, name, static_cast<int>(len), name_str.data(), needed, nullptr, nullptr);
				}
				SysFreeString(name);
			}

			// Map control type to a readable string
			auto type_name = [](CONTROLTYPEID id) -> char const*
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
				case UIA_TitleBarControlTypeId:  return "TitleBar";
				default:                         return "Other";
				}
			};

			// Output the element info if it has a name or text
			if (!name_str.empty() || !text_content.empty())
			{
				output += std::format("{}[{}] '{}'\n", indent, type_name(type_id), name_str);
				if (!text_content.empty())
				{
					// Truncate long text for readability
					auto display = text_content.size() > 200 ? text_content.substr(0, 200) + "..." : text_content;
					output += std::format("{}  text: {}\n", indent, display);
				}
			}

			// Recurse into children
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
							CollectText(uia, child, depth + 1, output, max_depth);
							child->Release();
						}
					}
					children->Release();
				}
				true_cond->Release();
			}
		}
	}

	struct Cmd_ReadText
	{
		void ShowHelp() const
		{
			std::cout <<
				"ReadText: Read text content from a window using UI Automation\n"
				" Syntax: Cex -read_text -p <process-name> [-w <window-name>] [-depth N]\n"
				"  -p     : Name (or partial name) of the target process\n"
				"  -w     : Title (or partial title) of the target window (default: largest)\n"
				"  -depth : Maximum tree depth to traverse (default: 5)\n"
				"\n"
				"  Reads text from UI elements using the Windows UI Automation API.\n"
				"  Outputs the element tree with names, control types, and text values.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			int max_depth = 5;
			if (args.count("depth") != 0) { max_depth = args("depth").as<int>(); }

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
			if (FAILED(hr) || !element) { std::cerr << "Failed to get UI Automation element for window\n"; uia->Release(); return -1; }

			std::cout << std::format("Reading text from '{}'\n", GetWindowTitle(hwnd));

			std::string output;
			CollectText(uia, element, 0, output, max_depth);

			if (output.empty())
				std::cout << "(no text elements found)\n";
			else
				std::cout << output;

			element->Release();
			uia->Release();
			return 0;
		}
	};

	int ReadText(pr::CmdLine const& args)
	{
		Cmd_ReadText cmd;
		return cmd.Run(args);
	}
}
