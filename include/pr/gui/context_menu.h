//*****************************************************************************
// Context Menu
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
// Usage:
#pragma once
#ifndef PR_GUI_CONTEXT_MENU_H
#define PR_GUI_CONTEXT_MENU_H

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <memory>
#include <string>
#include <vector>
#include <algorithm>
#include <atlbase.h>
#include <atlapp.h>
#include <atluser.h>
#include <atlmisc.h>
#include <atldlgs.h>
#include <atlcrack.h>
#include <gdiplus.h>

namespace pr
{
	namespace gui
	{
		// Forward
		class ContextMenu;
		struct ContextMenuStyle;
		struct IContextMenuItem;

		// Helper functions
		inline Gdiplus::Rect  ToRect (RECT const& rect)            { return Gdiplus::Rect(rect.left, rect.top, rect.right-rect.left, rect.bottom-rect.top); }
		inline Gdiplus::Rect  ToRect (Gdiplus::RectF const& rect)  { return Gdiplus::Rect(int(rect.X), int(rect.Y), int(rect.Width), int(rect.Height)); }
		inline RECT           ToRECT (Gdiplus::Rect const& rect)   { RECT r = {rect.GetLeft(), rect.GetTop(), rect.GetRight(), rect.GetBottom()}; return r; }
		inline Gdiplus::Color ToColor(COLORREF col)                { Gdiplus::Color c; c.SetFromCOLORREF(col); return c; }
		inline Gdiplus::Color ColAdj (Gdiplus::Color col, int dr, int dg, int db)
		{
			int r = col.GetR() + dr; if (r>255) r=255; else if (r<0) r=0;
			int g = col.GetG() + dg; if (g>255) g=255; else if (g<0) g=0;
			int b = col.GetB() + db; if (b>255) b=255; else if (b<0) b=0;
			return Gdiplus::Color(col.GetA(), BYTE(r), BYTE(g), BYTE(b));
		}
		inline NONCLIENTMETRICSW GetNonClientMetrics()
		{
			NONCLIENTMETRICSW metrics = { sizeof(NONCLIENTMETRICSW) };
			::SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(metrics), &metrics, 0);
			return metrics;
		}

		// Menu item base class
		struct IContextMenuItem
		{
			virtual ~IContextMenuItem() {}
			virtual int  GetId() const { return 0; }
			virtual void AddToMenu(HMENU menu, int index) = 0;
			virtual void MeasureItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPMEASUREITEMSTRUCT mi) = 0;
			virtual void DrawItem   (ContextMenu* menu, Gdiplus::Graphics& gfx, LPDRAWITEMSTRUCT di) = 0;
			virtual void Selected   (ContextMenu* menu, RECT const& rect, MENUITEMINFO const& mii) = 0;
		};

		// Reference counted pointers
		typedef std::shared_ptr<IContextMenuItem> ItemPtr;
		typedef std::shared_ptr<ContextMenuStyle> StylePtr;
		typedef std::shared_ptr<Gdiplus::Bitmap>  BitmapPtr;
		typedef std::shared_ptr<Gdiplus::Font>    FontPtr;

		// Style object used to give menu items individual styles
		struct ContextMenuStyle
		{
			NONCLIENTMETRICSW m_metrics;    // system metrics
			FontPtr        m_font_text;     // The font to display the text in
			FontPtr        m_font_marks;    // The font containing glyphs
			Gdiplus::Color m_col_bkgd;      // The backgroud colour for a menu item
			Gdiplus::Color m_col_select;    // The selection colour for a menu item
			Gdiplus::Color m_col_text;      // The colour to draw the text with
			Gdiplus::Color m_col_text_da;   // The colour to draw the text with when disabled
			Gdiplus::Color m_col_3dhi;      // The 3D effect highlighted colour
			Gdiplus::Color m_col_3dlo;      // The 3D effect shadow colour
			int            m_margin_left;   // The space to allow for bitmaps, check marks, etc

			ContextMenuStyle()
			:m_metrics     (GetNonClientMetrics())
			,m_font_text   (new Gdiplus::Font(::GetDC(0), &m_metrics.lfMenuFont))
			,m_font_marks  (new Gdiplus::Font(L"Marlett", m_font_text->GetSize()))
			,m_col_bkgd    (ToColor(::GetSysColor(COLOR_MENU)))
			,m_col_select  (ToColor(::GetSysColor(COLOR_MENUHILIGHT)))//ColAdj(m_col_bkgd, -50, -50, 50))//
			,m_col_text    (ToColor(::GetSysColor(COLOR_MENUTEXT)))
			,m_col_text_da (ToColor(::GetSysColor(COLOR_GRAYTEXT)))
			,m_margin_left (20)
			{}
		};

		// Helper methods for drawing context menu items
		struct ContextMenuItemDraw
		{
			static Gdiplus::Rect MeasureBitmap(BitmapPtr const& bm, int width)
			{
				// Measure the size of a bitmap rescaled to fit widthwise within 'width'
				if (!bm) return Gdiplus::Rect(0,0,0,0);
				Gdiplus::Rect rect(0, 0, bm->GetWidth(), bm->GetHeight());
				rect.Height = rect.Height * width / rect.Width;
				rect.Width  = width;
				return rect;
			}
			static void Bkgd(Gdiplus::Graphics& gfx, Gdiplus::Rect const& rect, bool selected, StylePtr const& style)
			{
				Gdiplus::Color col_bkgd = selected ? style->m_col_select : style->m_col_bkgd;
				Gdiplus::Pen   pen_3dhi(ColAdj(col_bkgd, 10, 10, 10));
				Gdiplus::Pen   pen_3dlo(ColAdj(col_bkgd,-10,-10,-10));
				Gdiplus::SolidBrush bsh_bkgd(col_bkgd);

				gfx.FillRectangle(&bsh_bkgd, rect);
				int x = rect.X + style->m_margin_left;
				gfx.DrawLine(&pen_3dlo, x+0, rect.GetTop(), x+0, rect.GetBottom());
				gfx.DrawLine(&pen_3dhi, x+1, rect.GetTop(), x+1, rect.GetBottom());
			}
			static void Bitmap(Gdiplus::Graphics& gfx, Gdiplus::Rect const& rect, BitmapPtr const& bm, StylePtr const& style)
			{
				if (bm == 0) return;
				Gdiplus::Rect bm_sz = MeasureBitmap(bm, style->m_margin_left - 2);
				Gdiplus::Rect r(rect.X + 1, rect.Y + 1, bm_sz.Width, bm_sz.Height);
				gfx.DrawImage(bm.get(), r);
			}
			static void Check(Gdiplus::Graphics& gfx, Gdiplus::Rect const& rect, int check_state, bool disabled, StylePtr const& style)
			{
				if (check_state == 0) return;
				Gdiplus::SolidBrush bsh_text(disabled ? style->m_col_text_da : style->m_col_text);
				wchar_t const* tick = (check_state == 1) ? L"a" : L"h"; int const tick_len = 1;
				Gdiplus::RectF sz; gfx.MeasureString(tick, tick_len, style->m_font_marks.get(), Gdiplus::PointF(), &sz);
				Gdiplus::PointF pt(float(rect.X + (style->m_margin_left - sz.Width)*0.5f), float(rect.Y + (rect.Height - sz.Height)*0.5f));
				gfx.DrawString(tick, tick_len, style->m_font_marks.get(), pt, &bsh_text);
			}
		};

		// Helper dialog for hosting controls within the context menu
		template <typename Ctrl> class ControlHost :public WTL::CIndirectDialogImpl<Ctrl>
		{
		public:
			BEGIN_DIALOG_EX(0,0,0,0,0)
				DIALOG_STYLE(DS_CENTER|DS_SHELLFONT|WS_VISIBLE|WS_GROUP|WS_POPUP)
				DIALOG_FONT(8, _T("MS Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
			END_CONTROLS_MAP()
		};

		// A context menu
		class ContextMenu
			:public ControlHost<ContextMenu>
			,public IContextMenuItem
		{
		public:
			class Separator :public IContextMenuItem
			{
			public:
				void AddToMenu(HMENU menu, int index)
				{
					IContextMenuItem* cmi = static_cast<IContextMenuItem*>(this);
					::AppendMenuW(menu, MF_SEPARATOR|MF_OWNERDRAW, index, LPCWSTR(cmi));
				}
				void MeasureItem(ContextMenu*, Gdiplus::Graphics&, LPMEASUREITEMSTRUCT mi)
				{
					mi->itemWidth = 1;
					mi->itemHeight = GetSystemMetrics(SM_CYMENU)/2;
				}
				void DrawItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPDRAWITEMSTRUCT di)
				{
					StylePtr const& style = menu->m_def_style;

					ContextMenuItemDraw::Bkgd(gfx, ToRect(di->rcItem), false, style);
					Gdiplus::Rect rect = ToRect(di->rcItem);

					Gdiplus::Pen pen_3dhi(ColAdj(style->m_col_bkgd, 10, 10, 10));
					Gdiplus::Pen pen_3dlo(ColAdj(style->m_col_bkgd,-10,-10,-10));
					int x0 = rect.X + style->m_margin_left + 1;
					int x1 = rect.X + rect.Width;
					int y = rect.Y + rect.Height / 2;
					gfx.DrawLine(&pen_3dlo, x0, y+0, x1, y+0);
					gfx.DrawLine(&pen_3dhi, x0, y+1, x1, y+1);
				}
				void Selected(ContextMenu*, RECT const&, MENUITEMINFO const&) {}
			};
			class Label :public IContextMenuItem
			{
			protected:
				enum { BmpMargin = 1, TextMargin = 2 };
				Gdiplus::Rect m_rect_text;    // The dimensions of the text

			public:
				std::wstring m_text;          // The label text
				UINT_PTR     m_id;            // An id for this item
				int          m_check_state;   // 0-Unchecked, 1-checked, 2-unknown
				StylePtr     m_style;         // The style to use to draw the menu item (null means use the default for the menu)
				BitmapPtr    m_bitmap;        // A bitmap to draw next to the item (null means no bitmap)

				Label(wchar_t const* text = L"Menu item", UINT_PTR id = 0, int check_state = 0, StylePtr style = StylePtr(), BitmapPtr bm = BitmapPtr())
				:m_text(text)
				,m_id(id)
				,m_check_state(check_state)
				,m_style(style)
				,m_bitmap(bm)
				{}
				int  GetId() const
				{
					return (int)m_id;
				}
				void AddToMenu(HMENU menu, int index)
				{
					IContextMenuItem* cmi = static_cast<IContextMenuItem*>(this);
					::AppendMenuW(menu, MF_STRING|MF_OWNERDRAW, index, LPCWSTR(cmi));
				}
				void MeasureItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPMEASUREITEMSTRUCT mi)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;

					// Measure the text size
					Gdiplus::StringFormat fmt; fmt.SetHotkeyPrefix(Gdiplus::HotkeyPrefixShow);
					Gdiplus::RectF text_sz; gfx.MeasureString(m_text.c_str(), int(m_text.size()), style->m_font_text.get(), Gdiplus::PointF(), &fmt, &text_sz);
					m_rect_text = ToRect(text_sz);

					// Measure the bitmap size
					auto bm_sz = ContextMenuItemDraw::MeasureBitmap(m_bitmap, style->m_margin_left - 2*BmpMargin);

					// Set item dimensions
					auto sz = m_rect_text; sz.Inflate(TextMargin, TextMargin);
					mi->itemWidth  = style->m_margin_left + sz.Width;
					mi->itemHeight = std::max(sz.Height, std::max(bm_sz.Height, GetSystemMetrics(SM_CYMENU)));
				}
				void DrawItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPDRAWITEMSTRUCT di)
				{
					auto style = m_style ? m_style : menu->m_def_style;
					bool selected = (di->itemState & ODS_SELECTED) != 0;
					bool disabled = (di->itemState & ODS_DISABLED) != 0;
					auto rect = ToRect(di->rcItem);

					// Draw background and left margin items
					ContextMenuItemDraw::Bkgd  (gfx, rect, selected, style);
					ContextMenuItemDraw::Bitmap(gfx, rect, m_bitmap, style);
					ContextMenuItemDraw::Check (gfx, rect, m_check_state, disabled, style);

					// Draw the label text
					Gdiplus::SolidBrush bsh_text(disabled ? style->m_col_text_da : style->m_col_text);
					Gdiplus::StringFormat fmt; fmt.SetHotkeyPrefix(Gdiplus::HotkeyPrefixShow);
					Gdiplus::PointF pt(float(rect.X + style->m_margin_left + TextMargin), float(rect.Y + (rect.Height - m_rect_text.Height)*0.5f));
					gfx.DrawString(m_text.c_str(), int(m_text.size()), style->m_font_text.get(), pt, &fmt, &bsh_text);
				}
				void Selected(ContextMenu*, RECT const&, MENUITEMINFO const&) {}
			};
			class Edit :public ControlHost<Edit> ,public Label
			{
			protected:
				enum { InnerMargin = 2, OuterMargin = 2 };
				CEdit         m_edit;         // The hosted edit control
				Gdiplus::Rect m_rect_value;   // The dimensions of the value text
				CFont         m_font;         // The font to use in the edit control

			public:
				std::wstring   m_value;       // The value to display in the edit box
				FontPtr        m_value_font;  // The font to use for the value, if null the label font is used
				int            m_min_width;   // The minimum width of the edit box

				Edit(wchar_t const* text = L"Menu item", wchar_t const* value = L"", UINT_PTR id = 0, int check_state = 0, StylePtr style = StylePtr(), BitmapPtr bm = BitmapPtr())
				:Label(text, id, check_state, style, bm)
				,m_edit()
				,m_rect_value()
				,m_font()
				,m_value(value)
				,m_value_font()
				,m_min_width(60)
				{}
				Gdiplus::Rect CtrlRect(Gdiplus::Rect const& rect, int margin) const
				{
					return Gdiplus::Rect(
						rect.X + margin + TextMargin + m_rect_text.Width + TextMargin + OuterMargin,
						rect.Y + OuterMargin,
						m_rect_value.Width + 2*InnerMargin,
						m_rect_value.Height + 2*InnerMargin);
				}
				void MeasureItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPMEASUREITEMSTRUCT mi)
				{
					auto style = m_style ? m_style : menu->m_def_style;

					// Measure the label portion of the item
					Label::MeasureItem(menu, gfx, mi);

					// Measure the edit box portion
					Gdiplus::RectF text_sz; gfx.MeasureString(m_value.c_str(), int(m_value.size()), style->m_font_text.get(), Gdiplus::PointF(), 0, &text_sz);
					m_rect_value = ToRect(text_sz);
					m_rect_value.Width = std::max(m_rect_value.Width, m_min_width);

					// Expand the item dimensions
					auto r = m_rect_value; r.Inflate(InnerMargin+OuterMargin, InnerMargin+OuterMargin);
					mi->itemWidth += r.Width;
					mi->itemHeight = std::max(int(mi->itemHeight), r.Height);
				}
				void DrawItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPDRAWITEMSTRUCT di)
				{
					auto style = m_style ? m_style : menu->m_def_style;
					bool disabled = (di->itemState & ODS_DISABLED) != 0;
					auto rect = ToRect(di->rcItem);

					// Draw the label
					Label::DrawItem(menu, gfx, di);

					// Draw edit box
					auto r = CtrlRect(rect, style->m_margin_left);
					Gdiplus::SolidBrush bsh_bkgd(disabled ? Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::LightGray) : Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::WhiteSmoke));
					Gdiplus::Pen        pen_brdr(Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::Black), 0);
					gfx.FillRectangle(&bsh_bkgd, r);
					gfx.DrawRectangle(&pen_brdr, r);

					// Write the edit box text
					Gdiplus::SolidBrush bsh_edittxt(style->m_col_text);
					Gdiplus::PointF pt(float(r.X + InnerMargin), float(r.Y + InnerMargin));
					gfx.DrawString(m_value.c_str(), int(m_value.size()), style->m_font_text.get(), pt, 0, &bsh_edittxt);
				}
				void Selected(ContextMenu* menu, RECT const& rect, MENUITEMINFO const&)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;

					// Set the font to use in the edit control
					Gdiplus::Graphics gfx(menu->m_hWnd); LOGFONTW logfont;
					FontPtr font = m_value_font ? m_value_font : style->m_font_text;
					if (font && font->GetLogFontW(&gfx, &logfont) == Gdiplus::Ok) m_font = ::CreateFontIndirectW(&logfont);

					// Display the control
					// 'rect' should be the position of the control in screen space
					CRect r = ToRECT(CtrlRect(ToRect(rect), style->m_margin_left));
					DoModal(menu->m_hWnd, (LPARAM)&r);
				}

				BEGIN_MSG_MAP(Edit)
					MSG_WM_INITDIALOG(OnInitDialog)
					MSG_WM_COMMAND(OnCommand)
				END_MSG_MAP()
				BOOL OnInitDialog(CWindow, LPARAM lparam)
				{
					// 'rect' should be the position of the control in screen space
					CRect const& rect = *(CRect const*)lparam;

					CRect winrect = rect;
					AdjustWindowRect(&winrect, (DWORD)GetWindowLongPtr(GWL_STYLE), FALSE);
					SetWindowPos(0, &winrect, SWP_NOZORDER);

					CRect r; GetClientRect(&r);
					m_edit.Create(m_hWnd, &r, 0, WS_CHILD|WS_VISIBLE|WS_BORDER|ES_AUTOHSCROLL, 0);
					if (!m_font.IsNull()) m_edit.SetFont(m_font);
					::SetWindowTextW(m_edit, m_value.c_str());

					return TRUE;
				}
				void OnCommand(UINT, int nID, CWindow)
				{
					switch (nID)
					{
					default: break;
					case IDCANCEL: EndDialog(0); break;
					case IDOK: // Copy the text from the control
						m_value.resize(::GetWindowTextLengthW(m_edit) + 1);
						if (!m_value.empty()) ::GetWindowTextW(m_edit, &m_value[0], int(m_value.size()));
						while (!m_value.empty() && *(--m_value.end()) == 0) m_value.resize(m_value.size() - 1);
						EndDialog(0);
						break;
					}
				}
			};
			class Combo :public ControlHost<Combo> ,public Label
			{
			protected:
				enum { InnerMargin = 2, OuterMargin = 2, ArwBtnWidth = 20 };
				CComboBox     m_combo;         // The hosted combo control
				Gdiplus::Rect m_rect_value;    // The dimensions of the largest value
				CFont         m_font;          // The font used in the combo control

			public:
				typedef std::vector<std::wstring> ValueCont;
				ValueCont m_values;            // The items to display in the combo box
				int       m_index;             // Index of the selected item
				FontPtr   m_value_font;        // The font to use for text in the control
				int       m_min_width;         // Minimum width of the text area in the combo box

				Combo(wchar_t const* text = L"Menu item", ValueCont const* values = 0, UINT_PTR id = 0, int check_state = 0, StylePtr style = StylePtr(), BitmapPtr bm = BitmapPtr())
				:Label(text, id, check_state, style, bm)
				,m_combo()
				,m_rect_value()
				,m_font()
				,m_values(values ? *values : ValueCont())
				,m_index()
				,m_value_font()
				,m_min_width(60)
				{}
				Gdiplus::Rect CtrlRect(Gdiplus::Rect const& rect, int margin) const
				{
					return Gdiplus::Rect(
						rect.X + margin + TextMargin + m_rect_text.Width + TextMargin + OuterMargin,
						rect.Y + OuterMargin,
						m_rect_value.Width + 2*InnerMargin + ArwBtnWidth,
						m_rect_value.Height + 2*InnerMargin);
				}
				CWindow ContextMenuWnd() const
				{
					return ::FindWindowW(L"#32768", 0);
				}
				void MeasureItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPMEASUREITEMSTRUCT mi)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;

					// Measure the label portion of the item
					Label::MeasureItem(menu, gfx, mi);

					// Measure the combo box portion
					Gdiplus::RectF text_sz(0,0,0,0);
					for (auto value : m_values)
					{
						Gdiplus::RectF sz; gfx.MeasureString(value.c_str(), int(value.size()), style->m_font_text.get(), Gdiplus::PointF(), 0, &sz);
						Gdiplus::RectF::Union(text_sz, text_sz, sz);
					}
					m_rect_value = ToRect(text_sz);
					m_rect_value.Width = std::max(m_rect_value.Width, m_min_width);

					// Expand the item dimensions
					auto r = m_rect_value;
					r.Inflate(InnerMargin, InnerMargin);
					r.Width += ArwBtnWidth;
					r.Inflate(OuterMargin, OuterMargin);
					mi->itemWidth += r.Width;
					mi->itemHeight = std::max(int(mi->itemHeight), r.Height);
				}
				void DrawItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPDRAWITEMSTRUCT di)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;
					bool disabled = (di->itemState & ODS_DISABLED) != 0;
					auto rect = ToRect(di->rcItem);
					auto rect_ctrl = CtrlRect(rect, style->m_margin_left);
					auto rect_btn  = rect_ctrl; rect_btn.X = rect_btn.GetRight() - ArwBtnWidth; rect_btn.Width = ArwBtnWidth;

					Gdiplus::SolidBrush bsh_edittxt(style->m_col_text);
					Gdiplus::SolidBrush bsh_bkgd(disabled ? Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::LightGray) : Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::WhiteSmoke));
					Gdiplus::SolidBrush bsh_bkgdarw(Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::LightGray));
					Gdiplus::Pen        pen_brdr(Gdiplus::Color((Gdiplus::ARGB)Gdiplus::Color::Black), 0);

					// Draw the label
					Label::DrawItem(menu, gfx, di);

					// Draw the combo box
					wchar_t const* arw = L"6";
					gfx.FillRectangle(&bsh_bkgd, rect_ctrl);
					gfx.DrawRectangle(&pen_brdr, rect_ctrl);
					gfx.FillRectangle(&bsh_bkgdarw, rect_btn);
					gfx.DrawRectangle(&pen_brdr, rect_btn);
					Gdiplus::RectF rect_arw; gfx.MeasureString(arw, 1, style->m_font_marks.get(), Gdiplus::PointF(), &rect_arw);
					Gdiplus::PointF pt(rect_btn.X + (rect_btn.Width - rect_arw.Width) * 0.5f, rect_btn.Y + (rect_btn.Height - rect_arw.Height) * 0.5f);
					gfx.DrawString(arw, 1, style->m_font_marks.get(), pt, &bsh_edittxt);

					// Draw the selected value text
					if (m_index < int(m_values.size()))
					{
						Gdiplus::PointF pt(float(rect_ctrl.X + InnerMargin), float(rect_ctrl.Y + InnerMargin));
						gfx.DrawString(m_values[m_index].c_str(), int(m_values[m_index].size()), style->m_font_text.get(), pt, 0, &bsh_edittxt);
					}
				}
				void Selected(ContextMenu* menu, RECT const& rect, MENUITEMINFO const&)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;

					// Set the font to use in the control
					Gdiplus::Graphics gfx(menu->m_hWnd); LOGFONTW logfont;
					FontPtr font = m_value_font ? m_value_font : style->m_font_text;
					if (font && font->GetLogFontW(&gfx, &logfont) == Gdiplus::Ok) m_font = ::CreateFontIndirectW(&logfont);

					// Display the control
					CRect r = ToRECT(CtrlRect(ToRect(rect), style->m_margin_left));
					DoModal(menu->m_hWnd, (LPARAM)&r);
				}

				BEGIN_MSG_MAP(Combo)
					MSG_WM_INITDIALOG(OnInitDialog)
					MSG_WM_COMMAND(OnCommand)
				END_MSG_MAP()
				BOOL OnInitDialog(CWindow, LPARAM lparam)
				{
					// 'rect' should be the position of the control in screen space
					CRect const& rect = *(CRect const*)lparam;

					CRect winrect = rect;
					AdjustWindowRect(&winrect, (DWORD)GetWindowLongPtr(GWL_STYLE), FALSE);
					SetWindowPos(0, &winrect, SWP_NOZORDER);

					CRect r; GetClientRect(&r);
					m_combo = ::CreateWindowExW(0, WC_COMBOBOXW, 0, WS_CHILD|WS_VISIBLE|WS_BORDER|CBS_DROPDOWNLIST|CBS_AUTOHSCROLL|CBS_HASSTRINGS, r.left, r.top, r.Width(), r.Height(), m_hWnd, 0, _AtlBaseModule.GetModuleInstance(), 0);
					if (!m_font.IsNull()) m_combo.SetFont(m_font);
					for (ValueCont::const_iterator i = m_values.begin(), iend = m_values.end(); i != iend; ++i)
						m_combo.AddString((LPCTSTR)i->c_str());

					m_combo.SetCurSel(m_index);
					return TRUE;
				}
				void OnCommand(UINT, int nID, CWindow)
				{
					switch (nID)
					{
					default: break;
					case IDCANCEL: EndDialog(0); break;
					case IDOK:
						m_index = m_combo.GetCurSel();
						EndDialog(0);
						break;
					}
				}
			};

		protected:
			struct Selection
			{
				CMenuHandle m_menu;
				UINT        m_flags;
				UINT        m_id;
				CRect       m_rect;
				Selection() :m_menu() ,m_flags(0) ,m_id(0) ,m_rect() {}
			};

		protected:
			typedef std::vector<ItemPtr> ItemCont;
			ItemCont  m_items;    // The items in this menu
			CMenu     m_root;     // The handle to the root menu
			int       m_result;   // The id of the item selected
			Selection m_sel;      // The menu item that was selected

		public:
			StylePtr m_def_style; // A default style for when 'm_style' pointers are null
			Label    m_label;     // The text for this menu (used when added as a submenu)

			ContextMenu(wchar_t const* text = L"Menu", UINT_PTR id = 0, int check_state = 0, StylePtr style = StylePtr())
				:m_items()
				,m_root(::CreatePopupMenu())
				,m_def_style(new ContextMenuStyle())
				,m_label(text, id, check_state, style)
				,m_sel()
			{}
			~ContextMenu()
			{
				if (IsWindow()) DestroyWindow();
			}

			// Add a context menu item to this menu
			void AddItem(ItemPtr item)
			{
				m_items.push_back(item);
			}

			// Add a context menu item to this menu, 'item' *MUST* be allocated using 'new'
			template <typename Item> Item& AddItem(Item* item)
			{
				AddItem(ItemPtr(item));
				return *item;
			}

			// Show the context menu. Blocks until the menu is closed
			// Returns the id of the item selected
			int Show(HWND parent, int x, int y)
			{
				CRect rect(x, y, x, y);
				DoModal(parent, (LPARAM)&rect);
				return m_result;
			}

			BEGIN_MSG_MAP(ContextMenu)
				MSG_WM_INITDIALOG(OnInitDialog)
				MSG_WM_CAPTURECHANGED(OnCaptureChanged)
				MSG_WM_MENUSELECT(OnMenuSelect)
				MSG_WM_MENUCHAR(OnMenuChar)
				MSG_WM_MEASUREITEM(OnMeasureItem)
				MSG_WM_DRAWITEM(OnDrawItem)
			END_MSG_MAP()

		protected:
			int ApplySelection()
			{
				if (m_sel.m_menu.IsNull() || (m_sel.m_flags & (MF_POPUP|MF_DISABLED|MF_GRAYED)) != 0)
					return 0;
				else
				{
					CMenuItemInfo mii; mii.fMask = MIIM_DATA;
					m_sel.m_menu.GetMenuItemInfo(m_sel.m_id, TRUE, &mii);
					IContextMenuItem* item = static_cast<IContextMenuItem*>((void*)(mii.dwItemData));
					item->Selected(this, m_sel.m_rect, mii);
					return item->GetId();
				}
			}
			BOOL OnInitDialog(CWindow, LPARAM lInitParam)
			{
				// Link up the menu items
				// Note: item ID's for the menu items are indices.
				int index = 0;
				for (ItemCont::iterator i = m_items.begin(), iend = m_items.end(); i != iend; ++i)
					(*i)->AddToMenu(m_root, index++);

				// This window is the parent because it handles the measure/draw item messages
				m_result = 0;
				CRect const& rect = *(CRect const*)lInitParam;
				DWORD align = GetSystemMetrics(SM_MENUDROPALIGNMENT);
				m_root.TrackPopupMenu(align|TPM_LEFTBUTTON, rect.left, rect.top, m_hWnd);
				EndDialog(0);
				return FALSE;
			}
			void OnCaptureChanged(CWindow)
			{
				// Capture changed occurs just as the menu is closing
				// but, crucially, while the window is still visible
				// :-/ doesn't work for sub menus...
				m_result = ApplySelection();
			}
			void OnMenuSelect(UINT nItemID, UINT nFlags, CMenuHandle menu)
			{
				//OutputDebugStringA(pr::FmtS("OnMenuSelect: id: %d, flags: %x, handle: %x\n", nItemID, nFlags, menu.m_hMenu));
				if (nFlags == 0x0000FFFF) return; // Menu closing notification
				m_sel.m_menu  = menu;
				m_sel.m_flags = nFlags;
				m_sel.m_id    = nItemID;
				menu.GetMenuItemRect(m_hWnd, nItemID, &m_sel.m_rect);
			}
			LRESULT OnMenuChar(UINT nChar, UINT nFlags, CMenuHandle menu)
			{
				OutputDebugStringA("OnMenuChar\n");
				(void)nChar;
				(void)nFlags;
				(void)menu;
				return S_OK;
			}
			void OnMeasureItem(int, LPMEASUREITEMSTRUCT mi)
			{
				// If you get a crash in here, check that a "static_cast<IContextMenuItem*>(this)"
				// pointer was used in AppendMenuW()
				if (mi->CtlType != ODT_MENU) return;
				Gdiplus::Graphics gfx(m_hWnd);
				static_cast<IContextMenuItem*>((void*)(mi->itemData))->MeasureItem(this, gfx, mi);
			}
			void OnDrawItem(int, LPDRAWITEMSTRUCT di)
			{
				// If you get a crash in here, check that a "static_cast<IContextMenuItem*>(this)"
				// pointer was used in AppendMenuW()
				if (di->CtlType != ODT_MENU) return;
				Gdiplus::Graphics gfx(di->hDC);
				static_cast<IContextMenuItem*>((void*)(di->itemData))->DrawItem(this, gfx, di);
			}

			// IContextMenuItem interface implementation
			int  GetId() const { return m_label.GetId(); }
			void AddToMenu(HMENU menu, int)
			{
				// Link up the submenu items
				int index = 0;
				for (ItemCont::iterator i = m_items.begin(), iend = m_items.end(); i != iend; ++i)
					(*i)->AddToMenu(m_root, index++);

				IContextMenuItem* cmi = static_cast<IContextMenuItem*>(this);
				::AppendMenuW(menu, MF_POPUP|MF_OWNERDRAW, UINT_PTR(m_root.m_hMenu), LPCWSTR(cmi));
			}
			void MeasureItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPMEASUREITEMSTRUCT mi)
			{
				using namespace Gdiplus;
				m_label.MeasureItem(menu, gfx, mi);
			}
			void DrawItem(ContextMenu* menu, Gdiplus::Graphics& gfx, LPDRAWITEMSTRUCT di)
			{
				using namespace Gdiplus;
				m_label.DrawItem(menu, gfx, di);
			}
			void Selected(ContextMenu*, RECT const&, MENUITEMINFO const&) {}
		};
	}
}

#endif
