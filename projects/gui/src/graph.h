#pragma once

#include "forward.h"
#include "pr/gui/graph_ctrl.h"

namespace pr
{
	namespace gui
	{
		// Application window
		struct GraphUI :Form
		{
			using GraphCtrl = GraphCtrl<>;

			Label m_lbl;
			GraphCtrl m_graph;
			GraphCtrl::Series m_series0;
			GraphCtrl::Series m_series1;

			enum { IDC_BTN1 = 100, IDC_BTN2 };
			GraphUI()
				:Form(MakeFormParams<>().name("GraphUI").title(L"Paul's Awesome Graph Window").main_wnd(true).wh(320,200).wndclass(RegisterWndClass<GraphUI>()))
				,m_lbl  (Label    ::Params<>().parent(this_).name("m_lbl").text(L"hello world").xy(80,20).wh(100,16))
				,m_graph(GraphCtrl::Params<>().parent(this_).name("m_graph").xy(10,40).wh(280,80).anchor(EAnchor::All))
				,m_series0(L"Sin")
				,m_series1(L"Cos")
			{
				float j = 0.0f;
				for (int i = 0; i != 3600; ++i, j += 0.1f)
				{
					m_series0.m_values.push_back(GraphDatum(j, sinf(j/pr::maths::tau)));
					m_series1.m_values.push_back(GraphDatum(j, cosf(j/pr::maths::tau)));
				}
				m_graph.m_series.push_back(&m_series0);
				m_graph.m_series.push_back(&m_series1);

				m_graph.m_opts.Border = GraphCtrl::RdrOptions::EBorder::Single;
				m_graph.FindDefaultRange();
				m_graph.ResetToDefaultRange();
			}
		};
	}
}