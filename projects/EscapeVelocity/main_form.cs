﻿using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace EscapeVelocity
{
	public partial class MainForm :Form
	{
		private readonly GameInstance m_inst;
		private readonly GraphForm m_graph;
		private int m_game_clock;

		public MainForm()
		{
			InitializeComponent();
	
			m_inst = new GameInstance(0);
			m_graph = new GraphForm();

			//Wire up the UI
			SetupHome();
			SetupChemLab();
			SetupShipDesign();
			SetupGraph();

			// Step up the game loop
			m_game_clock = Environment.TickCount;
			Application.Idle += OnIdle;
			m_graph.Show(this);
		}

		private void SetupHome()
		{
			m_text_time_till_nova.DataBindings.Add(Reflect<TextBox>.MemberName(x => x.Text), m_inst.World, Reflect<WorldState>.MemberName(x => x.TimeTillNova), false, DataSourceUpdateMode.OnPropertyChanged);
		}
		private void SetupChemLab()
		{
			m_grid_elements.AutoGenerateColumns = false;
			m_grid_elements.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			m_grid_elements.DataSource = m_inst.ChemLab.KnownElements;
			m_grid_elements.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "A#",
					DataPropertyName=Reflect<IElementKnown>.MemberName(x=>x.AtomicNumber),
					FillWeight = 1,
				});
			m_grid_elements.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "Name",
					DataPropertyName=Reflect<IElementKnown>.MemberName(x=>x.Fullname),
					FillWeight = 6,
				});
			m_grid_elements.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "Understood",
					DataPropertyName = Reflect<IElementKnown>.MemberName(x => x.PercentUnderstood),
					FillWeight = 2,
				});
			m_grid_elements.SelectionChanged += (s,a) =>
				{
					if (m_grid_elements.SelectedRows.Count == 0) return;
					var row = m_grid_elements.SelectedRows[0];
					m_prop_details.SelectedObject = row.DataBoundItem;
				};

			m_grid_related_mats.AutoGenerateColumns = false;
			m_grid_related_mats.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			m_grid_related_mats.DataSource = m_inst.ChemLab.KnownMaterials;
			m_grid_related_mats.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "Name",
					DataPropertyName = Reflect<IKnownCompound>.MemberName(x => x.Fullname),
					FillWeight = 6,
				});
			m_grid_related_mats.SelectionChanged += (s,a) =>
				{
					if (m_grid_related_mats.SelectedRows.Count == 0) return;
					var row = m_grid_related_mats.SelectedRows[0];
					m_prop_details.SelectedObject = row.DataBoundItem;
				};
		}
		private void SetupShipDesign()
		{
			m_prop_ship_spec.SelectedObject = new ShipSpec();
			m_prop_ship_design.SelectedObject = m_inst.ShipDesign;
		}

		private void SetupGraph()
		{
			var series_electronegativity = new GraphControl.Series("Electronegativity");
			var series_atomic_radius = new GraphControl.Series("Atomic Radius");
			var series_atomic_density = new GraphControl.Series("Solid Density");
			foreach (var e in m_inst.ChemLab.KnownElements.Cast<Element>())
			{
				series_electronegativity.Values.Add(new GraphControl.GraphValue(e.AtomicNumber, e.Electronegativity));
				series_atomic_radius.Values.Add(new GraphControl.GraphValue(e.AtomicNumber, e.ValenceOrbitalRadius));
				series_atomic_density.Values.Add(new GraphControl.GraphValue(e.AtomicNumber, e.SolidDensity));
			}
			m_graph.Graph.Data.Add(series_electronegativity);
			m_graph.Graph.Data.Add(series_atomic_radius);
			m_graph.Graph.Data.Add(series_atomic_density);
			m_graph.Graph.FindDefaultRange();
			m_graph.Graph.ResetToDefaultRange();

		}

		/// <summary>Call when the app is idle</summary>
		private void OnIdle(object sender, EventArgs e)
		{
			Func<bool> IsIdle = () =>
				{
					Message msg;
					return !Win32.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
				};

			while (IsIdle())
			{
				const int ticks_per_frame = 1000;
				int now = Environment.TickCount;
				if (now - m_game_clock < ticks_per_frame)
				{
					Thread.Sleep(0);
					continue;
				}

				double elapsed = (now - m_game_clock) * 0.001;
				m_game_clock = now;
				m_inst.Step(elapsed);
				Refresh();
			}
		}
	}
}