using System.Windows;

namespace Rylogic.Gui.WPF.ChartDetail
{
	/// <summary>A UI for setting these rendering properties</summary>
	public class RdrOptionsUI : Window
	{
		//private readonly ChartControl m_chart;
		//private readonly RdrOptions m_opts;
		//
		//public RdrOptionsUI(ChartControl chart, RdrOptions opts)
		//	: base(chart, EPin.Centre, Point.Empty, new Size(500, 400), true)
		//{
		//	m_chart = chart;
		//	m_opts = opts;
		//	ShowIcon = (chart.TopLevelControl as Form)?.ShowIcon ?? false;
		//	Icon = (chart.TopLevelControl as Form)?.Icon;
		//	Text = "Chart Properties";
		//	SetupUI();
		//}
		//private void SetupUI()
		//{
		//	var pg = Controls.Add2(new PropertyGrid
		//	{
		//		SelectedObject = m_opts,
		//		Dock = DockStyle.Fill,
		//	});
		//	pg.PropertyValueChanged += (s, a) =>
		//	{
		//		m_chart.Invalidate();
		//		m_chart.XAxis.GridLineGfx = null;
		//		m_chart.YAxis.GridLineGfx = null;
		//	};
		//}
	}
}
