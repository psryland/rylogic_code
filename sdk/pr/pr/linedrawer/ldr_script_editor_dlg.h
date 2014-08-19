//***************************************************************************************************
// Ldr Script Editor Dialog
//  Copyright (c) Rylogic Ltd 2014
//***************************************************************************************************
#pragma once

#include <memory>
#include <functional>
#include <windows.h>

namespace pr
{
	namespace ldr
	{
		// Script editor interface
		struct IScriptEditorDlg
		{
			virtual ~IScriptEditorDlg() {}

			// Implicitly convertable to HWND
			virtual operator HWND() = 0;

			// Create the non-modal window
			virtual HWND Create(HWND parent = 0) = 0;

			// Close and destroy the dialog window
			virtual void Close() = 0;
			virtual void Detach() = 0;

			// Show the window as a non-modal window
			virtual void Show(HWND parent = 0) = 0;

			// Show the window as a modal dialog
			virtual INT_PTR ShowDialog(HWND parent = 0) = 0;

			// Get/Set the visibility of the window
			virtual bool Visible() const = 0;
			virtual void Visible(bool show) = 0;

			// Get/Set the text in the dialog
			virtual std::string Text() const = 0;
			virtual void Text(char const* text) = 0;

			// Callback function for rendering the script
			typedef std::function<void(std::string&& script)> RenderCB;
		};

		// A gui for editing ldr script
		class ScriptEditorDlg :IScriptEditorDlg
		{
			// pImpl pattern to hide atl includes
			std::unique_ptr<IScriptEditorDlg> m_dlg;

			ScriptEditorDlg(ScriptEditorDlg const&);
			ScriptEditorDlg& operator=(ScriptEditorDlg const&);

		public:
			ScriptEditorDlg();

			// Implicitly convertable to HWND
			operator HWND() override
			{
				return m_dlg->operator HWND();
			}

			// Create the non-modal window
			HWND Create(HWND parent) override
			{
				return m_dlg->Create(parent);
			}

			// Close and destroy the dialog window
			void Close() override
			{
				m_dlg->Close();
			}
			void Detach() override
			{
				m_dlg->Detach();
			}

			// Show the window as a non-modal window
			void Show(HWND parent = 0) override
			{
				m_dlg->Show(parent);
			}

			// Show the window as a modal dialog
			INT_PTR ShowDialog(HWND parent = 0) override
			{
				return m_dlg->ShowDialog(parent);
			}

			// Get/Set the visibility of the window
			bool Visible() const override
			{
				return m_dlg->Visible();
			}
			void Visible(bool show) override
			{
				m_dlg->Visible(show);
			}

			// Get/Set the text in the dialog
			std::string Text() const override
			{
				return m_dlg->Text();
			}
			void Text(char const* text) override
			{
				m_dlg->Text(text);
			}

			// Callback function for rendering the script
			RenderCB Render;
		};
	}
}