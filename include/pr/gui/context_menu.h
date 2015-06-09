//*****************************************************************************
// Context Menu
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
// Usage:
#pragma once

#include <memory>
#include <string>
#include <vector>
#include <algorithm>
#include <gdiplus.h>

#include "pr/gui/wingui.h"
#include "pr/gui/gdiplus.h"

namespace pr
{
	namespace gui
	{
		// Forward
		struct ContextMenu;
		struct ContextMenuStyle;
		struct IContextMenuItem;
		using GdiGraphics   = ::Gdiplus::Graphics;
		using GdiPoint      = ::Gdiplus::Point;
		using GdiPointF     = ::Gdiplus::PointF;
		using GdiSize       = ::Gdiplus::Size;
		using GdiSizeF      = ::Gdiplus::SizeF;
		using GdiRect       = ::Gdiplus::Rect;
		using GdiRectF      = ::Gdiplus::RectF;
		using GdiColor      = ::Gdiplus::Color;
		using GdiARGB       = ::Gdiplus::ARGB;
		using GdiBitmap     = ::Gdiplus::Bitmap;
		using GdiFont       = ::Gdiplus::Font;
		using GdiPen        = ::Gdiplus::Pen;
		using GdiSolidBrush = ::Gdiplus::SolidBrush;
		using GdiStrFmt     = ::Gdiplus::StringFormat;
		using BitmapPtr     = std::shared_ptr<GdiBitmap>;
		using FontPtr       = std::shared_ptr<GdiFont>;

		#pragma region HelperFunctions
		namespace impl
		{
			inline GdiColor ColAdj(GdiColor col, int dr, int dg, int db)
			{
				int r = col.GetR() + dr; if (r>255) r=255; else if (r<0) r=0;
				int g = col.GetG() + dg; if (g>255) g=255; else if (g<0) g=0;
				int b = col.GetB() + db; if (b>255) b=255; else if (b<0) b=0;
				return GdiColor(col.GetA(), BYTE(r), BYTE(g), BYTE(b));
			}
			inline GdiRect Inflate(GdiRect rect, int dx, int dy)
			{
				rect.X -= dx;
				rect.Y -= dy;
				rect.Width += dx*2;
				rect.Height += dy*2;
				return rect;
			}
		}
		#pragma endregion

		#pragma region Style
		// Style object used to give menu items individual styles
		struct ContextMenuStyle
		{
			struct ColPair
			{
				GdiColor m_text; // The text colour for an item
				GdiColor m_bkgd; // The backgroud colour for an item
				ColPair(GdiColor text, GdiColor bkgd)
					:m_text(text)
					,m_bkgd(bkgd)
				{}
			};

			NonClientMetrics  m_metrics;     // system metrics
			FontPtr           m_font_text;   // The font to display the text in
			FontPtr           m_font_marks;  // The font containing glyphs
			ColPair           m_col_norm;    // Colours for a normal state menu item
			ColPair           m_col_select;  // Colours for a selected menu item
			ColPair           m_col_disable; // Colours for a disabled menu item
			int               m_margin_left; // The space to allow for bitmaps, check marks, etc
			int               m_text_margin; // The margin surrounding text in the item
			int               m_bmp_margin;  // The margin surrounding the bitmap in the item


			ContextMenuStyle()
				:m_metrics     ()
				,m_font_text   (std::make_shared<GdiFont>(::GetDC(0), &m_metrics.lfMenuFont))
				,m_font_marks  (std::make_shared<GdiFont>(L"Marlett", m_font_text->GetSize()))
				,m_col_norm    (To<GdiColor>(::GetSysColor(COLOR_MENUTEXT))     , To<GdiColor>(::GetSysColor(COLOR_MENU)))
				,m_col_select  (To<GdiColor>(::GetSysColor(COLOR_HIGHLIGHTTEXT)), To<GdiColor>(::GetSysColor(COLOR_MENUHILIGHT)))
				,m_col_disable (To<GdiColor>(::GetSysColor(COLOR_GRAYTEXT))     , To<GdiColor>(::GetSysColor(COLOR_MENU)))
				,m_margin_left (20)
				,m_text_margin (2)
				,m_bmp_margin  (1)
			{}

			// Return the colour pair for the given item state
			ColPair Col(UINT item_state) const
			{
				bool selected = (item_state & ODS_SELECTED) != 0;
				bool disabled = (item_state & ODS_DISABLED) != 0;
				if (disabled) return m_col_disable;
				if (selected) return m_col_select;
				return m_col_norm;
			}
		};
		typedef std::shared_ptr<ContextMenuStyle> StylePtr;
		#pragma endregion

		// Menu item base class
		struct ContextMenuItem
		{
			int              m_id;          // Menu item id
			ContextMenuItem* m_parent;      // The containing menu if not null
			int              m_check_state; // 0-Unchecked, 1-checked, 2-unknown
			StylePtr         m_style;       // Item specific style if not null
			BitmapPtr        m_bitmap;      // A bitmap to draw next to the item (null means no bitmap)

			// 'parent' is the containing menu item that properties are inherited from
			// 'id' is the menu item id
			ContextMenuItem(int id = -1, ContextMenuItem* parent = nullptr, int check_state = 0, StylePtr style = nullptr, BitmapPtr bm = nullptr)
				:m_id(id)
				,m_parent(parent)
				,m_check_state(check_state)
				,m_style(style)
				,m_bitmap(bm)
			{}
			virtual ~ContextMenuItem()
			{}

			// The window acting as the parent for hosted controls
			virtual HWND CtrlHost() const
			{
				assert(m_parent != nullptr && "Item is not parented to a ContextMenu item"); // ContextMenu overrides this method
				return m_parent->CtrlHost();
			}

			// Return the HMENU associated with m_parent
			virtual HMENU Menu() const
			{
				assert(m_parent != nullptr && "Item is not parented to a ContextMenu item"); // ContextMenu overrides this method
				return m_parent->Menu();
			}

			// Return the style to use for a context menu item
			virtual ContextMenuStyle const& Style() const
			{
				if (m_style) return *m_style;
				assert(m_parent != nullptr && "Item is not parented to a ContextMenu item"); // ContextMenu overrides this method
				return m_parent->Style();
			}

			virtual void MeasureItem(ContextMenu& menu, GdiGraphics& gfx, MEASUREITEMSTRUCT* mi) = 0;
			virtual void DrawItem   (ContextMenu& menu, GdiGraphics& gfx, DRAWITEMSTRUCT* di) = 0;
			virtual void Selected   (ContextMenu& /*menu*/, Rect const& /*rect*/, MENUITEMINFO const& /*mii*/) {}
		};

	// TODO: do this as a modal dialog

		// Context menu
		// Note: this class requires GdiPlus, remember to instantiate an instance
		// of 'pr::GdiPlus' somewhere so that the gdi+ dll is loaded
		struct ContextMenu :ContextMenuItem
		{
			#pragma region Draw Helpers

			// Helper methods for drawing context menu items
			struct Draw
			{
				static GdiRect MeasureText(GdiGraphics const& gfx, std::wstring const& text, ContextMenuStyle const& style)
				{
					GdiStrFmt fmt; fmt.SetHotkeyPrefix(Gdiplus::HotkeyPrefixShow);
					GdiRectF text_sz; gfx.MeasureString(text.c_str(), int(text.size()), style.m_font_text.get(), GdiPointF(), &fmt, &text_sz);
					return To<GdiRect>(text_sz);
				}
				static GdiRect MeasureBitmap(BitmapPtr const& bm, int width)
				{
					// Measure the size of a bitmap rescaled to fit widthwise within 'width'
					if (!bm) return GdiRect();
					GdiRect rect(0, 0, bm->GetWidth(), bm->GetHeight());
					rect.Height = rect.Height * width / rect.Width;
					rect.Width = width;
					return rect;
				}
				static void Bkgd(GdiGraphics& gfx, GdiRect const& rect, UINT item_state, ContextMenuStyle const& style)
				{
					auto col = style.Col(item_state);
					GdiPen   pen_3dhi(impl::ColAdj(col.m_bkgd, 10, 10, 10));
					GdiPen   pen_3dlo(impl::ColAdj(col.m_bkgd,-10,-10,-10));
					GdiSolidBrush bsh_bkgd(col.m_bkgd);

					gfx.FillRectangle(&bsh_bkgd, rect);
					int x = rect.X + style.m_margin_left;
					gfx.DrawLine(&pen_3dlo, x+0, rect.GetTop(), x+0, rect.GetBottom() - 1);
					gfx.DrawLine(&pen_3dhi, x+1, rect.GetTop(), x+1, rect.GetBottom() - 1);
				}
				static void Bitmap(GdiGraphics& gfx, GdiRect const& rect, BitmapPtr const& bm, ContextMenuStyle const& style)
				{
					if (bm == 0) return;
					auto bm_sz = MeasureBitmap(bm, style.m_margin_left - 2);
					GdiRect r(rect.X + 1, rect.Y + 1, bm_sz.Width, bm_sz.Height);
					gfx.DrawImage(bm.get(), r);
				}
				static void Check(GdiGraphics& gfx, GdiRect const& rect, int check_state, UINT item_state, ContextMenuStyle const& style)
				{
					if (check_state == 0) return;
					auto col = style.Col(item_state);
					GdiSolidBrush bsh_text(col.m_text);
					auto tick = (check_state == 1) ? L"a" : L"h"; int const tick_len = 1;
					GdiRectF sz; gfx.MeasureString(tick, tick_len, style.m_font_marks.get(), GdiPointF(), &sz);
					GdiPointF pt(float(rect.X + (style.m_margin_left - sz.Width)*0.5f), float(rect.Y + (rect.Height - sz.Height)*0.5f));
					gfx.DrawString(tick, tick_len, style.m_font_marks.get(), pt, &bsh_text);
				}
				//static void Label(GdiGraphics& gfx, GdiRect const& rect, UINT item_state, int check_state, std::wstring const& text, BitmapPtr bitmap, ContextMenuStyle const& style)
				//{
				//	// Draw background and left margin items
				//	Bkgd  (gfx, rect, item_state, style);
				//	Bitmap(gfx, rect, bitmap, style);
				//	Check (gfx, rect, check_state, item_state, style);

				//	// Draw the label text
				//	auto col = style.Col(item_state);
				//	GdiSolidBrush bsh_text(col.m_text);
				//	GdiStrFmt fmt; fmt.SetHotkeyPrefix(Gdiplus::HotkeyPrefixShow);
				//	GdiPointF pt(float(rect.X + style.m_margin_left + style.m_text_margin), float(rect.Y + (rect.Height - m_rect_text.Height)*0.5f));
				//	gfx.DrawString(text.c_str(), int(text.size()), style.m_font_text.get(), pt, &fmt, &bsh_text);
				//}
			};

			#pragma endregion

			#pragma region Menu Items

			// Separator menu item
			struct Separator :ContextMenuItem
			{
				Separator(ContextMenu& menu)
					:ContextMenuItem(-1, &menu)
				{
					auto cmi = static_cast<ContextMenuItem*>(this);
					Throw(::AppendMenuW(Menu(), MF_SEPARATOR|MF_OWNERDRAW, m_id, LPCWSTR(cmi)), "Failed to append menu separater");
				}
				void MeasureItem(ContextMenu&, GdiGraphics&, MEASUREITEMSTRUCT* mi) override
				{
					mi->itemWidth = 1;
					mi->itemHeight = 3;
				}
				void DrawItem(ContextMenu&, GdiGraphics& gfx, DRAWITEMSTRUCT* di) override
				{
					auto& style = Style();
					auto rect = To<GdiRect>(di->rcItem);
					Draw::Bkgd(gfx, rect, di->itemState, style);

					// Draw the separator line
					GdiPen pen_3dhi(impl::ColAdj(style.m_col_norm.m_bkgd, +10, +10, +10));
					GdiPen pen_3dlo(impl::ColAdj(style.m_col_norm.m_bkgd, -10, -10, -10));
					auto x0 = rect.X + style.m_margin_left + 1;
					auto x1 = rect.X + rect.Width;
					auto y  = rect.Y + rect.Height / 2;
					if (x0 < x1)
					{
						gfx.DrawLine(&pen_3dlo, x0, y+0, x1, y+0);
						gfx.DrawLine(&pen_3dhi, x0, y+1, x1, y+1);
					}
				}
			};

			// Label menu item
			struct Label :ContextMenuItem
			{
				std::wstring m_text;      // The label text
				GdiRect      m_rect_text; // The dimensions of the text

				Label(ContextMenu& menu, TCHAR const* text = _T("<menu item>"), int id = 0, int check_state = 0, StylePtr style = nullptr, BitmapPtr bm = nullptr)
					:ContextMenuItem(id, &menu, check_state, style, bm)
					,m_text(Widen(text))
					,m_rect_text()
				{
					auto cmi = static_cast<ContextMenuItem*>(this);
					Throw(::AppendMenuW(Menu(), MF_OWNERDRAW, m_id, LPCWSTR(cmi)), "Failed to append menu item");
				}
				void MeasureItem(ContextMenu&, GdiGraphics& gfx, MEASUREITEMSTRUCT* mi) override
				{
					auto& style = Style();

					// Measure the text size
					auto tx_sz = Draw::MeasureText(gfx, m_text, style);

					// Measure the bitmap size
					auto bm_sz = Draw::MeasureBitmap(m_bitmap, style.m_margin_left - 2*style.m_bmp_margin);

					// Set item dimensions
					auto sz = impl::Inflate(tx_sz, style.m_text_margin, style.m_text_margin);
					mi->itemWidth  = style.m_margin_left + sz.Width;
					mi->itemHeight = std::max(sz.Height, std::max(bm_sz.Height, GetSystemMetrics(SM_CYMENU)));
					m_rect_text = tx_sz;
				}
				void DrawItem(ContextMenu&, GdiGraphics& gfx, DRAWITEMSTRUCT* di) override
				{
					auto& style = Style();
					auto rect = To<GdiRect>(di->rcItem);

					// Draw background and left margin items
					Draw::Bkgd  (gfx, rect, di->itemState, style);
					Draw::Bitmap(gfx, rect, m_bitmap, style);
					Draw::Check (gfx, rect, m_check_state, di->itemState, style);

					// Draw the label text
					auto col = style.Col(di->itemState);
					GdiSolidBrush bsh_text(col.m_text);
					GdiStrFmt fmt; fmt.SetHotkeyPrefix(Gdiplus::HotkeyPrefixShow);
					GdiPointF pt(float(rect.X + style.m_margin_left + style.m_text_margin), float(rect.Y + (rect.Height - m_rect_text.Height)*0.5f));
					gfx.DrawString(m_text.c_str(), int(m_text.size()), style.m_font_text.get(), pt, &fmt, &bsh_text);
				}

			private:
				// This constructor is used by ContextMenu to create submenus
				friend struct ContextMenu;
				Label(ContextMenu* parent = nullptr, TCHAR const* text = nullptr, StylePtr style = nullptr, BitmapPtr bm = nullptr)
					:ContextMenuItem(-1, parent, 0, style, bm)
					,m_text(Widen(text))
					,m_rect_text()
				{}
			};

			// Textbox menu item
			// Consists of a label followed by an edit box
			struct TextBox :Label
			{
				using TBox = pr::gui::TextBox;
				enum { InnerMargin = 2, OuterMargin = 2 };

				TBox         m_edit;       // The hosted control
				GdiRect      m_rect_value; // The dimensions of the value text
				std::wstring m_value;      // The value to display in the edit box
				FontPtr      m_value_font; // The font to use for the value, if null the label font is used
				int          m_min_width;  // The minimum width of the edit box

				TextBox(ContextMenu& menu, TCHAR const* text = _T("<menu item>"), TCHAR const* value = _T(""), int id = 0, int check_state = 0, StylePtr style = nullptr, BitmapPtr bm = nullptr)
					:Label(menu, text, id, check_state, style, bm)
					,m_edit(value, 0, 0, TBox::DefW, TBox::DefH, id, menu.m_win, nullptr, EAnchor::None)
					,m_rect_value()
					,m_value(Widen(value))
					,m_value_font()
					,m_min_width(60)
				{}
				void MeasureItem(ContextMenu& menu, GdiGraphics& gfx, MEASUREITEMSTRUCT* mi) override
				{
					auto& style = Style();

					// Measure the label portion of the item
					Label::MeasureItem(menu, gfx, mi);

					// Measure the edit box portion
					GdiRectF text_sz; gfx.MeasureString(m_value.c_str(), int(m_value.size()), style.m_font_text.get(), GdiPointF(), 0, &text_sz);
					m_rect_value = To<GdiRect>(text_sz);
					m_rect_value.Width = std::max(m_rect_value.Width, m_min_width);

					// Expand the item dimensions
					auto r = impl::Inflate(m_rect_value, InnerMargin+OuterMargin, InnerMargin+OuterMargin);
					mi->itemWidth += r.Width;
					mi->itemHeight = std::max(int(mi->itemHeight), r.Height);
				}
				void DrawItem(ContextMenu& menu, GdiGraphics& gfx, DRAWITEMSTRUCT* di) override
				{
					auto& style = Style();
					auto col = style.Col(di->itemState);
					auto rect = To<GdiRect>(di->rcItem);

					// Draw the label
					Label::DrawItem(menu, gfx, di);

					// Draw an edit box
					auto disabled = (di->itemState & ODS_DISABLED) != 0;
					auto r = CtrlRect(rect, style.m_margin_left);
					GdiSolidBrush bsh_bkgd(disabled ? GdiColor((GdiARGB)GdiColor::LightGray) : GdiColor((GdiARGB)GdiColor::WhiteSmoke));
					GdiPen        pen_brdr(GdiColor((GdiARGB)GdiColor::Black), 0);
					gfx.FillRectangle(&bsh_bkgd, r);
					gfx.DrawRectangle(&pen_brdr, r);

					// Write the edit box text
					GdiSolidBrush bsh_edittxt(style.m_col_norm.m_text);
					GdiPointF pt(float(r.X + InnerMargin), float(r.Y + InnerMargin));
					gfx.DrawString(m_value.c_str(), int(m_value.size()), style.m_font_text.get(), pt, 0, &bsh_edittxt);
				}
				void Selected(ContextMenu&, Rect const& rect, MENUITEMINFO const&) override
				{
					auto& style = Style();

					// Set the font to use in the edit control
					//GdiGraphics gfx(menu); LOGFONTW logfont;
					//auto& font = m_value_font ? m_value_font : style.m_font_text;
					//if (font && font->GetLogFontW(&gfx, &logfont) == Gdiplus::Ok)
					//	m_font = ::CreateFontIndirectW(&logfont);

					// Display the control
					// 'rect' should be the position of the control in screen space
					auto r = To<RECT>(CtrlRect(To<GdiRect>(rect), style.m_margin_left));
					m_edit.MoveWindow(r);
					//TextBox::Show( DoModal(menu->m_hWnd, (LPARAM)&r);
				}

				// Returns the bounds of the hosted control
				GdiRect CtrlRect(GdiRect const& rect, int margin) const
				{
					auto& style = Style();
					return GdiRect(
						rect.X + margin + style.m_text_margin + m_rect_text.Width + style.m_text_margin + OuterMargin,
						rect.Y + OuterMargin,
						m_rect_value.Width + 2*InnerMargin,
						m_rect_value.Height + 2*InnerMargin);
				}

				//BEGIN_MSG_MAP(Edit)
				//	MSG_WM_INITDIALOG(OnInitDialog)
				//	MSG_WM_COMMAND(OnCommand)
				//END_MSG_MAP()
				//BOOL OnInitDialog(CWindow, LPARAM lparam)
				//{
				//	// 'rect' should be the position of the control in screen space
				//	CRect const& rect = *(CRect const*)lparam;

				//	CRect winrect = rect;
				//	AdjustWindowRect(&winrect, (DWORD)GetWindowLongPtr(GWL_STYLE), FALSE);
				//	SetWindowPos(0, &winrect, SWP_NOZORDER);

				//	CRect r; GetClientRect(&r);
				//	m_edit.Create(m_hWnd, &r, 0, WS_CHILD|WS_VISIBLE|WS_BORDER|ES_AUTOHSCROLL, 0);
				//	if (!m_font.IsNull()) m_edit.SetFont(m_font);
				//	::SetWindowTextW(m_edit, m_value.c_str());

				//	return TRUE;
				//}
				//void OnCommand(UINT, int nID, CWindow)
				//{
				//	switch (nID)
				//	{
				//	default: break;
				//	case IDCANCEL: EndDialog(0); break;
				//	case IDOK: // Copy the text from the control
				//		m_value.resize(::GetWindowTextLengthW(m_edit) + 1);
				//		if (!m_value.empty()) ::GetWindowTextW(m_edit, &m_value[0], int(m_value.size()));
				//		while (!m_value.empty() && *(--m_value.end()) == 0) m_value.resize(m_value.size() - 1);
				//		EndDialog(0);
				//		break;
				//	}
				//}
			};

			#pragma endregion

		private:
			// This window is used to handle menu messages such as measure/draw item messages
			// It is a zero-sized non-modal window created when the menu is created.
			// It also serves as the parent for hosted controls
			struct MenuParentWindow :Form<MenuParentWindow>
			{
				// A selected menu item
				struct Selection
				{
					HMENU m_menu;
					UINT  m_flags;
					UINT  m_id;
					Rect  m_rect;
					Selection()
						:m_menu()
						,m_flags(0)
						,m_id(0)
						,m_rect()
					{}
				};

				ContextMenu* m_owner;
				Selection m_sel;      // The menu item that was selected

				MenuParentWindow(ContextMenu* owner)
					:Form<MenuParentWindow>(_T(""), nullptr, 0, 0, 0, 0, 0, 0, IDC_UNUSED, "ctx-menu")
					//:Form<MenuParentWindow>(DlgTemplate(nullptr, 0, 0, 0, 0, DS_CENTER|DS_SHELLFONT|WS_VISIBLE|WS_GROUP|WS_POPUP, 0))
					,m_owner(owner)
					,m_sel()
				{}

				// Message map function
				bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
				{
					DebugMessage(parent_hwnd, message, wparam, lparam);
					switch (message)
					{
					case WM_MENUSELECT:
						{
							auto item  = (UINT)LOWORD(wparam);
							auto flags = (UINT)HIWORD(wparam);
							auto menu  = (HMENU)lparam;

							OutputDebugStringA(pr::FmtS("OnMenuSelect: id: %d, flags: %x, handle: %x\n", item, flags, menu));
							if (flags != 0x0000FFFF) // Menu closing notification
							{
								m_sel.m_menu  = menu;
								m_sel.m_flags = flags;
								m_sel.m_id    = item;
								GetMenuItemRect(m_hwnd, menu, item, &m_sel.m_rect);
							}
							break;
						}
					case WM_LBUTTONDOWN:
						{
							OutputDebugStringA(pr::FmtS("OnLButtonDown:\n"));
							break;
						}
					case WM_KEYDOWN:
						{
							OutputDebugStringA(pr::FmtS("OnKeyDown:\n"));
							break;
						}
					case WM_CAPTURECHANGED:
						{
							// Capture changed occurs just as the menu is closing
							// but, crucially, while the window is still visible
							// :-/ doesn't work for sub menus...
							//m_result = ApplySelection();
							break;
						}
					case WM_MENUCHAR:
						{
							auto ch    = (TCHAR)LOWORD(wparam);
							auto flags = (UINT)HIWORD(wparam);
							auto menu  = (HMENU)lparam;
							OutputDebugStringA("OnMenuChar\n");
							(void)ch,flags,menu;
							break;
						}
					case WM_MEASUREITEM:
						{
							auto mi = reinterpret_cast<MEASUREITEMSTRUCT*>(lparam);

							// If you get a crash in here, check that a "static_cast<ContextMenuItem*>(this)" pointer was used in AppendMenuW()
							if (mi->CtlType == ODT_MENU)
							{
								GdiGraphics gfx(m_hwnd);
								auto item = static_cast<ContextMenuItem*>((void*)(mi->itemData));
								item->MeasureItem(*m_owner, gfx, mi);
							}
							break;
						}
					case WM_DRAWITEM:
						{
							auto di = reinterpret_cast<DRAWITEMSTRUCT*>(lparam);

							// If you get a crash in here, check that a "static_cast<ContextMenuItem*>(this)" pointer was used in AppendMenuW()
							if (di->CtlType == ODT_MENU)
							{
								GdiGraphics gfx(di->hDC);
								auto item = static_cast<ContextMenuItem*>((void*)(di->itemData));
								item->DrawItem(*m_owner, gfx, di);
							}
							break;
						}
					}
					return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
				}
			};

			MenuParentWindow m_win; // A zero-sized window that handles the menu messages
			pr::gui::Menu m_root;   // The handle to the popup menu
			Label m_label;          // The text label for the submenu

			HWND CtrlHost() const override
			{
				return m_win;
			}
			HMENU Menu() const override
			{
				return m_root;
			}
			void MeasureItem(ContextMenu& menu, GdiGraphics& gfx, MEASUREITEMSTRUCT* mi) override
			{
				m_label.MeasureItem(menu, gfx, mi);
			}
			void DrawItem(ContextMenu& menu, GdiGraphics& gfx, DRAWITEMSTRUCT* di) override
			{
				m_label.DrawItem(menu, gfx, di);
			}

		public:

			// The context menu item is created from a zero-size dialog template.
			// This allows the creation of the window to be deferred until the menu is displayed (and parented to an arbitrary window) 
			ContextMenu(ContextMenu* parent = nullptr, TCHAR const* text = nullptr, StylePtr style = nullptr, BitmapPtr bm = nullptr)
				:ContextMenuItem(-1, parent, 0, style, bm)
				,m_win(this)
				,m_root(::CreatePopupMenu())
				,m_label(parent, text, style, bm)
			{
				// If no style is given and no parent to inherit the style from, create a default style
				if (!m_style && !m_parent)
					m_style = std::make_shared<ContextMenuStyle>();

				// If this is a sub menu, append it to the parent
				if (m_parent)
				{
					auto cmi = static_cast<ContextMenuItem*>(this);
					::AppendMenuW(m_parent->Menu(), MF_OWNERDRAW|MF_POPUP, UINT_PTR(m_root.m_menu), LPCWSTR(cmi));
				}
			}

			// Show the context menu. Blocks until the menu is closed
			// Returns the id of the item selected
			int Show(HWND parent, int x, int y)
			{
				//m_win.Show(SW_HIDE, parent);
				TrackPopupMenu(m_root, GetSystemMetrics(SM_MENUDROPALIGNMENT)|TPM_LEFTBUTTON, x, y, 0, m_win, nullptr);
				return m_win.m_sel.m_id;
			}

			// Return the HMENU associated with this menu
			operator HMENU() const
			{
				return m_root;
			}
		};

		#pragma region Implementation


		#pragma endregion

/*
#if 0
			class Combo :public ControlHost<Combo> ,public Label
			{
			protected:
				enum { InnerMargin = 2, OuterMargin = 2, ArwBtnWidth = 20 };
				CComboBox     m_combo;         // The hosted combo control
				GdiRect m_rect_value;    // The dimensions of the largest value
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
				GdiRect CtrlRect(GdiRect const& rect, int margin) const
				{
					return GdiRect(
						rect.X + margin + style.m_text_margin + m_rect_text.Width + style.m_text_margin + OuterMargin,
						rect.Y + OuterMargin,
						m_rect_value.Width + 2*InnerMargin + ArwBtnWidth,
						m_rect_value.Height + 2*InnerMargin);
				}
				CWindow ContextMenuWnd() const
				{
					return ::FindWindowW(L"#32768", 0);
				}
				void MeasureItem(ContextMenu* menu, GdiGraphics& gfx, MEASUREITEMSTRUCT* mi)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;

					// Measure the label portion of the item
					Label::MeasureItem(menu, gfx, mi);

					// Measure the combo box portion
					GdiRectF text_sz(0,0,0,0);
					for (auto value : m_values)
					{
						GdiRectF sz; gfx.MeasureString(value.c_str(), int(value.size()), style->m_font_text.get(), GdiPointF(), 0, &sz);
						GdiRectF::Union(text_sz, text_sz, sz);
					}
					m_rect_value = ToRect(text_sz);
					m_rect_value.Width = std::max(m_rect_value.Width, m_min_width);

					// Expand the item dimensions
					auto r = m_rect_value;
					r = Inflate(r, InnerMargin, InnerMargin);
					r.Width += ArwBtnWidth;
					r = Inflate(r, OuterMargin, OuterMargin);
					mi->itemWidth += r.Width;
					mi->itemHeight = std::max(int(mi->itemHeight), r.Height);
				}
				void DrawItem(ContextMenu* menu, GdiGraphics& gfx, DRAWITEMSTRUCT* di)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;
					bool disabled = (di->itemState & ODS_DISABLED) != 0;
					auto rect = ToRect(di->rcItem);
					auto rect_ctrl = CtrlRect(rect, style->m_margin_left);
					auto rect_btn  = rect_ctrl; rect_btn.X = rect_btn.GetRight() - ArwBtnWidth; rect_btn.Width = ArwBtnWidth;

					GdiSolidBrush bsh_edittxt(style->m_col_text);
					GdiSolidBrush bsh_bkgd(disabled ? GdiColor((GdiARGB)GdiColor::LightGray) : GdiColor((GdiARGB)GdiColor::WhiteSmoke));
					GdiSolidBrush bsh_bkgdarw(GdiColor((GdiARGB)GdiColor::LightGray));
					GdiPen        pen_brdr(GdiColor((GdiARGB)GdiColor::Black), 0);

					// Draw the label
					Label::DrawItem(menu, gfx, di);

					// Draw the combo box
					wchar_t const* arw = L"6";
					gfx.FillRectangle(&bsh_bkgd, rect_ctrl);
					gfx.DrawRectangle(&pen_brdr, rect_ctrl);
					gfx.FillRectangle(&bsh_bkgdarw, rect_btn);
					gfx.DrawRectangle(&pen_brdr, rect_btn);
					GdiRectF rect_arw; gfx.MeasureString(arw, 1, style->m_font_marks.get(), GdiPointF(), &rect_arw);
					GdiPointF pt(rect_btn.X + (rect_btn.Width - rect_arw.Width) * 0.5f, rect_btn.Y + (rect_btn.Height - rect_arw.Height) * 0.5f);
					gfx.DrawString(arw, 1, style->m_font_marks.get(), pt, &bsh_edittxt);

					// Draw the selected value text
					if (m_index < int(m_values.size()))
					{
						GdiPointF pt(float(rect_ctrl.X + InnerMargin), float(rect_ctrl.Y + InnerMargin));
						gfx.DrawString(m_values[m_index].c_str(), int(m_values[m_index].size()), style->m_font_text.get(), pt, 0, &bsh_edittxt);
					}
				}
				void Selected(ContextMenu* menu, RECT const& rect, MENUITEMINFO const&)
				{
					StylePtr const& style = m_style ? m_style : menu->m_def_style;

					// Set the font to use in the control
					GdiGraphics gfx(menu->m_hWnd); LOGFONTW logfont;
					FontPtr font = m_value_font ? m_value_font : style->m_font_text;
					if (font && font->GetLogFontW(&gfx, &logfont) == GdiOk) m_font = ::CreateFontIndirectW(&logfont);

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
#endif
*/
/*

		protected:

			pr::GdiPlus   m_gdip_ctx; // Ensure the dlls have been loaded
			ItemCont      m_items;    // The items in this menu
			pr::gui::Menu m_root;     // The handle to the root menu
			Selection     m_sel;      // The menu item that was selected

			int ApplySelection()
			{
				if (!m_sel.m_menu || (m_sel.m_flags & (MF_POPUP|MF_DISABLED|MF_GRAYED)) != 0)
					return 0;

				MenuItemInfo mii; mii.fMask = MIIM_DATA;
				GetMenuItemInfo(m_sel.m_menu, m_sel.m_id, TRUE, &mii);
				IContextMenuItem* item = static_cast<IContextMenuItem*>((void*)(mii.dwItemData));
				item->Selected(*this, m_sel.m_rect, mii);
				return item->GetId();
			}

			// IContextMenuItem interface implementation
			//int GetId() const
			//{
			//	return m_label.GetId();
			//}
			void AddToMenu(ContextMenu& menu, int) override
			{
				// Link up the submenu items
				int index = 0;
				for (auto& item : m_items)
					item->AddToMenu(*this, index++);

				auto cmi = static_cast<IContextMenuItem*>(this);
				::AppendMenuW(menu.m_root, MF_POPUP|MF_OWNERDRAW, UINT_PTR(m_root.m_menu), LPCWSTR(cmi));
			}

		public:

			StylePtr m_def_style; // A default style for when 'm_style' pointers are null
			//Label    m_label;     // The text for this menu (used when added as a submenu)

			ContextMenu(HWND parent, TCHAR const* text = _T("Menu"), int check_state = 0, StylePtr style = StylePtr())
				:Form<ContextMenu>(_T("menu"), parent, 0, 0, 0, 0, DS_CENTER|DS_SHELLFONT|WS_VISIBLE|WS_GROUP|WS_POPUP, 0, -1, "ctx-menu")
				,IContextMenuItem(0)
				,m_gdip_ctx()
				,m_items()
				,m_root(::CreatePopupMenu())
				,m_def_style(new ContextMenuStyle())
				//,m_label(text, id, check_state, style)
				,m_sel()
			{}

			// Add a context menu item to this menu
			void AddItem(ItemPtr item)
			{
				// Note: item ID's for the menu items are indices.
				item->AddToMenu(*this, int(m_items.size()));
				m_items.push_back(item);
			}

			// Add a context menu item to this menu, 'item' *MUST* be allocated using 'new'
			template <typename Item> Item& AddItem(ItemPtr item)
			{
				AddItem(item);
				return static_cast<Item&>(*item);
			}

			// Add an item by raw pointer (held as a shared pointer)
			template <typename Item> Item& AddItem(Item* item)
			{
				return AddItem<Item>(std::shared_ptr<Item>(item));
			}

		};
*/
	}
}
