//***********************************************
// Graph Control
//  Copyright (c) Rylogic Ltd 2009
//***********************************************

#pragma once
#ifndef PR_GUI_GRAPH_CTRL_H
#define PR_GUI_GRAPH_CTRL_H

#include <vector>
#include <algorithm>
#include <mutex>
#include <thread>
#include <cassert>

#include <gdiplus.h>

#include "pr/common/min_max_fix.h"
//#include "pr/common/assert.h"
//#include "pr/common/colour.h"
//#include "pr/common/fmt.h"
#include "pr/common/multi_cast.h"
#include "pr/maths/maths.h"
#include "pr/gui/gdiplus.h"
#include "pr/gui/context_menu.h"
#include "pr/gui/wingui.h"

namespace pr
{
	namespace gui
	{
		// A default/example data source for the graph control
		struct GraphData
		{
			struct Elem
			{
				float m_x, m_y;
				Elem() :m_x() ,m_y() {}
				Elem(float x, float y) :m_x(x) ,m_y(y) {}
				static bool CmpX(Elem const& lhs, Elem const& rhs) { return lhs.m_x < rhs.m_x; }
			};
			typedef std::vector<Elem> DataCont;
			DataCont m_data;

			// A custom data source must implement these methods:

			// Return the range of indices that need to be considered when plotting from 'xmin' to 'xmax'
			// Note: in general, this range should include one point to the left of xmin and one to the right
			// of xmax so that line graphs plot a line up to the border of the plot area
			void IndexRange(float xmin, float xmax, size_t& imin, size_t& imax) const
			{
				Elem lwr(xmin, 0.0f), upr(xmax, 0.0f);
				auto iter_lwr = std::lower_bound(m_data.begin(), m_data.end(), lwr, Elem::CmpX);
				auto iter_upr = std::upper_bound(iter_lwr      , m_data.end(), upr, Elem::CmpX);
				imin = iter_lwr - m_data.begin(); imin -= imin != 0;
				imax = iter_upr - m_data.begin(); imax += imax != m_data.size();
			}

			// Return the value for a given index
			void Value(size_t idx, float& x, float& y) const
			{
				x = m_data[idx].m_x;
				y = m_data[idx].m_y;
			}

			// Return the range of y values for a given data index
			void ErrValues(size_t, float& ymin, float& ymax) const
			{
				ymin = ymax = 0.0f;
			}
		};

		// A control for rendering a graph
		template <typename DataSrc = GraphData>
		struct GraphCtrl :Control
		{
			struct RdrOptions
			{
				// Transform for positioning the graph title, offset from top centre
				Gdiplus::Matrix m_txfm_title;
				
				// Graph element Colours
				Gdiplus::Color m_col_bkgd;       // Control background colour
				Gdiplus::Color m_col_plot_bkgd;  // Plot background colour
				Gdiplus::Color m_col_title;      // Title string colour
				Gdiplus::Color m_col_axis;       // Axis colour
				Gdiplus::Color m_col_grid;       // Grid line colour
				Gdiplus::Color m_col_select;     // Area selection colour

				// Graph margins
				int m_margin_left;
				int m_margin_top;
				int m_margin_right;
				int m_margin_bottom;

				// Fonts
				Gdiplus::Font m_font_title;
				Gdiplus::Font m_font_note;

				enum class EBorder { None, Single };
				EBorder m_border;
				pr::v2 m_pixels_per_tick;

				RdrOptions()
					:m_txfm_title      (1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)
					,m_col_bkgd        (0xFF000000 | ::GetSysColor(COLOR_BTNFACE))
					,m_col_plot_bkgd   (Gdiplus::ARGB(Gdiplus::Color::WhiteSmoke))
					,m_col_title       (Gdiplus::ARGB(Gdiplus::Color::Black))
					,m_col_axis        (Gdiplus::ARGB(Gdiplus::Color::Black))
					,m_col_grid        (Gdiplus::ARGB(Gdiplus::Color::MakeARGB(255,230,230,230)))
					,m_col_select      (Gdiplus::ARGB(Gdiplus::Color::MakeARGB(255,128,128,128)))
					,m_margin_left     (3)
					,m_margin_top      (3)
					,m_margin_right    (3)
					,m_margin_bottom   (3)
					,m_font_title      (L"tahoma", 18, Gdiplus::FontStyleBold)
					,m_font_note       (L"tahoma",  8, Gdiplus::FontStyleRegular)
					,m_border          (EBorder::None)
					,m_pixels_per_tick (pr::v2::make(30.0f, 24.0f))
				{}
			};
			struct Series
			{
				struct RdrOptions
				{
					enum class EPlotType  { Point, Line, Bar };
					enum class EPlotZeros { Draw, Hide, Skip };

					bool           m_visible;
					bool           m_draw_data;
					EPlotZeros     m_draw_zeros;
					bool           m_draw_error_bars;
					EPlotType      m_plot_type;
					Gdiplus::Color m_col_point;
					float          m_point_size;
					Gdiplus::Color m_col_line;
					float          m_line_width;
					Gdiplus::Color m_col_bar;
					float          m_bar_width;
					Gdiplus::Color m_col_error_bars;
					bool           m_draw_moving_avr;
					int            m_ma_window_size;
					Gdiplus::Color m_col_ma_line;
					float          m_ma_line_width;

					RdrOptions()
						:m_visible         (true)
						,m_draw_data       (true)
						,m_draw_zeros      (EPlotZeros::Draw)
						,m_draw_error_bars (false)
						,m_plot_type       (EPlotType::Line)
						,m_col_point       (0xff, 0x80, 0, 0xff)
						,m_point_size      (5.0f)
						,m_col_line        (0xff, 0, 0, 0xff)
						,m_line_width      (1.0f)
						,m_col_bar         (0xff, 0x80, 0, 0xff)
						,m_bar_width       (0.8f)
						,m_col_error_bars  (0x80, 0xff, 0, 0xff)
						,m_draw_moving_avr (false)
						,m_ma_window_size  (10)
						,m_col_ma_line     (0xff, 0, 0, 0xFF)
						,m_ma_line_width   (3.0f)
					{}
					Gdiplus::Color color() const
					{
						return
							m_plot_type == EPlotType::Point ? m_col_point :
							m_plot_type == EPlotType::Line  ? m_col_line : m_col_bar;
					}
				};

				std::wstring m_name;
				RdrOptions   m_opts;
				DataSrc      m_values;

				Series(std::wstring const& name = L"")
					:m_name(name)
					,m_opts()
					,m_values()
				{}
			};
			struct Axis
			{
				struct Range
				{
					float m_min;
					float m_max;

					Range() :m_min(0.0f) ,m_max(1.0f) {}
					Range(float mn, float mx) :m_min(mn) ,m_max(mx) {}
					float span() const    { return m_max - m_min; }
					void  span(float s)   { float c = centre(); m_min = c - s*0.5f; m_max = c + s*0.5f; }
					float centre() const  { return (m_min + m_max) * 0.5f; }
					void  centre(float c) { float d = c - centre(); m_min += d; m_max += d; }
				};
				struct RdrOptions
				{
					Gdiplus::Matrix m_txfm_label;   // Offset transform from default label position
					Gdiplus::Font   m_font_label;   // Font of axis label
					Gdiplus::Font   m_font_tick;    // Font of tick labels
					Gdiplus::Color  m_col_label;    // Colour of axis label
					Gdiplus::Color  m_col_tick;     // Colour of tick labels
					int             m_tick_length;  // Length of axis ticks

					RdrOptions()
						:m_txfm_label  (1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)
						,m_font_label  (L"tahoma", 14, Gdiplus::FontStyleRegular)
						,m_font_tick   (L"tahoma", 10, Gdiplus::FontStyleRegular)
						,m_col_label   (Gdiplus::ARGB(Gdiplus::Color::Black))
						,m_col_tick    (Gdiplus::ARGB(Gdiplus::Color::Black))
						,m_tick_length (5)
					{}
				};

				typedef std::wstring (*TickText)(float tick);
				static std::wstring ToText(float tick)
				{
					tick = std::round(tick * 1000.0f) / 1000.0f;
					wchar_t buf[20]; _snwprintf_s(buf, sizeof(buf), L"%.4g", tick);
					return buf;
				}

				std::wstring m_label;
				RdrOptions   m_opts;
				Range        m_range;
				TickText     m_tick_text;
				bool         m_allow_scroll;
				bool         m_allow_zoom;
				bool         m_lock_range;

				Axis()
					:m_label(L"Axis")
					,m_opts()
					,m_range()
					,m_tick_text(ToText)
					,m_allow_scroll(true)
					,m_allow_zoom(true)
					,m_lock_range(false)
				{}
				float min() const     { return m_range.m_min; }
				void  min(float x)    { m_range.m_min = x; }
				float max() const     { return m_range.m_max; }
				void  max(float x)    { m_range.m_max = x; }
				float span() const    { return m_range.span(); }
				void  span(float x)   { m_range.span(x); }
				float centre() const  { return m_range.centre(); }
				void  centre(float x) { m_range.centre(x); }
				void shift(float delta)
				{
					if (!m_allow_scroll) return;
					m_range.m_min += delta;
					m_range.m_max += delta;
				}
			};

			typedef std::vector<Series*> SeriesCont;
			typedef typename Axis::Range AxisRange;
			static LPCTSTR WndClassName()
			{
				return _T("PRGRAPHCTRL");
			}
			static WNDCLASSEX WndClassInfo(HINSTANCE hinst)
			{
				return Control::WndClassInfo<GraphCtrl<DataSrc>>(hinst);
			}

		private:
			struct Snapshot
			{
				std::shared_ptr<Gdiplus::Bitmap> m_bm;     // A copy of the plot area at a moment in time
				AxisRange                        m_xrange; // The x axis range when the snapshot was taken
				AxisRange                        m_yrange; // The y axis range when the snapshot was taken

				Snapshot() :m_bm() ,m_xrange() ,m_yrange() {}
				Size size() const { return Size(m_bm?m_bm->GetWidth():0, m_bm?m_bm->GetHeight():0); }
				Rect rect() const { return Rect(size()); }
			};
			struct Tooltip //:CWindowImpl<Tooltip>
			{
			//	HWND m_parent;
			//	std::wstring  m_text;
			//	Gdiplus::Font m_font;
			//	
			//	Tooltip() :m_text() ,m_font(Gdiplus::FontFamily::GenericSansSerif(), 10.0f) {}
			//	DECLARE_WND_CLASS_EX(_T("PRWTLGRAPHTT"),0,COLOR_INFOBK)
			//	BEGIN_MSG_MAP(Tooltip)
			//		MSG_WM_CREATE(OnCreate)
			//		MSG_WM_PAINT(OnPaint)
			//	END_MSG_MAP()
			//	int OnCreate(LPCREATESTRUCT create)
			//	{
			//		SetWindowPos(0, 0, 0, 0, 0, SWP_NOZORDER|SWP_NOMOVE);
			//		m_parent = create->hwndParent;
			//		return S_OK;
			//	}
			//	void OnPaint(CDCHandle)
			//	{
			//		using namespace Gdiplus;
			//		CPaintDC paint_dc(m_hWnd);
			//		Graphics gfx(paint_dc);
			//		Color bkgd; bkgd.SetFromCOLORREF(::GetSysColor(COLOR_INFOBK));
			//		SolidBrush bsh_text((ARGB)Color::Black);
			//		gfx.Clear(bkgd);
			//		gfx.DrawString(m_text.c_str(), int(m_text.size()), &m_font, PointF(), &bsh_text);
			//	}
				void SetTipText(int x, int y, wchar_t const* text)
				{
					(void)x,y,text;
			//		using namespace Gdiplus;
			//		m_text = text;
			//		Graphics gfx(m_hWnd);
			//		RectF sz; gfx.MeasureString(m_text.c_str(), int(m_text.size()), &m_font, PointF(), &sz);
			//		CPoint pt(x,y); ::ClientToScreen(m_parent, &pt);
			//		SetWindowPos(0, pt.x, pt.y, int(sz.Width), int(sz.Height), SWP_NOZORDER);
			//		Invalidate();
				}
				bool IsWindowVisible() const
				{
					return false;
				}
				void ShowWindow(int show)
				{
					(void)show;
				}
			};

			pr::GdiPlus      m_gdiplus;    // Initialises the Gdiplus library
			std::thread      m_rdr_thread; // A thread used to render
			std::atomic_bool m_rdr_cancel; // Cancel renderering flag
			std::mutex       m_mutex_snap; // A mutex to protect access to the snapshot bitmap
			Snapshot         m_tmp;        // A temporary bitmap used for background thread rendering
			Snapshot         m_snap;       // A snapshot of the plot area, used as a temporary copy during drag/zoom operations

		public:
			std::wstring m_title;  // The graph title
			SeriesCont   m_series; // The container of graph data
			RdrOptions   m_opts;   // Global graph rendering options
			Axis         m_xaxis;  // The x axis
			Axis         m_yaxis;  // The y axis

			// A mutex that is held by the control during rendering.
			// This should be used to synchronise source data changes with rendering.
			std::mutex m_mutex_rdring;

		protected:
			Rect      m_plot_area;   // The area containing the graph data
			AxisRange m_base_xrange; // The default x range of the data
			AxisRange m_base_yrange; // The default y range of the data
			AxisRange m_zoom;        // The amount of zoom (plus limits)
			HCURSOR   m_cur_arrow;   // The normal cursor
			HCURSOR   m_cur_cross;   // The cross cursor
			HCURSOR   m_cur_grab;    // The grab cursor
			Tooltip   m_tt;          // Tracking tooltip for displaying coordinates
			pr::v2    m_pt_grab;     // The grab start location
			Rect      m_selection;   // The selection area
			bool      m_dragging;    // True when the graph is being dragged around
			bool      m_selecting;   // True while performing an area selection
			bool      m_impl_dirty;  // True when the graph must be redrawn

		public:
			GraphCtrl(int x = CW_USEDEFAULT, int y = CW_USEDEFAULT, int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,int id = IDC_UNUSED
				,HWND hwndparent = 0
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = WS_CHILD | WS_VISIBLE | WS_TABSTOP
				,DWORD ex_style = 0
				,char const* name = nullptr)
				:Control(MAKEINTATOM(RegisterWndClass<GraphCtrl<DataSrc>>()), nullptr, x, y, w, h, id, hwndparent, parent, anchor, style, ex_style, name)
				,m_gdiplus()
				,m_rdr_thread()
				,m_rdr_cancel()
				,m_mutex_snap()
				,m_tmp()
				,m_snap()
				,m_title(L"Graph")
				,m_opts()
				,m_xaxis()
				,m_yaxis()
				,m_mutex_rdring()
				,m_base_xrange()
				,m_base_yrange()
				,m_zoom(pr::maths::tiny, pr::maths::float_max)
				,m_cur_arrow()
				,m_cur_cross()
				,m_cur_grab()
				,m_tt()
				,m_pt_grab()
				,m_selection()
				,m_dragging(false)
				,m_selecting(false)
				,m_impl_dirty(true)
			{
				m_cur_arrow = ::LoadCursor(nullptr, IDC_ARROW);
				m_cur_cross = ::LoadCursor(nullptr, IDC_CROSS);
				m_cur_grab  = ::LoadCursor(nullptr, IDC_HAND);
				//m_tt.Create(m_hWnd, 0, 0, WS_BORDER|WS_POPUP, WS_EX_TOPMOST);
			}
			~GraphCtrl()
			{
				m_rdr_cancel = true;
				if (m_rdr_thread.joinable())
					m_rdr_thread.join();
			}

			// Returns a point in graph space from a point in client space
			// Use to convert mouse (client-space) locations to graph coordinates
			pr::v2 PointToGraph(pr::v2 const& point) const
			{
				return pr::v2::make(
					m_xaxis.min() + (point.x - m_plot_area.left  ) * m_xaxis.span() / m_plot_area.width(),
					m_yaxis.min() - (point.y - m_plot_area.bottom) * m_yaxis.span() / m_plot_area.height());
			}

			// Returns a point in client space from a point in graph space.
			// Inverse of PointToGraph
			pr::v2 GraphToPoint(pr::v2 const& gs_point) const
			{
				return pr::v2::make(
					(m_plot_area.left   + (gs_point.x - m_xaxis.min()) * m_plot_area.width()  / m_xaxis.span()),
					(m_plot_area.bottom - (gs_point.y - m_yaxis.min()) * m_plot_area.height() / m_yaxis.span()));
			}

			// Shifts the X and Y range of the graph so that graph space position
			// 'gs_point' is at client space position 'cs_point'
			void PositionGraph(pr::v2 const& cs_point, pr::v2 const& gs_point)
			{
				pr::v2 dst = PointToGraph(cs_point);
				m_xaxis.shift(gs_point.x - dst.x);
				m_yaxis.shift(gs_point.y - dst.y);
				Dirty(true);
			}

			// Get/Set the centre of the graph.
			pr::v2 Centre() const
			{
				return pr::v2::make(
					m_xaxis.min() + m_xaxis.span()*0.5f,
					m_yaxis.min() + m_yaxis.span()*0.5f);
			}
			void Centre(pr::v2 ctr)
			{
				m_xaxis.min(ctr.x - m_xaxis.span()*0.5f);
				m_yaxis.min(ctr.y - m_yaxis.span()*0.5f);
				Dirty(true);
			}

			// Zoom in/out on the graph. Remember to call refresh.
			// Zoom is a floating point value where 1f = no zoom, 2f = 2x magnification
			float Zoom() const
			{
				return 
					m_xaxis.m_allow_zoom ? m_xaxis.span() / m_base_xrange.span() :
					m_yaxis.m_allow_zoom ? m_yaxis.span() / m_base_yrange.span() : 1.0f;
			}
			void Zoom(float zm)
			{
				zm = pr::Clamp(zm, m_zoom.m_min, m_zoom.m_max);
				auto aspect = (m_yaxis.span() * m_base_xrange.span()) / (m_base_yrange.span() * m_xaxis.span());
				aspect = pr::Clamp(pr::IsFinite(aspect) ? aspect : 1.0f, 0.001f, 1000.0f);
				if (m_xaxis.m_allow_zoom) m_xaxis.span(m_base_xrange.span() * zm         );
				if (m_yaxis.m_allow_zoom) m_yaxis.span(m_base_yrange.span() * zm * aspect);
				Dirty(true);
			}

			// Get/Set the Zoom limits
			float ZoomMin() const  { return m_zoom.m_min; }
			void  ZoomMin(float x) { assert(x > 0.0f); m_zoom.m_min = x; }
			float ZoomMax() const  { return m_zoom.m_max; }
			void  ZoomMax(float x) { assert(x > 0.0f); m_zoom.m_max = x; }

			// Get/Set the default ranges for the x/y axes
			AxisRange DefaultXRange() const               { return m_base_xrange; }
			void      DefaultXRange(AxisRange const& rng) { m_base_xrange = rng; }
			AxisRange DefaultYRange() const               { return m_base_yrange; }
			void      DefaultYRange(AxisRange const& rng) { m_base_yrange = rng; }

			// Find the appropriate range for all data in the graph
			// Call ResetToDefaultRange() to zoom the graph to this range
			void FindDefaultRange()
			{
				AxisRange xrng(pr::maths::float_max, -pr::maths::float_max);
				AxisRange yrng(pr::maths::float_max, -pr::maths::float_max);
				for (auto& s : m_series)
				{
					if (m_rdr_cancel)
						break;

					auto& series = *s;
					if (!series.m_opts.m_visible)
						continue;

					size_t imin = 0, imax = 0;
					series.m_values.IndexRange(-pr::maths::float_max, pr::maths::float_max, imin, imax);
					for (size_t i = imin; i != imax; ++i)
					{
						float x,y; series.m_values.Value(i,x,y);
						if (x < xrng.m_min) xrng.m_min = x;
						if (x > xrng.m_max) xrng.m_max = x;
						if (y < yrng.m_min) yrng.m_min = y;
						if (y > yrng.m_max) yrng.m_max = y;
					}
				}
				if (xrng.span() > 0.0f) xrng.span(xrng.span() * 1.05f); else xrng.span(1.0f);
				if (yrng.span() > 0.0f) yrng.span(yrng.span() * 1.05f); else yrng.span(1.0f);
				m_base_xrange = xrng;
				m_base_yrange = yrng;
			}

			// Reset the axis ranges to the default
			// Call FindDefaultRange() first to set the default range
			void ResetToDefaultRange()
			{
				if (!m_xaxis.m_lock_range) { m_xaxis.m_range = m_base_xrange; }
				if (!m_yaxis.m_lock_range) { m_yaxis.m_range = m_base_yrange; }
				Dirty(true);
			}

			// Returns the 'Y' value for a given 'X' value in a series in the graph
			float GetValueAt(int series_index, float x) const
			{
				if (series_index < 0 || series_index >= int(m_series.size())) return 0.0f;
				auto& series = *m_series[series_index];

				size_t imin = 0, imax = 0;
				series.m_values.IndexRange(x, x, imin, imax);
				if (imax - imin == 0) { return 0.0f; }

				// Search for the nearest on the left and right of 'x'
				// (note: not assuming the series is sorted here)
				pr::v2 lhs = -pr::v2Max, rhs =  pr::v2Max;
				for (size_t i = imin; i != imax; ++i)
				{
					pr::v2 tmp; series.m_values.Value(i, tmp.x. tmp.y);
					if (lhs.x < tmp.x && tmp.x < x) lhs = tmp;
					if (rhs.x > tmp.x && tmp.x > x) rhs = tmp;
				}
				if (lhs.x > x && rhs.x < x) return 0.0f;
				if (lhs.x > x) return rhs.y;
				if (rhs.x < x) return lhs.y;
				return pr::Lerp(lhs.y, rhs.y, (x-lhs.x)/(rhs.x-lhs.x));
			}

			// Returns the nearest graph data point, 'gv' to 'pt' given
			// a selection tolerance. 'pt' should be in graph space (use PointToGraph).
			// Returns false if no point is within the selection tolerance
			bool GetValueAt(int series_index, pr::v2 const& pt, pr::v2& gv, int px_tol = 5)
			{
				if (series_index < 0 || series_index >= int(m_series.size())) return false;
				auto& series = *m_series[series_index];

				auto tol = px_tol * m_plot_area.Width / m_xaxis.span();

				size_t imin = 0, imax = 0;
				series.m_values.IndexRange(pt.x - tol, pt.x + tol, imin, imax);
				if (imax - imin == 0)
					return false;

				// Find the closesd within this range of indices
				auto dist_sq = tol * tol;
				for (auto i = imin; i != imax; ++i)
				{
					pr::v2 tmp; series.m_values.Value(i, tmp.x, tmp.y);
					auto d = pr::Length2Sq(tmp - pt);
					if (d < dist_sq) { dist_sq = d; gv = tmp; }
				}
				return dist_sq < tol * tol;
			}

			// Render the graph into a device context (synchronously).
			// To use this to create bitmaps use:
			//   CSize size(100,100);
			//   CDC dc;     dc.CreateCompatibleDC();
			//   CBitmap bm; bm.CreateCompatibleBitmap(dc, size.cx, size.cy);
			//   pr::DCSelect<HBITMAP>(dc, bm, false);
			//   CRect area(POINT(), size);
			//   RenderGraph(dc, area);
			void RenderGraph(HDC hdc, RECT const& graph_area, RECT* plot_area = 0) const
			{
				Gdiplus::Graphics gfx(hdc);
				auto area      = pr::To<Rect>(graph_area);
				auto plot_rect = PlotArea(gfx, area);
				if (plot_area)
					*plot_area = pr::To<RECT>(plot_rect);

				RenderGraphFrame(gfx, area, plot_rect);
				RenderData(gfx, plot_rect);
			}

			// Get the transform from client space to graph space in 'c2g' and the x/y scaling factor in 'scale'.
			// 'c2g' is an out parameter because Gdiplus::Matrix is non-copyable.
			// Note: The returned transform has no scale because lines and points would also be scaled turning them into elipses or caligraphy etc
			void ClientToGraphSpace(Rect const& plot_area, Gdiplus::Matrix& c2g, pr::v2& scale) const
			{
				auto plot = plot_area.Offset(1,1).Inflate(0, 0, -1,-1);
				scale = pr::v2::make(plot.width() / m_xaxis.span(), plot.height() / m_yaxis.span());
				if (!pr::IsFinite(scale.x)) scale.x = scale.x >= 0 ? pr::maths::float_max : -pr::maths::float_max;
				if (!pr::IsFinite(scale.y)) scale.y = scale.y >= 0 ? pr::maths::float_max : -pr::maths::float_max;
				c2g.SetElements(1.0f, 0.0f, 0.0f, -1.0f, plot.left - m_xaxis.min() * scale.x, plot.bottom + m_yaxis.min() * scale.y);
			}
			void ClientToGraphSpace(Gdiplus::Matrix& c2g, pr::v2& scale) const
			{
				return ClientToGraphSpace(m_plot_area, c2g, scale);
			}
			void ClientToGraphSpace(Gdiplus::Matrix& c2g) const
			{
				pr::v2 scale;
				return ClientToGraphSpace(c2g, scale);
			}

			// Called whenever the control is repainted to allow user graphics to be overlayed over the cached bitmap
			// 'sender' is this control
			// 'gfx' is the graphics object to use when adding graphics
			// Rendering is in screen space, use GraphToPoint(),PointToGraph(),ClientToGraphSpace()
			pr::MultiCast<std::function<void(GraphCtrl& sender, Gdiplus::Graphics& gfx)>> AddOverlayOnPaint;

			// Called when the cached graph bitmap is created to allow user graphics to be baked into the cached bitmap.
			// 'sender' is this control
			// 'gfx' is the graphics object to use when adding graphics
			// Rendering is in screen space, use GraphToPoint(),PointToGraph(),ClientToGraphSpace()
			// Note: this method is called in the worker thread context.
			pr::MultiCast<std::function<void(GraphCtrl& sender, Gdiplus::Graphics& gfx)>> AddOverlayOnRender;

		protected:

			// Mark the control as needing a repaint
			bool Dirty() const
			{
				return m_impl_dirty;
			}
			void Dirty(bool dirty)
			{
				if (!m_impl_dirty) Invalidate();
				m_impl_dirty |= dirty;
			}

			void OnWindowPosChange(SizeEventArgs const& args) override
			{
				Dirty(true);
				Control::OnWindowPosChange(args);
			}
			bool OnEraseBkGnd(EmptyArgs const&) override
			{
				return true;
			}
			bool OnPaint(PaintEventArgs const& args) override
			{
				if (args.m_alternate_hdc) { DoPaint(args.m_alternate_hdc); }
				else                      { PaintStruct ps(m_hwnd); DoPaint(ps.hdc); }
				return Control::OnPaint(args);
			}
			void DoPaint(HDC dc)
			{
				auto area = ClientRect();
				MemDC memdc(dc, area);

				Gdiplus::Graphics gfx(memdc); assert(gfx.GetLastStatus() == Gdiplus::Ok && "GDI+ not initialised");
				m_plot_area = PlotArea(gfx, area);

				// If the graph is dirty, begin an asynchronous render of the plot into 'm_tmp'
				if (Dirty())
				{
					m_rdr_cancel = true;
					if (m_rdr_thread.joinable())
						m_rdr_thread.join();
					m_rdr_cancel = false;

					// Make sure the temporary bitmap and the snapshot bitmp are the correct size
					auto plot_size = m_plot_area.size();
					if (m_tmp.size() != plot_size)
						m_tmp.m_bm = std::make_shared<Gdiplus::Bitmap>(plot_size.cx, plot_size.cy);

					m_tmp.m_xrange = m_xaxis.m_range;
					m_tmp.m_yrange = m_yaxis.m_range;

					m_rdr_thread = std::thread([&]{ RenderGraphAsync(); });
					m_impl_dirty = false;
				}

				// In the mean time, compose the graph in 'm_bm' by rendering the frame
				// synchronously and blt'ing the last snapshot into the plot area
				RenderGraphFrame(gfx, area, m_plot_area);

				gfx.SetClip(pr::To<Gdiplus::Rect>(m_plot_area.Offset(1,1).Inflate(0, 0, -1,-1)));
				gfx.SetSmoothingMode(Gdiplus::SmoothingModeHighQuality);
				{
					std::lock_guard<std::mutex> lock(m_mutex_snap);
					if (m_snap.m_bm)
					{
						auto tl = GraphToPoint(pr::v2::make(m_snap.m_xrange.m_min, m_snap.m_yrange.m_max));
						auto br = GraphToPoint(pr::v2::make(m_snap.m_xrange.m_max, m_snap.m_yrange.m_min));
						Rect dst_rect(int(tl.x), int(tl.y), int(br.x), int(br.y));
						Rect src_rect(m_snap.rect());
						gfx.DrawImage(m_snap.m_bm.get(), pr::To<Gdiplus::Rect>(dst_rect), int(src_rect.left), int(src_rect.top), int(src_rect.width()), int(src_rect.height()), Gdiplus::UnitPixel);
					}
				}

				// Allow clients to draw on the graph
				AddOverlayOnPaint.Raise(*this, gfx);

				// Draw the selection rubber band
				if (m_selection.width() != 0 && m_selection.height() != 0)
				{
					auto sel = m_selection.NormalizeRect();
					Gdiplus::Pen pen(m_opts.m_col_select);
					pen.SetDashStyle(Gdiplus::DashStyleDot);
					gfx.DrawRectangle(&pen, pr::To<Gdiplus::Rect>(sel));
				}
				gfx.ResetClip();
			}

			// Plot rendering (done in a background thread). This thread renders the plot into the bitmap in 'm_tmp' using readonly access to the series data.
			void RenderGraphAsync()
			{
				// Hold the rendering CS. Clients should hold this if they want to
				// modify the data while the graph is potentially rendering
				std::lock_guard<std::mutex> lock(m_mutex_rdring);

				// Render the plot into 'm_tmp'
				RenderData(*Gdiplus::Graphics::FromImage(m_tmp.m_bm.get()), m_tmp.rect());

				// If the render was cancelled, ignore the result
				// Otherwise get the main thread to do something with the plot bitmap
				if (!m_rdr_cancel)
				{
					// Update the snapshot
					std::lock_guard<std::mutex> lock(m_mutex_snap);
					std::swap(m_snap, m_tmp);

					// Cause a refresh
					Invalidate();
				}
			}

			// Mouse Navigation
			bool OnMouseButton(MouseEventArgs const& args) override
			{
				Control::OnMouseButton(args);

				if (args.m_down)
				{
					m_selection = Rect(args.m_point, Size());
					if (int(args.m_button & EMouseKey::Left) != 0)
					{
						m_dragging = true;
						m_pt_grab = PointToGraph(pr::To<pr::v2>(args.m_point));
						::SetCursor(m_cur_grab);
						::SetCapture(m_hwnd);
					}
					if (int(args.m_button & EMouseKey::Right) != 0)
					{
						m_selecting = true;
						::SetCapture(m_hwnd);
					}
				}
				else
				{
					if (m_dragging)
					{
						m_dragging = false;
					}
					if (m_selecting)
					{
						m_selecting = false;

						// If the selection has area, rescale the graph
						if (abs(m_selection.width()) != 0 && abs(m_selection.height()) != 0)
						{
							auto sel = m_selection.NormalizeRect();

							// Rescale the graph
							auto lower = PointToGraph(pr::v2::make(float(sel.left), float(sel.bottom)));
							auto upper = PointToGraph(pr::v2::make(float(sel.right), float(sel.top)));
							m_xaxis.min(lower.x);
							m_xaxis.max(upper.x);
							m_yaxis.min(lower.y);
							m_yaxis.max(upper.y);
							Dirty(true);

							// Clear the selection
							m_selection.right  = m_selection.left;
							m_selection.bottom = m_selection.top;
						}
						else
						{
							auto pt = args.m_point;
							::ClientToScreen(m_hwnd, &pt);
							ShowContextMenu(pt);
						}
					}
					::ReleaseCapture();
				}
				return true;
			}
			void OnMouseMove(MouseEventArgs const& args) override
			{
				Control::OnMouseMove(args);
				if (int(args.m_button & EMouseKey::Left) != 0 && m_dragging)
				{
					auto grab_loc = GraphToPoint(m_pt_grab);
					auto dx = args.m_point.x - grab_loc.x;
					auto dy = args.m_point.y - grab_loc.y;
					if (dx*dx + dy*dy >= 25.0f) // must drag at least 5 pixels
						PositionGraph(pr::To<pr::v2>(args.m_point), m_pt_grab);
				}
				if (int(args.m_button & EMouseKey::Right) != 0 && m_selecting)
				{
					int const MinAreaSelectDistance = 3;
					m_selection.right  = args.m_point.x;
					m_selection.bottom = args.m_point.y;
					if (abs(m_selection.width())  < MinAreaSelectDistance) m_selection.right  = m_selection.left;
					if (abs(m_selection.height()) < MinAreaSelectDistance) m_selection.bottom = m_selection.top;
					Invalidate();
				}
				if (m_tt.IsWindowVisible())
				{
					auto pt = PointToGraph(pr::v2::make(float(args.m_point.x), float(args.m_point.y)));
					m_tt.SetTipText(args.m_point.x, args.m_point.y - 40, pr::FmtS(L"%f %f", pt.x, pt.y));
				}
			}
			bool OnMouseWheel(MouseWheelArgs const& args) override
			{
				Control::OnMouseWheel(args);

				auto point = args.m_point;
				::ScreenToClient(m_hwnd, &point);
				if (!m_plot_area.Contains(point))
					return true;

				auto pt = PointToGraph(pr::To<pr::v2>(point));
				int delta = pr::Clamp<short>(args.m_delta, -999, 999);
				Zoom(Zoom() * (1.0f + delta * 0.001f));
				PositionGraph(pr::To<pr::v2>(point), pt);
				Dirty(true);
				return true;
			}

			// Show a right click context menu
			void ShowContextMenu(Point point)
			{
				enum class ECmd
				{
					ShowValues = 1,
					ResetZoom,
					Visible,
					VisibleData,
					VisibleErrorBars,
					PlotType,
					PointSize,
					PointColour,
					LineWidth,
					LineColour,
					BarWidth,
					BarColour,
				};
				int const idx_all = 0xFFFF;

				// Construct the menu
				ContextMenu menu;
				menu.AddItem(std::make_shared<ContextMenu::Label>(L"&Show Values", MAKEWPARAM(ECmd::ShowValues, idx_all), m_tt.IsWindowVisible()));
				menu.AddItem(std::make_shared<ContextMenu::Label>(L"&Reset Zoom", MAKEWPARAM(ECmd::ResetZoom, idx_all)));
				if (!m_series.empty())
				{
					std::vector<std::wstring> plot_types;
					plot_types.push_back(L"Point");
					plot_types.push_back(L"Line");
					plot_types.push_back(L"Bar");

					int vis = 0, invis = 0;
					for (auto& series : m_series)
						(series->m_opts.m_visible ? vis : invis) = 1;

					// All series options
					auto& series_all = menu.AddItem<ContextMenu>(std::make_shared<ContextMenu>(L"Series: All"));
					series_all.AddItem(std::make_shared<ContextMenu::Label>(L"&Visible", MAKEWPARAM(ECmd::Visible, idx_all), vis + invis));

					// Specific series options
					int idx_series = -1;
					for (auto s : m_series)
					{
						auto& series = *s;
						++idx_series;

						// Create a sub menu for this series
						StylePtr style(new ContextMenuStyle());
						style->m_col_text = series.m_opts.color();
						auto& series_m = menu.AddItem<ContextMenu>(std::make_shared<ContextMenu>(series.m_name.c_str(), 0, series.m_opts.m_visible, style));

						// Visibility
						series_m.AddItem(std::make_shared<ContextMenu::Label>(L"&Visible"     ,MAKEWPARAM(ECmd::Visible          ,idx_series), series.m_opts.m_visible));
						series_m.AddItem(std::make_shared<ContextMenu::Label>(L"Series &Data" ,MAKEWPARAM(ECmd::VisibleData      ,idx_series), series.m_opts.m_draw_data));
						series_m.AddItem(std::make_shared<ContextMenu::Label>(L"&Error Bars"  ,MAKEWPARAM(ECmd::VisibleErrorBars ,idx_series), series.m_opts.m_draw_error_bars));

						// Plot Type
						series_m.AddItem(std::make_shared<ContextMenu::Combo>(L"&Plot Type"   ,&plot_types, MAKEWPARAM(ECmd::PlotType ,idx_series)));

						{// Appearance menu
							auto& appearance = series_m.AddItem<ContextMenu>(std::make_shared<ContextMenu>(L"&Appearance"));
							if (series.m_opts.m_plot_type == Series::RdrOptions::EPlotType::Point ||
								series.m_opts.m_plot_type == Series::RdrOptions::EPlotType::Line)
							{
								appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Point Size:", L"9", MAKEWPARAM(ECmd::PointSize, idx_series)));
								appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Point Colour:", L"9", MAKEWPARAM(ECmd::PointColour, idx_series)));
							}
							if (series.m_opts.m_plot_type == Series::RdrOptions::EPlotType::Line)
							{
								appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Line Width:", L"9", MAKEWPARAM(ECmd::LineWidth, idx_series)));
								appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Line Colour:", L"9", MAKEWPARAM(ECmd::LineColour, idx_series)));
							}
							if (series.m_opts.m_plot_type == Series::RdrOptions::EPlotType::Bar)
							{
								appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Bar Width:", L"9", MAKEWPARAM(ECmd::BarWidth, idx_series)));
								appearance.AddItem(std::make_shared<ContextMenu::Edit>(L"Bar Colour:", L"9", MAKEWPARAM(ECmd::BarColour, idx_series)));
							}
						}
					}
				}
				
				int res = menu.Show(m_hwnd, point.x, point.y);
				ECmd cmd = (ECmd)LOWORD(res);
				int  idx = (int)HIWORD(res);
				switch (cmd)
				{
				default: break;
				case ECmd::ShowValues:
					m_tt.ShowWindow(m_tt.IsWindowVisible() ? SW_HIDE : SW_SHOW);
					break;
				case ECmd::ResetZoom:
					ResetToDefaultRange();
					Dirty(true);
					break;
				case ECmd::Visible:
					if (idx == idx_all)
					{
						for (SeriesCont::iterator i = m_series.begin(), iend = m_series.end(); i != iend; ++i)
						{
						}
					}
					break;
				}
				
				//ContextMenu::Label show_values(L"&Show Values", ECmd_ShowValues, 0, m_tt.IsWindowVisible());
				//menu.AddItem(show_values);
				//
				//ContextMenu::Label reset_zoom(L"&Reset Zoom", ECmd_ResetZoom);
				//menu.AddItem(reset_zoom);
				//
				//Series::Menu series_all(L"Series: &All");
				//ContextMenu::Label all_visible(L"&Visible", ECmd_All_Visible);
				//series_all.AddItem(all_visible);
				//if (!m_series.empty()) menu.AddItem(series_all);
				//int invis = 0, vis = 0;
				//for (SeriesCont::const_iterator i = m_series.begin(), iend = m_series.end(); i != iend; ++i) (*i)->m_opts.m_visible ? vis : invis) = 1;
				//all_visible.m_check_state = vis + invis;
				//
				//
				//struct Menu :pr::gui::ContextMenu
				//{
				//	pr::gui::ContextMenuStyle m_style;
				//	pr::gui::ContextMenu::Label m_vis;
				//	
				//	Menu()
				//	:pr::gui::ContextMenu(L"", &m_style)
				//	,m_vis(L"&Visible", ECmd_Series)
				//	{}
				//};
				//
				//
				//for (SeriesCont::iterator i = m_series.begin(), iend = m_series.end(); i != iend; ++i)
				//{
				//	Series& series = **i;
				//	series.m_menu_style
				//	(*i)->m_opts.m_visible ? vis : invis) = 1;
				//
				//	
				//	series.AppendMenu(menu);

				//		series.m_menu.m_label.m_text      = series.m_name;
				//		series.m_menu.m_style.m_col_text  = series.m_opts.color();
				//		series.m_menu.m_vis.m_check_state = series.m_opts.m_visible;
				//		series.m_menu.AddItem(series.m_menu.m_vis);
				//		menu.AddItem(series.m_menu);
				//	}
				//}
				//
			}

			// Returns an area for the plot part of the graph given a bitmap with size 'size'. (i.e. excl titles, axis labels, etc)
			Rect PlotArea(Gdiplus::Graphics const& gfx, Rect const& area) const
			{
				Gdiplus::RectF rect(0.0f, 0.0f, float(area.width()), float(area.height()));

				// Add margins
				rect.X      += m_opts.m_margin_left;
				rect.Y      += m_opts.m_margin_top;
				rect.Width  -= m_opts.m_margin_left + m_opts.m_margin_right;
				rect.Height -= m_opts.m_margin_top  + m_opts.m_margin_bottom;

				// Add space for tick marks
				rect.X      += m_yaxis.m_opts.m_tick_length;
				rect.Width  -= m_yaxis.m_opts.m_tick_length;
				rect.Height -= m_xaxis.m_opts.m_tick_length;

				// Add space for the title and axis labels
				Gdiplus::RectF r;
				if (!m_title.empty())
				{
					gfx.MeasureString(m_title.c_str(), int(m_title.size()), &m_opts.m_font_title, Gdiplus::PointF(), &r);
					rect.Y      += r.Height;
					rect.Height -= r.Height;
				}
				if (!m_xaxis.m_label.empty())
				{
					gfx.MeasureString(m_xaxis.m_label.c_str() ,int(m_xaxis.m_label.size()) ,&m_xaxis.m_opts.m_font_label ,Gdiplus::PointF(), &r);
					rect.Height -= r.Height;
				}
				if (!m_yaxis.m_label.empty())
				{
					gfx.MeasureString(m_yaxis.m_label.c_str() ,int(m_yaxis.m_label.size()) ,&m_yaxis.m_opts.m_font_label ,Gdiplus::PointF(), &r);
					rect.X     += r.Height; // will be rotated by 90°
					rect.Width -= r.Height;
				}

				// Add space for tick labels
				wchar_t const lbl[] = L"9.999";
				int const lbl_len = sizeof(lbl)/sizeof(lbl[0]);

				gfx.MeasureString(lbl, lbl_len, &m_xaxis.m_opts.m_font_tick ,Gdiplus::PointF(), &r);
				rect.Height -= r.Height;

				gfx.MeasureString(lbl, lbl_len, &m_yaxis.m_opts.m_font_tick ,Gdiplus::PointF(), &r);
				rect.X     += r.Width;
				rect.Width -= r.Width;

				return Rect(int(rect.X), int(rect.Y), int(rect.X + rect.Width), int(rect.Y + rect.Height));
			}

			// Return the min, max, and step size for the x/y axes
			void PlotGrid(Rect const& plot_area, pr::v2& min, pr::v2& max, pr::v2& step)
			{
				// Choose step sizes
				float const scale[] = {0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1.0f, 2.0f, 4.0f, 5.0f, 10.0f, 20.0f, 50.0f};
				auto max_ticks_x = plot_area.width()  / m_opts.m_pixels_per_tick.x;
				auto max_ticks_y = plot_area.height() / m_opts.m_pixels_per_tick.y;
				auto xspan = m_xaxis.span();
				auto yspan = m_yaxis.span();
				auto step_x = (float)pow(10.0, (int)log10f(xspan)); step.x = step_x;
				auto step_y = (float)pow(10.0, (int)log10f(yspan)); step.y = step_y;
				for (float const *s = scale, *send = s + sizeof(scale)/sizeof(scale[0]); s != send; ++s)
				{
					if (*s * xspan / step_x <= max_ticks_x) step.x = step_x / *s;
					if (*s * yspan / step_y <= max_ticks_y) step.y = step_y / *s;
				}

				min.x  = (m_xaxis.min() - fmodf(m_xaxis.min(), step.x)) - m_xaxis.min(); if (min.x < 0.0) min.x += step.x;
				min.y  = (m_yaxis.min() - fmodf(m_yaxis.min(), step.y)) - m_yaxis.min(); if (min.y < 0.0) min.y += step.y;
				max.x  = m_xaxis.span() * 1.0001f;
				max.y  = m_yaxis.span() * 1.0001f;

				// protect against increments smaller than can be represented by a float
				if (min.x + step.x == min.x)    step.x = (max.x - min.x) * 0.01f;
				if (min.y + step.y == min.y)    step.y = (max.y - min.y) * 0.01f;

				// protect against too many ticks along the axis
				if (max.x - min.x > step.x*100) step.x = (max.x - min.x) * 0.01f;
				if (max.y - min.y > step.y*100) step.y = (max.y - min.y) * 0.01f;
			}

			// Render the basic graph, axes, title, labels, etc
			void RenderGraphFrame(Gdiplus::Graphics& gfx, Rect const& area, Rect const& plot_area)
			{
				// This is not enforced in the axis.Min/Max accessors because it's useful
				// to be able to change the min/max independently of each other, set them
				// to float max etc. It's only invalid to render a graph with a negative range
				assert(m_xaxis.span() > 0 && "Negative x range");
				assert(m_yaxis.span() > 0 && "Negative y range");

				// Clear to the background colour
				gfx.Clear(m_opts.m_col_bkgd);

				// Draw the graph title and labels
				Gdiplus::RectF str_size;
				if (!m_title.empty())
				{
					Gdiplus::SolidBrush bsh(m_opts.m_col_title);
					gfx.MeasureString(m_title.c_str(), int(m_title.size()), &m_opts.m_font_title, Gdiplus::PointF(), &str_size);
					auto x = float(area.width() - str_size.Width) * 0.5f;
					auto y = float(area.top + m_opts.m_margin_top);
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(&m_opts.m_txfm_title);
					gfx.DrawString(m_title.c_str(), int(m_title.size()), &m_opts.m_font_title, Gdiplus::PointF(), &bsh);
					gfx.ResetTransform();
				}
				if (!m_xaxis.m_label.empty())
				{
					Gdiplus::SolidBrush bsh(m_xaxis.m_opts.m_col_label);
					gfx.MeasureString(m_xaxis.m_label.c_str(), int(m_xaxis.m_label.size()), &m_xaxis.m_opts.m_font_label, Gdiplus::PointF(), &str_size);
					auto x = float(area.width() - str_size.Width) * 0.5f;
					auto y = float(area.bottom - m_opts.m_margin_bottom - str_size.Height);
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(&m_xaxis.m_opts.m_txfm_label);
					gfx.DrawString(m_xaxis.m_label.c_str(), int(m_xaxis.m_label.size()), &m_xaxis.m_opts.m_font_label, Gdiplus::PointF(), &bsh);
					gfx.ResetTransform();
				}
				if (!m_yaxis.m_label.empty())
				{
					Gdiplus::SolidBrush bsh(m_yaxis.m_opts.m_col_label);
					gfx.MeasureString(m_yaxis.m_label.c_str(), int(m_yaxis.m_label.size()), &m_yaxis.m_opts.m_font_label, Gdiplus::PointF(), &str_size);
					auto x = float(area.left + m_opts.m_margin_left);
					auto y = float(area.height() + str_size.Width) * 0.5f;
					gfx.TranslateTransform(x, y);
					gfx.RotateTransform(-90.0f);
					gfx.MultiplyTransform(&m_yaxis.m_opts.m_txfm_label);
					gfx.DrawString(m_yaxis.m_label.c_str(), int(m_yaxis.m_label.size()), &m_yaxis.m_opts.m_font_label, Gdiplus::PointF(), &bsh);
					gfx.ResetTransform();
				}

				{// Draw the graph frame and background
					Gdiplus::SolidBrush bsh_bkgd(m_opts.m_col_bkgd);
					Gdiplus::SolidBrush bsh_axis(m_opts.m_col_axis);
					Gdiplus::Pen        pen_axis(m_opts.m_col_axis, 0.0f);

					// Background
					RenderPlotBkgd(gfx, plot_area);

					pr::v2 min,max,step;
					PlotGrid(plot_area, min, max, step);

					// Tick marks and labels
					Gdiplus::SolidBrush bsh_xtick(m_xaxis.m_opts.m_col_tick);
					Gdiplus::SolidBrush bsh_ytick(m_yaxis.m_opts.m_col_tick);
					auto lblx = float(plot_area.left - m_yaxis.m_opts.m_tick_length - 1);
					auto lbly = float(plot_area.top + plot_area.height() + m_xaxis.m_opts.m_tick_length + 1);
					for (float x = min.x; x < max.x; x += step.x)
					{
						int X = int(plot_area.left + x * plot_area.width() / m_xaxis.span());
						auto s = m_xaxis.m_tick_text(x + m_xaxis.min());
						gfx.MeasureString(s.c_str(), int(s.size()), &m_xaxis.m_opts.m_font_tick, Gdiplus::PointF(), &str_size);
						gfx.DrawString   (s.c_str(), int(s.size()), &m_xaxis.m_opts.m_font_tick, Gdiplus::PointF(X - str_size.Width*0.5f, lbly), &bsh_xtick);
						gfx.DrawLine(&pen_axis, X, plot_area.top + plot_area.height(), X, plot_area.top + plot_area.height() + m_xaxis.m_opts.m_tick_length);
					}
					for (float y = min.y; y < max.y; y += step.y)
					{
						int Y = int(plot_area.top + plot_area.height() - y * plot_area.height() / m_yaxis.span());
						auto s = m_yaxis.m_tick_text(y + m_yaxis.min());
						gfx.MeasureString(s.c_str(), int(s.size()), &m_yaxis.m_opts.m_font_tick, Gdiplus::PointF(), &str_size);
						gfx.DrawString   (s.c_str(), int(s.size()), &m_yaxis.m_opts.m_font_tick, Gdiplus::PointF(lblx - str_size.Width, Y - str_size.Height*0.5f), &bsh_ytick);
						gfx.DrawLine(&pen_axis, plot_area.left - m_yaxis.m_opts.m_tick_length, Y, plot_area.left, Y);
					}

					// Graph border
					gfx.DrawRectangle(&pen_axis, pr::To<Gdiplus::Rect>(plot_area));
				}

				// Control border
				switch (m_opts.m_border)
				{
				default: throw std::exception("");
				case RdrOptions::EBorder::None: break;
				case RdrOptions::EBorder::Single:
					{
						Gdiplus::Pen pen_border(static_cast<Gdiplus::ARGB>(Gdiplus::Color::Black), 0.0f);
						gfx.DrawRectangle(&pen_border, pr::To<Gdiplus::Rect>(area.Inflate(0, 0, -1,-1)));
						break;
					}
				}
			}

			// Render the plot background including gridlines
			void RenderPlotBkgd(Gdiplus::Graphics& gfx, Rect const& plot_area)
			{
				Gdiplus::SolidBrush bsh_plot(m_opts.m_col_plot_bkgd);
				Gdiplus::Pen        pen_grid(m_opts.m_col_grid, 0.0f);

				pr::v2 min,max,step;
				PlotGrid(plot_area, min, max, step);

				// Background
				gfx.FillRectangle(&bsh_plot, pr::To<Gdiplus::Rect>(plot_area));

				// Grid lines
				for (float x = min.x; x < max.x; x += step.x)
				{
					int X = int(plot_area.left + x * plot_area.width() / m_xaxis.span());
					gfx.DrawLine(&pen_grid, X, plot_area.top, X, plot_area.bottom);
				}
				for (float y = min.y; y < max.y; y += step.y)
				{
					int Y = int(plot_area.bottom - y * plot_area.height() / m_yaxis.span());
					gfx.DrawLine(&pen_grid, plot_area.left, Y, plot_area.right, Y);
				}
			}

			// Render the series data into the graph (within 'area')
			void RenderData(Gdiplus::Graphics& gfx, Rect const& plot_area)
			{
				auto plot = plot_area.Offset(1,1).Inflate(0, 0, -1,-1);
				gfx.SetClip(pr::To<Gdiplus::Rect>(plot));

				RenderPlotBkgd(gfx, plot_area);

				// Set the transform so that we can draw directly in graph space.
				// Note: We can't use a scale transform here because the lines and
				// points will also be scaled, into elipses or caligraphy etc
				Gdiplus::Matrix c2g; pr::v2 scale;
				ClientToGraphSpace(m_plot_area, c2g, scale);
				gfx.SetTransform(&c2g);

				// Plot each series
				for (auto s : m_series)
				{
					if (m_rdr_cancel)
						break;

					auto& series = *s;
					auto& opts = series.m_opts;
					if (!opts.m_visible)
						continue;

					Gdiplus::SolidBrush bsh_pt  (opts.m_col_point);
					Gdiplus::SolidBrush bsh_bar (opts.m_col_bar);
					Gdiplus::SolidBrush bsh_err (opts.m_col_error_bars);
					Gdiplus::Pen        pen_line(opts.m_col_line, 0.0f);
					Gdiplus::Pen        pen_bar (opts.m_col_bar, 0.0f);
					auto pt_size   = int(opts.m_point_size);
					auto pt_radius = int(opts.m_point_size * 0.5f);

					// Find the range of indices to consider
					size_t imin = 0, imax = 0;
					series.m_values.IndexRange(m_xaxis.min(), m_xaxis.max(), imin, imax);

					// Loop over data points
					auto curr_gv = pr::v2Zero;
					auto prev_gv = pr::v2Zero;
					auto first = true;
					for (size_t i = imin; i != imax; ++i)
					{
						if (m_rdr_cancel)
							break;

						// Get the data point
						series.m_values.Value(i, curr_gv.x, curr_gv.y);
						int x = int(curr_gv.x * scale.x) , px = int(prev_gv.x * scale.x);
						int y = int(curr_gv.y * scale.y) , py = int(prev_gv.y * scale.y);

						// Render the data point
						switch (opts.m_plot_type)
						{
						// Draw the data point
						case Series::RdrOptions::EPlotType::Point:
							#pragma region Plot Points
							{
								// If the point is a zero and we're not drawing zeros, skip
								if (curr_gv.y == 0.0f && opts.m_draw_zeros != Series::RdrOptions::EPlotZeros::Draw)
									break;

								// Draw error bars is on
								if (opts.m_draw_error_bars)
								{
									float ymin,ymax; series.m_values.ErrValues(i, ymin, ymax);
									int iymin = int(ymin * scale.y), iymax = int(ymax * scale.y);
									if ((iymax - iymin) > 0)
										gfx.FillRectangle(&bsh_err, Gdiplus::Rect(x - pt_radius, iymin, pt_size, iymax - iymin));
								}

								// Plot the data point
								if (opts.m_draw_data)
								{
									// If the point lies on the previous one then don't bother drawing it
									if (x != px || y != py)
										gfx.FillEllipse(&bsh_pt, Gdiplus::Rect(x - pt_radius, y - pt_radius, pt_size, pt_size));
								}
								break;
							}
							#pragma endregion

						// Draw the data point and connect with a line
						case Series::RdrOptions::EPlotType::Line:
							#pragma region Plot_Lines
							{
								// If the point is a zero and we're not drawing zeros, skip
								if (curr_gv.y == 0.0f && opts.m_draw_zeros != Series::RdrOptions::EPlotZeros::Draw)
									break;

								// Draw error bars is on
								if (opts.m_draw_error_bars)
								{
									float ymin,ymax; series.m_values.ErrValues(i, ymin, ymax);
									int iymin = int(ymin * scale.y), iymax = int(ymax * scale.y);
									if ((iymax - iymin) > 0)
										gfx.FillRectangle(&bsh_err, Gdiplus::Rect(x - pt_radius, iymin, pt_size, iymax - iymin));
								}

								// Plot the point and line
								if (opts.m_draw_data)
								{
									// If the point lies on the previous one then don't bother drawing it
									if (x != px || y != py)
									{
										// Draw the line from the previous point (i.e. don't draw if this is the first point)
										if (!first)
											gfx.DrawLine(&pen_line, px, py, x, y);
										
										// Plot the point (if it's size is non-zero)
										if (opts.m_point_size > 0)
											gfx.FillEllipse(&bsh_pt, Gdiplus::Rect(x - pt_radius, y - pt_radius, pt_size, pt_size));
									}
								}
								break;
							}
							#pragma endregion

						// Draw the data as columns in a bar graph
						case Series::RdrOptions::EPlotType::Bar:
							#pragma region Plot_Bars
							{
								// If the point is a zero and we're not drawing zeros, skip
								if (curr_gv.y == 0.0f && opts.m_draw_zeros != Series::RdrOptions::EPlotZeros::Draw)
									break;

								// Calc the left and right side of the bar
								float width_scale = opts.m_bar_width, lhs = 0.0f, rhs = 0.0f;
								auto next_gv = pr::v2Zero; if (i+1 != imax) series.m_values.Value(i+1, next_gv.x, next_gv.y);
								if (i   != imin) { lhs = (curr_gv.x - prev_gv.x) * scale.x * width_scale * 0.5f; }
								if (i+1 != imax) { rhs = (next_gv.x - curr_gv.x) * scale.x * width_scale * 0.5f; }
								if (lhs == 0.0f) lhs = rhs;
								if (rhs == 0.0f) rhs = lhs;

								// Draw error bars is on
								if (opts.m_draw_error_bars)
								{
									float ymin,ymax; series.m_values.ErrValues(i, ymin, ymax);
									int iymin = int(ymin * scale.y), iymax = int(ymax * scale.y);
									if ((iymax - iymin) > 0)
										gfx.FillRectangle(&bsh_err, Gdiplus::Rect(int(x - lhs), iymin, int(rhs + lhs), iymax - iymin));
								}

								// Plot the bar
								if (opts.m_draw_data)
								{
									if      (y < 0) { gfx.FillRectangle(&bsh_bar, Gdiplus::Rect(int(x - lhs), y, int(lhs + rhs), -y)); }
									else if (y > 0) { gfx.FillRectangle(&bsh_bar, Gdiplus::Rect(int(x - lhs), 0, int(lhs + rhs),  y)); }
									else            { gfx.DrawLine(&pen_bar, int(x - lhs), 0, int(lhs + rhs), 0); }
								}
								break;
							}
							#pragma endregion
						}

						//// Add a moving average line
						//if (opts.m_draw_moving_avr)
						//{
						//	// Find the sum of the values around 'i' for the moving average
						//	float sum = 0;
						//	int count = 0;
						//	int avr_i0 = Math.Max(0                   ,i - series.RenderOptions.m_ma_window_size / 2);
						//	int avr_i1 = Math.Min(series.Values.Count ,i + series.RenderOptions.m_ma_window_size / 2);
						//	for (int j = avr_i0; j != avr_i1; ++j)
						//	{
						//		// Don't include zeros if they're not being drawn
						//		if (series.Values[j].m_valueY == 0 && series.RenderOptions.m_draw_zeros != Series.RdrOpts.PlotZeros.Draw) continue;
						//		sum += series.Values[j].m_valueY;
						//		++count;
						//	}
						//	if (count != 0)
						//	{
						//		// Get the data point in screen space
						//		float curr_x = curr_gv     .m_valueX * scale.x;
						//		float prev_x = prev_gv.m_valueX * scale.x;
						//		// Draw a segment of the moving average line
						//		float ma_y = (sum / count) * scale.y;
						//		if (!first && (prev_ma_y != 0 || series.RenderOptions.m_draw_zeros == Series.RdrOpts.PlotZeros.Draw))
						//			gfx.DrawLine(ma_pen, prev_x, prev_ma_y, curr_x, ma_y);
						//		
						//		prev_ma_y = ma_y;
						//	}
						//}

						if (curr_gv.y != 0.0f || opts.m_draw_zeros != Series::RdrOptions::EPlotZeros::Skip)
						{
							prev_gv = curr_gv;
							first = false;
						}
					}
				}
				gfx.ResetTransform();

				// Allow clients to draw on the graph
				AddOverlayOnRender.Raise(*this, gfx);

				gfx.ResetClip();
			}
		};
	}
}

#endif
