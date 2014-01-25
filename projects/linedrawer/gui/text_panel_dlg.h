//***************************************************************************************************
// Lighting Dialog
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_LDR_TEXT_PANEL_DLG_H
#define PR_LDR_TEXT_PANEL_DLG_H

#include "linedrawer/main/forward.h"
#include "linedrawer/resources/linedrawer.resources.h"

class CTextEntryDlg
	:public CDialogImpl<CTextEntryDlg>
	,public CDialogResize<CTextEntryDlg>
{
	HWND  m_parent;
	CEdit m_edit;
	CFont m_font;
	bool  m_multiline;

public:
	std::string m_title;
	std::string m_body;
	int m_width;
	int m_height;

	enum { IDD = IDD_DIALOG_TEXT_ENTRY };
	BEGIN_MSG_MAP(CTextEntryDlg)
		MESSAGE_HANDLER(WM_INITDIALOG             ,OnInitDialog)
		COMMAND_ID_HANDLER(IDOK                   ,OnCloseDialog)
		COMMAND_ID_HANDLER(IDCANCEL               ,OnCloseDialog)
		CHAIN_MSG_MAP(CDialogResize<CTextEntryDlg>)
	END_MSG_MAP()
	BEGIN_DLGRESIZE_MAP(CTextEntryDlg)
		DLGRESIZE_CONTROL(IDC_EDIT_TEXT_ENTRY, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
		DLGRESIZE_CONTROL(IDOK, DLSZ_MOVE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
		DLGRESIZE_CONTROL(IDCANCEL, DLSZ_MOVE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
	END_DLGRESIZE_MAP()

	CTextEntryDlg(HWND parent, char const* title, char const* body, bool multiline)
	:m_parent(parent)
	,m_edit()
	,m_font()
	,m_multiline(multiline)
	,m_title(title)
	,m_body(body)
	,m_width(-1)
	,m_height(-1)
	{
		m_font.CreatePointFont(80, "courier new");
	}

	// Handler methods
	LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
	{
		SetWindowTextA(m_title.c_str());
		if (m_multiline) ModifyStyle(DS_MODALFRAME, WS_THICKFRAME);
		else             ModifyStyle(WS_THICKFRAME, DS_MODALFRAME);

		m_edit.Attach(GetDlgItem(IDC_EDIT_TEXT_ENTRY));
		if (m_multiline) m_edit.ModifyStyle(0, ES_MULTILINE|ES_WANTRETURN|WS_HSCROLL|WS_VSCROLL|ES_AUTOVSCROLL);
		else             m_edit.ModifyStyle(ES_MULTILINE|ES_WANTRETURN, 0);
		m_edit.SetTabStops(12);
		m_edit.SetFont(m_font);
		m_edit.SetWindowText(m_body.c_str());
		m_edit.SetSel(0, -1);
		m_edit.SetFocus();

		DlgResize_Init(m_multiline);
		ResizeClient(m_width, m_height);
		CenterWindow(m_parent);
		return S_OK;
	}
	LRESULT OnCloseDialog(WORD, WORD wID, HWND, BOOL&)
	{
		m_body.resize(m_edit.GetWindowTextLengthA() + 1);
		m_edit.GetWindowTextA(&m_body[0], (int)m_body.size());
		while (!m_body.empty() && *(--m_body.end()) == 0) m_body.resize(m_body.size() - 1);
		m_edit.Detach();
		EndDialog(wID);
		return S_OK;
	}
};

#endif
