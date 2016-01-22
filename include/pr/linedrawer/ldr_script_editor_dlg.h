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
		struct ScriptEditorDlg
		{
			// Callback function for rendering the script
			typedef std::function<void(std::wstring&& script)> RenderCB;

			virtual ~ScriptEditorDlg() {}
			ScriptEditorDlg(HWND parent, RenderCB render_cb = nullptr);
			ScriptEditorDlg(ScriptEditorDlg const&) = delete;
			ScriptEditorDlg& operator=(ScriptEditorDlg const&) = delete;

			// Implicitly convertible to HWND
			virtual operator HWND() const { return m_dlg->operator HWND(); }

			// Hide the window instead of closing
			virtual bool HideOnClose() const { return m_dlg->HideOnClose(); }
			virtual void HideOnClose(bool enable) { m_dlg->HideOnClose(enable); }

			// Show the window as a non-modal window
			virtual void Show(HWND parent = 0) { m_dlg->Show(parent); }

			// Show the window as a modal dialog
			virtual INT_PTR ShowDialog(HWND parent = 0) { return m_dlg->ShowDialog(parent); }

			// Position the window relative to the owner window
			virtual void PositionWindow(int x, int y, int w, int h) { return m_dlg->PositionWindow(x,y,w,h); }

			// Get/Set the visibility of the window
			virtual bool Visible() const { return m_dlg->Visible(); }
			virtual void Visible(bool show) { m_dlg->Visible(show); }

			// Get/Set the text in the dialog
			virtual std::wstring Text() const { return m_dlg->Text(); }
			virtual void Text(wchar_t const* text) { m_dlg->Text(text); }

			// Get/Set the script render callback function
			virtual RenderCB Render() const { return m_dlg->Render(); }
			virtual void Render(RenderCB cb) { m_dlg->Render(cb); }

		protected:
			// pImpl pattern to hide window implementation
			std::unique_ptr<ScriptEditorDlg> m_dlg;

			struct Internal {};
			ScriptEditorDlg(Internal) {}
		};
	}
}