#pragma once

#include "forward.h"

namespace pr::gui
{
	// About from dialog resource
	struct About :Form
	{
		Button m_btn_ok;

		// Construct from a dialog resource.
		// Note, not all of the controls are required in the class. The ones that
		// are get bound to the controls in the created dialog using the id
		About()
			:Form(Params<>().dlg().name("about").id(IDD_ABOUTBOX).start_pos(EStartPosition::CentreParent))
			,m_btn_ok(Button::Params<>().parent(this_).name("btn-ok").id(IDOK).anchor(EAnchor::BottomRight))
		{
			m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
		}
	};

	// About from auto generated dialog template
	struct About2 :Form
	{
		enum { ID_LBL_VERSION = 100, ID_LBL_COPYRIGHT, ID_IMG_ICON };

		ImageBox m_img_icon;
		Label m_lbl_version;
		Label m_lbl_copyright;
		Button m_btn_ok;

		About2()
			:Form(Params<>().dlg().name("about2").title(L"About2 - TestWinGUI").wh(163,62).dlu().start_pos(EStartPosition::CentreParent).wndclass(RegisterWndClass<About2>()))
			,m_img_icon     (ImageBox::Params<>().parent(this_).name("img-icon"     ).image(L"refresh", Image::EType::Png).id(ID_IMG_ICON).xy(14,14).wh(20,20).dlu())
			,m_lbl_version  (Label   ::Params<>().parent(this_).name("lbl-version"  ).text(L"TestWinGUI, Version 1.0").id(ID_LBL_VERSION  ).xy(42,14).dlu().border())
			,m_lbl_copyright(Label   ::Params<>().parent(this_).name("lbl-copyright").text(L"Copyright (C) 2014"     ).id(ID_LBL_COPYRIGHT).xy(42,26).dlu())
			,m_btn_ok       (Button  ::Params<>().parent(this_).name("btn-ok"       ).text(L"OK"                     ).id(IDOK            ).xy(106,41).wh(50,14).dlu().def_btn().anchor(EAnchor::BottomRight))
		{
			//"\projects\TestWingui\res\Smiling gekko 140x110.ico"
			m_btn_ok.Click += [&](Button&, EmptyArgs const&)
			{
				Close(EDialogResult::Ok);
			};
		}
	};
}