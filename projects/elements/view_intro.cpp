#include "elements/stdafx.h"
#include "elements/view_intro.h"
#include "elements/game_instance.h"

using namespace pr::console;

namespace ele
{
	const double seconds_per_page = 20.0;

	ViewIntro::ViewIntro(pr::Console& cons, GameInstance& inst)
		:ViewBase(cons, inst)
		,m_page(-1)
		,m_display_time(seconds_per_page)
	{}

	// Step the view, returns the next view to display
	EView ViewIntro::Step(double elapsed)
	{
		m_display_time += elapsed;
		if (m_display_time < seconds_per_page || (m_display_time = 0) != 0)
			return EView::SameView;
		
		if (++m_page == 4)
			return EView::Home;

		Render();
		return EView::SameView;
	}

	void ViewIntro::Render() const
	{
		Scope s(m_cons);
		m_cons.Clear();

		Pad pad;
		pad.Border(EColour::Blue);
		switch (m_page)
		{
		default:break;
		case 0:
			pad << Colours(EColour::Blue)
				<< "2143-05-03 - UN Low Orbit Solar Observatory:     \n"
				<< Colours(EColour::Green)
				<< "Abnormal energy spike detected in solar output.\n"
				<< "Beta radiation levels appear to have increased.\n"
				<< "Requesting independent verification.";
			break;
		case 1:
			pad << Colours(EColour::Blue)
				<< "2143-05-12 - EU Subterrain Neutrino Detector:     \n"
				<< Colours(EColour::Green)
				<< "Abnormal solar activity confirmed.\n"
				<< "Increasing levels of neutrinos recorded.\n"
				<< "Trend appears to be exponential over observation period.";
			break;
		case 2:
			pad << Colours(EColour::Blue)
				<< "2143-06-02 - Emergency Solar Summit Minutes:    \n"
				<< Colours(EColour::Green)
				<< "Accepted probable cause; star has entered the Red Giant phase\n"
				<< "of it's life-cycle, far earlier than models had predicted. Likely\n"
				<< "outcome is the destruction of inner planets, and ejection of the outer\n"
				<< "solar system\n"
				<< "Estimated time until planetary inhabitability:\n"
				<< pr::datetime::ToCountdownString(m_inst.m_consts.m_start_time_till_nova, pr::datetime::EMaxUnit::Days)
				<< "(± " << long(m_inst.m_consts.m_start_time_till_nova_error_margin / pr::datetime::seconds_p_day) << " days)\n"
				<< "\n"
				<< "Agreed course of action: Evacuation of a representitive human population\n"
				<< "to neighbouring star system\n"
				<< Colours(EColour::Red)
				<< "Required action:\n"
				<< "Immediate focused research into the materials and technologies required to\n"
				<< "build a space craft capable of achieving solar system escape velocity. Primary\n"
				<< "objective is to preserve the lives of as many people as possible";
			break;
		case 3:
			pad << Colours(EColour::Blue)
				<< "2143-06-03\n"
				<< Colours(EColour::Black)
				<< "\n"
				<< " \"Good morning sir, I assume you'll be starting immediately?\" "
				<< "\n";
			break;
		}
		pad.AutoSize();
		pad.Draw(m_cons, EAnchor::Centre);
	}

	void ViewIntro::OnEvent(pr::console::Evt_KeyDown const& e)
	{
		// Check for enter, space, escape to skip
		int vk = e.m_key.wVirtualKeyCode;
		if (vk == VK_SPACE || vk == VK_RETURN || vk == VK_ESCAPE)
		{
			m_display_time += seconds_per_page;
			return;
		}
		ViewBase::HandleKeyEvent(EView::Intro, e);
	}
}
