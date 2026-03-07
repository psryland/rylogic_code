#pragma once
#include "src/forward.h"

namespace physics_sandbox
{
	// A bottom-docked panel with centred media controls for the physics simulation.
	// Contains Play/Pause/Reset buttons and a time-scale slider for slow-motion control.
	// Exposes Play, Pause, and Reset as event handlers that the parent wires up.
	struct MediaPanel : Panel
	{
		Button m_btn_play;
		Button m_btn_pause;
		Button m_btn_reset;
		Label  m_lbl_speed;  // Shows current time scale, e.g. "Speed: 1.00x"
		HWND   m_slider;     // Win32 trackbar for time scale (no wingui wrapper exists)

		// Slider range: 1..200, mapping to 0.01x..2.00x (position / 100).
		// Default position 100 = real-time (1.00x).
		static constexpr int SliderMin = 1;
		static constexpr int SliderMax = 200;
		static constexpr int SliderDefault = 100;

		// Events fired when buttons are clicked
		pr::EventHandler<MediaPanel&, pr::gui::EmptyArgs const&> OnPlay;
		pr::EventHandler<MediaPanel&, pr::gui::EmptyArgs const&> OnPause;
		pr::EventHandler<MediaPanel&, pr::gui::EmptyArgs const&> OnReset;

		MediaPanel(Panel::Params<> p = Panel::Params<>())
			: Panel(p.dock(EDock::Bottom).wh(Fill, 36).padding(4))
			, m_btn_play(Button::Params<>().parent(this_).text(L"\u25B6 Play").xy(0, 4).wh(80, 26))
			, m_btn_pause(Button::Params<>().parent(this_).text(L"\u23F8 Pause").xy(84, 4).wh(80, 26))
			, m_btn_reset(Button::Params<>().parent(this_).text(L"\u23EE Reset").xy(168, 4).wh(80, 26))
			, m_lbl_speed(Label::Params<>().parent(this_).text(L"Speed: 1.00x").xy(280, 8).wh(90, 20))
			, m_slider(nullptr)
		{
			// Ensure the trackbar common control class is registered.
			// The default InitCtrls() only loads ICC_STANDARD_CLASSES which doesn't
			// include the trackbar. ICC_BAR_CLASSES covers toolbar, statusbar, trackbar.
			INITCOMMONCONTROLSEX icc = { sizeof(icc), ICC_BAR_CLASSES };
			::InitCommonControlsEx(&icc);

			// Centre the button group and slider group when the panel is resized.
			// Layout: [Play][Pause][Reset] --- gap --- [Speed: 1.00x][====slider====]
			// The trackbar is created lazily here because wingui defers HWND creation —
			// m_hwnd is null during the constructor body, so CreateWindowEx would fail.
			WindowPosChange += [&](Control&, WindowPosEventArgs const& args)
			{
				if (args.m_before) return;

				// Create the trackbar on first resize (when our HWND is valid)
				if (m_slider == nullptr && m_hwnd != nullptr)
				{
					m_slider = ::CreateWindowExW(
						0, TRACKBAR_CLASSW, L"",
						WS_CHILD | WS_VISIBLE | TBS_HORZ | TBS_NOTICKS,
						370, 2, 150, 28,
						m_hwnd, nullptr, nullptr, nullptr);
					::SendMessage(m_slider, TBM_SETRANGE, TRUE, MAKELPARAM(SliderMin, SliderMax));
					::SendMessage(m_slider, TBM_SETPOS, TRUE, SliderDefault);
				}

				auto cx = args.m_wp->cx;

				// Button group: 3 buttons × 80px with 4px gaps between them
				auto btn_group_w = 80 + 4 + 80 + 4 + 80; // = 248
				// Slider group: speed label (90px) + gap (4px) + slider (150px)
				auto slider_group_w = 90 + 4 + 150; // = 244
				// Total width with 20px gap between groups
				auto total_w = btn_group_w + 20 + slider_group_w;
				auto left = (cx - total_w) / 2;

				// Reposition buttons to stay centred
				::SetWindowPos(m_btn_play, nullptr, left, 4, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
				::SetWindowPos(m_btn_pause, nullptr, left + 84, 4, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
				::SetWindowPos(m_btn_reset, nullptr, left + 168, 4, 0, 0, SWP_NOSIZE | SWP_NOZORDER);

				// Reposition speed label and slider to the right of the buttons
				auto slider_left = left + btn_group_w + 20;
				::SetWindowPos(m_lbl_speed, nullptr, slider_left, 8, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
				if (m_slider)
					::SetWindowPos(m_slider, nullptr, slider_left + 94, 2, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
			};

			// Wire button clicks to events
			m_btn_play.Click += [&](Button&, pr::gui::EmptyArgs const&)
			{
				OnPlay(*this, pr::gui::EmptyArgs{});
			};
			m_btn_pause.Click += [&](Button&, pr::gui::EmptyArgs const&)
			{
				OnPause(*this, pr::gui::EmptyArgs{});
			};
			m_btn_reset.Click += [&](Button&, pr::gui::EmptyArgs const&)
			{
				OnReset(*this, pr::gui::EmptyArgs{});
			};
		}

		~MediaPanel()
		{
			if (m_slider)
				::DestroyWindow(m_slider);
		}

		// Read the current time scale from the slider.
		// Returns a value in the range [0.01, 2.0], where 1.0 = real-time.
		// Returns 1.0 if the slider hasn't been created yet.
		float TimeScale() const
		{
			if (!m_slider)
				return 1.0f;

			auto pos = static_cast<int>(::SendMessage(m_slider, TBM_GETPOS, 0, 0));
			return pos / 100.0f;
		}

		// Set the slider position programmatically (e.g. for keyboard shortcuts).
		// Clamps the value to the valid range.
		void TimeScale(float scale)
		{
			if (!m_slider)
				return;

			auto pos = Clamp(static_cast<int>(scale * 100.0f), SliderMin, SliderMax);
			::SendMessage(m_slider, TBM_SETPOS, TRUE, pos);
		}

		// Update the speed label to reflect the current slider position.
		// Called periodically from the render loop to keep the label in sync.
		void UpdateSpeedLabel()
		{
			auto scale = TimeScale();
			wchar_t buf[32];
			swprintf_s(buf, L"Speed: %.2fx", scale);
			m_lbl_speed.Text(buf);
		}
	};
}