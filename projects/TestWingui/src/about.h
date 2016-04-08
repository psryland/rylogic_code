#pragma once

#include "forward.h"

// About from dialog resource
struct About :Form
{
	Button m_btn_ok;

	// Construct from a dialog resource.
	// Note, not all of the controls are required in the class. The ones that
	// are get bound to the controls in the created dialog using the id
	About()
		:Form(DlgParams<>().name("about").id(IDD_ABOUTBOX).start_pos(EStartPosition::CentreParent))
		,m_btn_ok(Button::Params<>().parent(this_).name("btn-ok").id(IDOK).anchor(EAnchor::BottomRight))
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};

// About from auto generated dialog template
struct About2 :Form
{
	enum { ID_LBL_VERSION = 100, ID_LBL_COPYRIGHT };

	Label m_lbl_version;
	Label m_lbl_copyright;
	Button m_btn_ok;

	About2()
		:Form(DlgParams<>().name("about2").title(L"About2 - TestWinGUI").wh(170,62).start_pos(EStartPosition::CentreParent))
		//   ICON            IDR_MAINFRAME,IDC_STATIC,14,14,20,20
		,m_lbl_version  (Label ::Params<>().parent(this_).name("lbl-version  ").text(L"TestWinGUI, Version 1.0").id(ID_LBL_VERSION  ).xy(42,14).wh(114,8).style('+',SS_NOPREFIX))
		,m_lbl_copyright(Label ::Params<>().parent(this_).name("lbl-copyright").text(L"Copyright (C) 2014"     ).id(ID_LBL_COPYRIGHT).xy(42,26).wh(114,8))
		,m_btn_ok       (Button::Params<>().parent(this_).name("btn-ok       ").text(L"OK"                     ).id(IDOK            ).xy(113,41).wh(50,14).def_btn().anchor(EAnchor::BottomRight))
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};