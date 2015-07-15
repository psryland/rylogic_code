#pragma once

#include "forward.h"
#include "pr/gui/graph_ctrl.h"

// Application window
struct GraphUI :Form<GraphUI>
{
	Label m_lbl;
	GraphCtrl<> m_graph;
	GraphCtrl<>::Series m_series0;
	GraphCtrl<>::Series m_series1;

	enum { IDC_BTN1 = 100, IDC_BTN2 };
	GraphUI()
		:Form<GraphUI>(L"Pauls Window", ApplicationMainWindow, CW_USEDEFAULT, CW_USEDEFAULT, 320, 200)
		,m_lbl(L"hello world", 80, 20, 100, 16, -1, this)
		,m_graph(10, 40, 280, 80, -1, this, EAnchor::All)
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

		m_graph.m_opts.Border = GraphCtrl<>::RdrOptions::EBorder::Single;
		m_graph.FindDefaultRange();
		m_graph.ResetToDefaultRange();
	}
};


