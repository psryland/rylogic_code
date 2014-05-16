//********************************************************************
// CRndButton
//  Copyright (c) Rylogic Ltd 2009
//********************************************************************

#pragma once
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>
#include <atlcrack.h>
#include <algorithm>
#include <string>
#include "pr/common/assert.h"
#include "pr/gui/misc.h"

namespace pr
{
	namespace gui
	{
		class CRndButton :public WTL::CButton
		{
			int m_radius;
		public:
			BEGIN_MSG_MAP(CRndButton)
				//MSG_WM_LBUTTONUP(OnLButtonUp)
				//MSG_WM_MOUSEMOVE(OnMouseMove)
				MESSAGE_HANDLER(WM_DRAWITEM, OnDrawItem)
				//MSG_WM_CAPTURECHANGED(OnCaptureChanged)
			END_MSG_MAP()
			
			// This Function is called each time, the Button needs a redraw
			LRESULT OnDrawItem(UINT, WPARAM, LPARAM, BOOL&)
			{
				PAINTSTRUCT ps;
				BeginPaint(&ps);
				
				// Generate in memory dc
				CMemoryDC mem_dc(ps.hdc, ps.rcPaint);
				mem_dc.FillRect(&ps.rcPaint, (HBRUSH)::GetStockObject(GRAY_BRUSH));
				
				// Copy correct bitmap to screen
				::BitBlt(ps.hdc, ps.rcPaint.left, ps.rcPaint.top, m_radius, m_radius, mem_dc, 0, 0, SRCCOPY);
				EndPaint(&ps);
				return S_OK;
			}
		};
		
		/*
		class CRndButton2 :public WTL::CButton
		{
		public:
			// State of Button
			enum ButtonState
			{
				BS_DISABLED     = 0,    // Button is disabled
				BS_ENABLED      = 1,    // Button is enabled, but not clicked
				BS_CLICKED      = 2,    // Button is clicked, meaning selected as check button
				BS_PRESSED      = 3,    // Button is pressed with the mouse right now
				BS_HOT          = 4,    // Button is hot, meaning mouse cursor is over button
				BS_LAST_STATE   = 5     // last known State
			};
			
			// Structure declaring Color-Scheme
			struct ColorScheme
			{
				COLORREF m_tDisabled;  // Button is disabled
				COLORREF m_tEnabled;   // Button is enabled but not clicked
				COLORREF m_tClicked;   // Button is clicked, meaning checked as check box or selected as radio button
				COLORREF m_tPressed;   // Button is pressed, Mouse is on button and left mouse button pressed
				COLORREF m_tHot;       // Button is hot, not yet implemented
			};
			
			// Structure declaring Button-Style
			struct BtnStyle
			{
				float m_dSizeAA;                    // Size of Anti-Aliasing-Zone
				float m_dRadius;                    // Radius of Button
				float m_dBorderRatio;               // ...Part of Radius is Border
				float m_dHeightBorder;              // Height of Border
				float m_dHeightButton;              // Height of Button face
				float m_dHighLightX, m_dHighLightY; // Position of HighLight
				float m_dRadiusHighLight;           // Radius of HighLight
				float m_dPowerHighLight;            // Power of Highlight
				
				// Color-Scheme of Button
				ColorScheme m_tColorBack;
				ColorScheme m_tColorBorder;
				ColorScheme m_tColorFace;
			};
			
			// Button style data
			class Style
			{
				bool           m_btn_drawn;      // Is Button already drawn?
				CSize          m_btn_size;       // Size of generated Mask-Edge (use 2 * m_btn_size + 1 for whole mask)
				CBitmap        m_bmp_btn_edge;   // Bitmap of Button-Edge
				CBitmap        m_bmp_btns;       // Bitmap of stated Buttons
				BtnStyle m_btn_style;      // Current Style of Button
				
			public:

				// Constructor
				Style() :m_btn_drawn(false)
				{
					// No Image => No Size
					m_btn_size  = CSize(0, 0);
					m_btn_style.m_dSizeAA                    = 2.0;  // Set Standard-AntiAliasing-Zone
					m_btn_style.m_dHighLightX                =  0.0; // Set Standard-Position of HighLight
					m_btn_style.m_dHighLightY                = -7.0;
					m_btn_style.m_dRadius                    = 10.0; // Set Radii of Edges
					m_btn_style.m_dBorderRatio               = 0.2;
					m_btn_style.m_dHeightBorder              = 0.5; // Set Heights of Button
					m_btn_style.m_dHeightButton              = 0.5;
					m_btn_style.m_dRadiusHighLight           = 7.0; // Set Data of Highlight
					m_btn_style.m_dPowerHighLight            = 0.4;
					m_btn_style.m_tColorBack.m_tDisabled     = GetSysColor(COLOR_3DFACE); // Set Colors for different States
					m_btn_style.m_tColorBorder.m_tDisabled   = RGB(128, 128, 128);
					m_btn_style.m_tColorFace.m_tDisabled     = RGB(128, 128, 128);
					m_btn_style.m_tColorBack.m_tEnabled      = GetSysColor(COLOR_3DFACE);
					m_btn_style.m_tColorBorder.m_tEnabled    = RGB(164, 128, 128);
					m_btn_style.m_tColorFace.m_tEnabled      = RGB(164, 164, 164);
					m_btn_style.m_tColorBack.m_tClicked      = GetSysColor(COLOR_3DFACE);
					m_btn_style.m_tColorBorder.m_tClicked    = RGB(255, 255,   0);
					m_btn_style.m_tColorFace.m_tClicked      = RGB(164, 164, 164);
					m_btn_style.m_tColorBack.m_tPressed      = GetSysColor(COLOR_3DFACE);
					m_btn_style.m_tColorBorder.m_tPressed    = RGB(164, 128, 128);
					m_btn_style.m_tColorFace.m_tPressed      = RGB(64,  64,  64);
					m_btn_style.m_tColorBack.m_tHot          = GetSysColor(COLOR_3DFACE);
					m_btn_style.m_tColorBorder.m_tHot        = RGB(164, 128, 128);
					m_btn_style.m_tColorFace.m_tHot          = RGB(192, 192, 192);
				}
				
				// Get pointer to bitmap containing edges of button-face
				CBitmap* GetButtonEdge(HDC hdc)
				{
					// Check, if button needs to be redrawn
					if (m_btn_drawn) return &m_bmp_btn_edge;
					m_btn_drawn = true;
					
					// Draw Masks of Button
					{
						// Create DC in Memory
						CMemoryDC mem_dc(hdc, ;
						if (mem_dc.CreateCompatibleDC(dc->) == FALSE) return false;
						
						float      fDistCenter = 0.0; // Distance from Center of Button
						float      fDistHigh   = 0.0; // Distance from Highlight-Center
						float      fXHigh, fYHigh;    // Position of Highlight
						float      fFacBack    = 0.0; // Color-Factor of Background-Color
						float      fFacBorder  = 0.0; // Color-Factor of Border-Color
						float      fFacFace    = 0.0; // Color-Factor of Button-Face-Color
						float      fFacHigh    = 0.0; // Color-Factor of Highlight-Color
						float      fFacR,fFacG,fFacB; // Color-Factor Green
						COLORREF   tColPixel;         // Color of actual Pixel
						float      fSizeAA;           // Size of Anti-Aliasing-Region
						float      fRadOuter;         // Radius of Outer Rim (between Border and Nirvana)
						float      fRadInner;         // Radius of Inner Rim (between Button-Face and Border)
						float      fRatioBorder;      // Ratio of Border
						float      fHeightBorder;     // Height of Border
						float      fHeightButton;     // Height of Button-Face
						float      fRadHigh;          // Radius of Highlight
						float      fPowHigh;          // Power of Highlight
						int        nSizeEdge = 0;     // Size of single Edge
						
						// Load Position of HighLight
						fSizeAA = m_btn_style.m_dSizeAA;
						fXHigh  = m_btn_style.m_dHighLightX;
						fYHigh  = m_btn_style.m_dHighLightY;
						fRadOuter       = m_btn_style.m_dRadius;
						fRatioBorder    = m_btn_style.m_dBorderRatio;
						fHeightBorder   = m_btn_style.m_dHeightBorder;
						fHeightButton   = m_btn_style.m_dHeightButton;
						fRadHigh        = m_btn_style.m_dRadiusHighLight;
						fPowHigh        = m_btn_style.m_dPowerHighLight;
						
						fRadInner = std::min(fRadOuter, std::max(0.0f, fRadOuter * (1.0f - fRatioBorder))); // Calculate Radius of Inner Border
						nSizeEdge = (int)ceil(fRadOuter + fSizeAA / 2.0); // Calculate Size of an Edge
						m_btn_size.SetSize(nSizeEdge, nSizeEdge); // Store Size of Mask in global var
						if (m_bmp_btn_edge.m_hBitmap != 0) m_bmp_btn_edge.DeleteObject();
						m_bmp_btn_edge.CreateCompatibleBitmap(dc, 2 * nSizeEdge + 1, (2 * nSizeEdge + 1) * BS_LAST_STATE); // Generate new Bitmap
						HGDIOBJ old_bmp = mem_dc.SelectObject(m_bmp_btn_edge); // Select Bitmap of Button-Edge into DC
						
						// Draw Button-Edge
						for (int nX = -nSizeEdge; nX <= nSizeEdge; nX++)
						{
							for (int nY = -nSizeEdge; nY <= nSizeEdge; nY++)
							{
								// Calculate Distance of Point from Center of Button
								fDistCenter = sqrt((float)nX * (float)nX + (float)nY * (float)nY);
								// Calculate factor of Background
								fFacBack    = std::max(0.0, std::min(1.0, 0.5 + (fDistCenter - fRadOuter) * 2.0 / fSizeAA));
								// Calculate Factor for Border
								fFacBorder  = 1.0 - fHeightBorder * pow((fRadOuter + fRadInner - fDistCenter * 2.0) / (fRadOuter - fRadInner) ,2);
								fFacBorder  = std::max(0.0, std::min(1.0, 0.5 - (fDistCenter - fRadOuter) * 2.0 / fSizeAA)) * fFacBorder;
								fFacBorder  = std::max(0.0, std::min(1.0, 0.5 + (fDistCenter - fRadInner) * 2.0 / fSizeAA)) * fFacBorder;
								for (int nState = 0; nState < BS_LAST_STATE; nState++)
								{
									// Get Colors of State
									COLORREF tColorBack, tColorBorder, tColorFace;
									switch (nState)
									{
									case BS_ENABLED:
										tColorBack      = m_btn_style.m_tColorBack.m_tEnabled;
										tColorBorder    = m_btn_style.m_tColorBorder.m_tEnabled;
										tColorFace      = m_btn_style.m_tColorFace.m_tEnabled;
										break;
									case BS_CLICKED:
										tColorBack      = m_btn_style.m_tColorBack.m_tClicked;
										tColorBorder    = m_btn_style.m_tColorBorder.m_tClicked;
										tColorFace      = m_btn_style.m_tColorFace.m_tClicked;
										break;
									case BS_PRESSED:
										tColorBack      = m_btn_style.m_tColorBack.m_tPressed;
										tColorBorder    = m_btn_style.m_tColorBorder.m_tPressed;
										tColorFace      = m_btn_style.m_tColorFace.m_tPressed;
										break;
									case BS_HOT:
										tColorBack      = m_btn_style.m_tColorBack.m_tHot;
										tColorBorder    = m_btn_style.m_tColorBorder.m_tHot;
										tColorFace      = m_btn_style.m_tColorFace.m_tHot;
										break;
									case BS_DISABLED:
									default:
										tColorBack      = m_btn_style.m_tColorBack.m_tDisabled;
										tColorBorder    = m_btn_style.m_tColorBorder.m_tDisabled;
										tColorFace      = m_btn_style.m_tColorFace.m_tDisabled;
										break;
									}
									// Calculate Distance of Point from Highlight of Button
									fDistHigh   = sqrt(((float)nX - fXHigh) * ((float)nX - fXHigh) + ((float)nY - fYHigh) * ((float)nY - fYHigh));
									
									// Calculate Factor of Inner Surface
									if (fHeightButton > 0)
										fFacFace    = 1.0 - fHeightButton * (fDistCenter / fRadInner) * (fDistCenter / fRadInner);
									else
										fFacFace    = 1.0 + fHeightButton - fHeightButton * (fDistCenter / fRadInner) * (fDistCenter / fRadInner);
									
									fFacFace    = std::max(0.0, std::min(1.0, 0.5 - (fDistCenter - fRadInner) * 2.0 / fSizeAA)) * fFacFace;
									// Calculate Factor of Highlight
									fFacHigh    = 1.0 + std::max(-1.0, std::min(1.0, 1.0 - fHeightButton * fDistHigh / fRadHigh)) * fPowHigh;
									fFacFace = fFacFace * fFacHigh;
									// Calculate Color-Factors
									fFacR =
										(float)GetRValue(tColorBack)    * fFacBack +
										(float)GetRValue(tColorBorder)  * fFacBorder +
										(float)GetRValue(tColorFace)    * fFacFace;
									fFacG =
										(float)GetGValue(tColorBack)    * fFacBack +
										(float)GetGValue(tColorBorder)  * fFacBorder +
										(float)GetGValue(tColorFace)    * fFacFace;
									fFacB =
										(float)GetBValue(tColorBack)    * fFacBack +
										(float)GetBValue(tColorBorder)  * fFacBorder +
										(float)GetBValue(tColorFace)    * fFacFace;
									// Calculate actual Color of Pixel
									tColPixel = RGB(
													std::max(0, std::min(255, (int)fFacR)),
													std::max(0, std::min(255, (int)fFacG)),
													std::max(0, std::min(255, (int)fFacB))
												);
									// Draw Pixels
									mem_dc.SetPixel(nSizeEdge + nX, nSizeEdge + nY + (2 * nSizeEdge + 1) * nState, tColPixel);
								}
							}
						}
						// Select Old Bitmap into DC
						mem_dc.SelectObject(old_bmp);
						return true;
					}
					return &m_bmp_btn_edge;
				}
				
				// Get Size of Edges
				CSize GetEdgeSize()
				{
					return m_btn_size;
				}
				
				// Get Size of Masks
				CSize GetMaskSize()
				{
					return CSize(2 * m_btn_size.cx + 1, 2 * m_btn_size.cy + 1);
				}
			};
			
		private:
			CRect           m_btn_size;       // Size of Button-Images
			CBitmap         m_bmp;            // Image of Buttons
			CFont           m_font_btn;       // Font for Caption
			LOGFONT         m_log_font;       // Data-Block for Font
			ColorScheme     m_text_colour;    // Color Scheme of Caption
			std::string     m_old_caption;    // Stored Old Caption to recognize the need for a redraw
			bool            m_default_btn;    // Is Button Default-Button
			bool            m_check_btn;      // Is Check-Button
			bool            m_radio_btn;      // Is Radio-Button
			bool            m_hot_btn;        // Is Hot-Button
			bool            m_checked;        // Is Checked
			bool            m_mouse_on_btn;   // The Mouse is on the Button-Area, needed for Hot-Button
			bool            m_redraw;         // Button should be redrawn
			Style*          m_style;          // Structure containing Style of Button
			
		public:
			BEGIN_MSG_MAP(CRndButton2)
				MSG_WM_LBUTTONUP(OnLButtonUp)
				MSG_WM_MOUSEMOVE(OnMouseMove)
				MSG_WM_DRAWITEM(OnDrawItem)
				MSG_WM_CAPTURECHANGED(OnCaptureChanged)
			END_MSG_MAP()

			CRndButton2()
			:m_default_btn(false)
			,m_check_btn(false)
			,m_radio_btn(false)
			,m_hot_btn(false)
			,m_mouse_on_btn(false)
			,m_checked(false)
			,m_style(0)
			,m_btn_size(0, 0, 0, 0)
			,m_redraw(false)
			,m_old_caption()
			{
				// Set standards in font-style
				m_log_font.lfHeight         = 16;
				m_log_font.lfWidth          = 0;
				m_log_font.lfEscapement     = 0;
				m_log_font.lfOrientation    = 0;
				m_log_font.lfWeight         = FW_BOLD;
				m_log_font.lfItalic         = false;
				m_log_font.lfUnderline      = false;
				m_log_font.lfStrikeOut      = false;
				m_log_font.lfCharSet        = DEFAULT_CHARSET;
				m_log_font.lfOutPrecision   = OUT_DEFAULT_PRECIS;
				m_log_font.lfClipPrecision  = CLIP_DEFAULT_PRECIS;
				m_log_font.lfQuality        = ANTIALIASED_QUALITY;
				m_log_font.lfPitchAndFamily = DEFAULT_PITCH;
				strcpy(m_log_font.lfFaceName, "Tahoma");
				m_font_btn.CreateFontIndirect(&m_log_font);
				
				// Set Standard Font-Color
				m_text_colour.m_tDisabled    = RGB(64, 64, 64);
				m_text_colour.m_tEnabled     = RGB(0,  0,  0);
				m_text_colour.m_tClicked     = RGB(0,  0,  0);
				m_text_colour.m_tPressed     = RGB(0,  0,  0);
				m_text_colour.m_tHot         = RGB(0,  0,  0);
			}
			
			// Set Style of Button
			void SetRoundButtonStyle(Style* round_button_style)
			{
				PR_ASSERT(PR_DBG, round_button_style, "");
				m_style = round_button_style;
				m_redraw = true;
			}
			
			// Presubclass-Window-Function
			void PreSubclassWindow()
			{
				// Check if it's a default button
				if (GetStyle() & 0x0FL) m_default_btn = true;
				
				// Make the button owner-drawn
				ModifyStyle(0x0FL, BS_OWNERDRAW | BS_AUTOCHECKBOX, SWP_FRAMECHANGED);
				CButton::PreSubclassWindow();
			}
			
			// Generate Bitmaps to hold Buttons
			void GenButtonBmps(HDC hdc, CRect rect)
			{
				if (m_bmp.m_hBitmap != 0) m_bmp.DeleteObject();
				m_bmp.m_hBitmap = 0;
				if (m_bmp.CreateCompatibleBitmap(hdc, rect.Width(), rect.Height() * BS_LAST_STATE))
					m_btn_size = rect;
				else
					m_btn_size = CRect(0, 0, 0, 0);
			}
			
			// This Function is called each time, the Button needs a redraw
			void OnDrawItem(int, LPDRAWITEMSTRUCT draw_item)
			{
				// Button size changed
				bool gen = !m_btn_size.EqualRect(&draw_item->rcItem) || m_redraw;
				if (gen) GenButtonBmps(draw_item->hDC, draw_item->rcItem); // Generate bitmap to hold buttons
				
				// Generate in memory dc
				CMemoryDC mem_dc(draw_item->hDC, draw_item->rcItem);
				
				// Get the current caption
				std::string curr_caption = pr::GetCtrlText(*this);
				gen |= curr_caption != m_old_caption;
				m_old_caption = curr_caption;
				
				// Generate the button images in the memory dc
				if (gen)
				{
					// Draw the button face
					PR_ASSERT(PR_DBG, m_style, "a style is needed");
					
					// Create Memory-DC
					CMemoryDC src_dc(mem_dc, draw_item->rcItem);
					
					// Get pointer to bitmap of masks
					CBitmap* pButtonMasks = m_style->GetButtonEdge(mem_dc);
					
					// Select Working Objects into DCs
					CSize edge_size = m_style->GetEdgeSize();
					CSize mask_size = m_style->GetMaskSize();
					
					// Correct Edge-Size for smaller Buttons
					CSize fixed_edge_size;
					fixed_edge_size.cx = std::min(edge_size.cx, std::min(m_btn_size.Width() / 2, m_btn_size.Height() / 2));
					fixed_edge_size.cy = fixed_edge_size.cx;
					for (int nState = 0; nState < BS_LAST_STATE; nState++)
					{
						mem_dc.StretchBlt(0, nState * m_btn_size.Height(), fixed_edge_size.cx, fixed_edge_size.cy, &src_dc, 0, nState * mask_size.cy, edge_size.cx, edge_size.cy, SRCCOPY); // Left-Top
						mem_dc.StretchBlt(0, nState * m_btn_size.Height() + m_btn_size.Height() - fixed_edge_size.cy, fixed_edge_size.cx, fixed_edge_size.cy, &src_dc, 0, nState * mask_size.cy + mask_size.cy - edge_size.cy, edge_size.cx, edge_size.cy, SRCCOPY); // Left-Bottom
						mem_dc.StretchBlt(m_btn_size.Width() - fixed_edge_size.cx, nState * m_btn_size.Height(), fixed_edge_size.cx, fixed_edge_size.cy, &src_dc, mask_size.cx - edge_size.cx, nState * mask_size.cy, edge_size.cx, edge_size.cy, SRCCOPY);// Right-Top
						mem_dc.StretchBlt(m_btn_size.Width() - fixed_edge_size.cx, nState * m_btn_size.Height() + m_btn_size.Height() - fixed_edge_size.cy, fixed_edge_size.cx, fixed_edge_size.cy, &src_dc, mask_size.cx - edge_size.cx, nState * mask_size.cy + mask_size.cy - edge_size.cy, edge_size.cx, edge_size.cy, SRCCOPY); // Right-Bottom
						mem_dc.StretchBlt(fixed_edge_size.cx, nState * m_btn_size.Height(), m_btn_size.Width() - 2 * fixed_edge_size.cx, fixed_edge_size.cy, &src_dc, edge_size.cx, nState * mask_size.cy, 1, edge_size.cy, SRCCOPY); // Top
						mem_dc.StretchBlt(fixed_edge_size.cx, nState * m_btn_size.Height() + m_btn_size.Height() - fixed_edge_size.cy, m_btn_size.Width() - 2 * fixed_edge_size.cx, fixed_edge_size.cy, &src_dc, edge_size.cx, nState * mask_size.cy + mask_size.cy - edge_size.cy, 1, edge_size.cy, SRCCOPY); // Bottom
						mem_dc.StretchBlt(0, nState * m_btn_size.Height() + fixed_edge_size.cy, fixed_edge_size.cx, m_btn_size.Height() - 2 * fixed_edge_size.cy, &src_dc, 0, nState * mask_size.cy + edge_size.cy, edge_size.cx, 1, SRCCOPY); // Left
						mem_dc.StretchBlt(m_btn_size.Width() - fixed_edge_size.cx, nState * m_btn_size.Height() + fixed_edge_size.cy, fixed_edge_size.cx, m_btn_size.Height() - 2 * fixed_edge_size.cy, &src_dc, mask_size.cx - edge_size.cx, nState * mask_size.cy + edge_size.cy, edge_size.cx, 1, SRCCOPY); // Right
						mem_dc.StretchBlt(fixed_edge_size.cx, nState * m_btn_size.Height() + fixed_edge_size.cy, m_btn_size.Width() - 2* fixed_edge_size.cx, m_btn_size.Height() - 2 * fixed_edge_size.cy, &src_dc, edge_size.cx, nState * mask_size.cy + edge_size.cy, 1, 1, SRCCOPY);
					}
					
					// Draw button caption
					int      nOldBckMode = mem_dc.SetBkMode(TRANSPARENT); // Select Transparency for Background
					COLORREF tOldColor   = mem_dc.SetTextColor(RGB(0,0,0)); // Get old Text-Color
					HGDIOBJ  hOldFont    = mem_dc.SelectObject(&m_font_btn); // Select Font into DC
					
					// Get Caption of Button
					std::string sCaption = pr::GetCtrlText(*this);
					for (int nState = 0; nState < BS_LAST_STATE; nState++)
					{
						switch (nState)
						{
						default:
						case BS_DISABLED: mem_dc.SetTextColor(m_text_colour.m_tDisabled); break;
						case BS_ENABLED:  mem_dc.SetTextColor(m_text_colour.m_tEnabled); break;
						case BS_CLICKED:  mem_dc.SetTextColor(m_text_colour.m_tClicked); break;
						case BS_PRESSED:  mem_dc.SetTextColor(m_text_colour.m_tPressed); break;
						case BS_HOT:      mem_dc.SetTextColor(m_text_colour.m_tHot); break;
						}
						mem_dc.DrawText(sCaption.c_str(), CRect(m_btn_size.left, nState * m_btn_size.Height() + m_btn_size.top, m_btn_size.right, nState * m_btn_size.Height() + m_btn_size.bottom), DT_CENTER | DT_VCENTER | DT_SINGLELINE);
					}
					
					mem_dc.SelectObject(hOldFont);  // Select Old Font back
					mem_dc.SetBkMode(nOldBckMode);  // Set old Background-Mode
					mem_dc.SetTextColor(tOldColor); // Set old Text-Color
				}
				
				int btn_state = BS_ENABLED;
				if      ((draw_item->itemState & ODS_DISABLED) == ODS_DISABLED) btn_state = BS_DISABLED;
				else if ((draw_item->itemState & ODS_SELECTED) == ODS_SELECTED) btn_state = BS_PRESSED;
				else if (m_checked)                                             btn_state = BS_CLICKED;
				else if (m_hot_btn && m_mouse_on_btn)                           btn_state = BS_HOT;
				
				// Copy correct bitmap to screen
				::BitBlt(draw_item->hDC, draw_item->rcItem.left, draw_item->rcItem.top, m_btn_size.Width(), m_btn_size.Height(), mem_dc, 0, m_btn_size.Height() * btn_state, SRCCOPY);
				m_redraw = false;
			}
			
			// Left mouse up handler
			void OnLButtonUp(UINT nFlags, CPoint point)
			{
				if (m_check_btn) m_checked = !m_checked;
				if (m_radio_btn) m_checked = true;
				CButton::OnLButtonUp(nFlags, point);
			}
			
			void OnMouseMove(UINT nFlags, CPoint point)
			{
				// Check, if Mouse is on Control
				if (pr::IsWithin(pr::ClientArea(*this), pr::iv2::make(point)))
				{
					bool bRedrawNeeded = !m_mouse_on_btn; // We only need to redraw, if the mouse enters
					m_mouse_on_btn = true;                // Mouse is on Control
					SetCapture();                           // Set Capture to recognize, when the mouse leaves the control
					if (m_hot_btn) Invalidate();       // Redraw Control, if Button is hot
				}
				else
				{
					m_mouse_on_btn = false;               // We have lost the mouse-capture, so the mouse has left the buttons face
					ReleaseCapture();                       // Mouse has left the button
					if (m_hot_btn) Invalidate();       // Redraw Control, if Button is hot
				}
				CButton::OnMouseMove(nFlags, point);
			}
			
			// Check, if we lost the mouse-capture
			void OnCaptureChanged(CWindow wnd)
			{
				if (GetCapture() != this)
				{
					// We have lost the mouse-capture, so the mouse has left the buttons face
					m_mouse_on_btn = false;
					
					// Redraw Control, if Button is hot
					if (m_hot_btn)
						Invalidate();
				}
				CButton::OnCaptureChanged(wnd);
			}
			
			// Get/Set current button-style
			BtnStyle const& ButtonStyle() const     { return m_btn_style; }
			void ButtonStyle(BtnStyle const& style) { memcpy(&m_btn_style, style, sizeof(BtnStyle)); m_btn_drawn = false; }
			
			// Get/Set the button font
			LOGFONT const& Font() const { return m_log_font; }
			void Font(LOGFONT const& log_font)
			{
				if (m_font_btn.m_hObject != 0) m_font_btn.DeleteObject();
				memcpy(&m_log_font, log_font, sizeof(LOGFONT));
				m_font_btn.CreateFontIndirect(&m_log_font);
				m_redraw = true;
			}
			
			// Get/Set button text color of caption
			ColorScheme const& TextColor() const           { return m_text_colour; }
			void TextColor(ColorScheme const& text_colour) { memcpy(&m_text_colour, text_colour, sizeof(ColorScheme)); m_redraw = true; }
			
			// Get/Set as a check button
			bool CheckButton() const  { return m_check_btn; }
			void CheckButton(bool on) { m_check_btn = _bCheckButton; }
			
			// Get/Set the checked state
			bool GetCheck() const { return m_checked; }
			void Checked(bool on) { m_checked = on; Invalidate(); }
			
			// Get/Set as a radio button
			bool RadioButton() const  { return m_radio_btn; }
			void RadioButton(bool on) { m_radio_btn = on; }
			
			// Get/Set as a hot button
			bool HotButton() const  { return m_hot_btn; }
			void HotButton(bool on) { m_hot_btn = on; }
		};
		*/
	}
}