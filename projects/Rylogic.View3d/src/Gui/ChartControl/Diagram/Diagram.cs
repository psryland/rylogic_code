using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	public static class Diagram_
	{
		/// <summary>Initialise a chart control to be used for diagrams</summary>
		public static void Init(ChartControl diagram, Options options)
		{
			diagram.Scene.Window.LightProperties = View3d.LightInfo.Ambient(0xFF808080);
			diagram.Options.Orthographic = true;
			diagram.Options.LockAspect = 1.0;
			diagram.Options.AllowElementDragging = options.AllowArrange;

			// Add support for resizing nodes
			//chart.MouseDown -= HandleMouseDown;
			//chart.MouseDown += HandleMouseDown;
			//void HandleMouseDown(object? sender, MouseButtonEventArgs e)
			//{
			//	if (sender is not ChartControl chart) return;
			//	if (!chart.Selected.OfType<Node>().Any(x => x.Selected && x.Style.Resizeable))
			//		return;
			//	
			//	chart.MouseOperations.Pending[MouseButton.Left] = new MouseOpResize(chart);
			//	e.Handled = true;
			//}
		}

		public static void RelinkNodes(ChartControl diagram)
		{
		}

		/// <summary>Diagram Options</summary>
		public class Options :SettingsSet<Options>
		{
			public Options()
			{
				AllowArrange = true;
				NodeMargin = 30.0;
				Scatter = new ScatterOptions();
			}

			/// <summary>Allow nodes in the diagram to be moved</summary>
			public bool AllowArrange
			{
				get => get<bool>(nameof(AllowArrange));
				set => set(nameof(AllowArrange), value);
			}

			/// <summary>The margin between nodes</summary>
			[Description("The gap between nodes")]
			public double NodeMargin
			{
				get => get<double>(nameof(NodeMargin));
				set => set(nameof(NodeMargin), value);
			}

			/// <summary>Node scattering parameters</summary>
			public ScatterOptions Scatter { get; }
			public class ScatterOptions :SettingsSet<ScatterOptions>
			{
				public ScatterOptions()
				{
					MaxIterations = 1000;
					SpringConstant = 0.1f;
					CoulombConstant = 1f;
					FrictionConstant = 0.1f;
					ConnectorScale = 1f;
					Equilibrium = 0.01f;
				}

				[Description("The number of iterations to use when scattering nodes")]
				public int MaxIterations
				{
					get => get<int>(nameof(MaxIterations));
					set => set(nameof(MaxIterations), value);
				}

				[Description("Tuning constant for the attractive force between connected nodes")]
				public float SpringConstant
				{
					get => get<float>(nameof(SpringConstant));
					set => set(nameof(SpringConstant), value);
				}

				[Description("Tuning constant for the repulsive force between nodes")]
				public float CoulombConstant
				{
					get => get<float>(nameof(CoulombConstant));
					set => set(nameof(CoulombConstant), value);
				}

				[Description("Tuning constant for the friction force that slows nodes motion during scattering")]
				public float FrictionConstant
				{
					get => get<float>(nameof(FrictionConstant));
					set => set(nameof(FrictionConstant), value);
				}

				[Description("Tuning constant that multiples the coulomb force proportionally to the number of connections a node has")]
				public float ConnectorScale
				{
					get => get<float>(nameof(ConnectorScale));
					set => set(nameof(ConnectorScale), value);
				}

				[Description("Threshold for node movements that indicates equilibrium has been reached")]
				public float Equilibrium
				{
					get => get<float>(nameof(Equilibrium));
					set => set(nameof(Equilibrium), value);
				}
			}
		}

		/// <summary>Implementation of Diagram context menu commands</summary>
		public class CMenu :IDiagramCMenu
		{
			public CMenu(ChartControl diagram, Options opts)
			{
				Diagram = diagram;
				Options = opts;
				ToggleScattering = Command.Create(diagram, ToggleScatteringInternal);
			}

			/// <summary>The chart that hosts the diagram</summary>
			private ChartControl Diagram { get; }

			/// <summary>The diagram option</summary>
			private Options Options { get; }

			/// <inheritdoc/>
			public bool Scattering
			{
				get => m_timer_scatter != null;
				set
				{
					if (Scattering == value) return;
					if (m_timer_scatter != null)
					{
						m_timer_scatter.Stop();
						m_scatterer = null;
					}
					m_timer_scatter = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, HandleTick, Dispatcher.CurrentDispatcher) : null;
					if (m_timer_scatter != null)
					{
						m_timer_scatter.Start();
					}
					NotifyPropertyChanged(nameof(Scattering));

					// Handler
					void HandleTick(object? sender, EventArgs e)
					{
						// Recreate the scatterer instance whenever the Elements collection changes
						if (m_scatterer == null || m_scatter_elements_issue != Diagram.Elements.IssueNumber)
						{
							var nodes = Diagram.Elements.OfType<Node>().ToList();
							m_scatterer = new ScatterNodesHelper(nodes, Options);
							m_scatter_elements_issue = Diagram.Elements.IssueNumber;
						}

						// On each tick, step the scattering integrator and invalidate the display
						const float step_size = 0.05f;
						m_scatterer.Step(step_size);
						m_scatterer.Apply();

						// Only re-link nodes when not in equilibrium so that they
						// don't jump around while the nodes aren't moving.
						if (!m_scatterer.Equalibrium)
							RelinkNodes(Diagram);

						// Redraw the chart
						Diagram.Invalidate();
					}
				}
			}
			public ICommand ToggleScattering { get; }
			private void ToggleScatteringInternal()
			{
				Scattering = !Scattering;
				Diagram.Invalidate();
			}
			private DispatcherTimer? m_timer_scatter;
			private ScatterNodesHelper? m_scatterer;
			private int m_scatter_elements_issue;










			/// <inheritdoc/>
			public event PropertyChangedEventHandler? PropertyChanged;
			private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>A mouse operation for resizing selected elements</summary>
		public class MouseOpResize :ChartControl.MouseOp
		{
			// The objects being resized
			private readonly Resizee[] m_resizees;
			private class Resizee
			{
				public Resizee(Node node)
				{
					Node = node;
					InitialPosition = node.Position;
					InitialSize = node.Size;
				}
				public Node Node;
				public m4x4 InitialPosition;
				public v4 InitialSize;
			}

			//private readonly Tools.ResizeGrabber m_grabber; // The grabber to use for resizing

			public MouseOpResize(ChartControl chart)//, Tools.ResizeGrabber grabber)
				: base(chart, allow_cancel: true)
			{
				//m_grabber = grabber;

				// Make a collection of the resizeable selected elements and their initial size
				m_resizees = Chart.Selected
					.OfType<Node>()
					.Where(x => x.Style.Resizeable)
					.Select(x => new Resizee(x))
					.ToArray();
			}
			public override void MouseDown(MouseButtonEventArgs? e)
			{
				//m_diag.Cursor = m_grabber.Cursor;
				//foreach (var node in m_resizees)
				//	node.Node.Resizing = true;
			}
			public override void MouseMove(MouseEventArgs e)
			{
				//	// The resize delta in diagram space
				//	var pt = e.GetPosition(Chart.Scene);
				//	var vec_ds = Chart.Camera.SSVecToWSVec(GrabClient, pt);
				//	//var delta = Math_.Dot(vec_ds.xy, m_grabber.Direction);
				//
				//	// Scale all of the resizeable selected elements
				//	foreach (var elem in m_resizees)
				//	{
				//		elem.Elem.PositionXY = elem.InitialPosition + (delta / 2) * m_grabber.Direction;
				//		elem.Elem.Size = new v2(
				//			Math_.Max(10, elem.InitialSize.x + delta * Math.Abs(m_grabber.Direction.x)),
				//			Math_.Max(10, elem.InitialSize.y + delta * Math.Abs(m_grabber.Direction.y)));
				//	}
				//
				//	Chart.Invalidate();
			}
			public override void MouseUp(MouseButtonEventArgs e)
			{
				Chart.Cursor = Cursors.Arrow;
				//	m_resizees.ForEach(x => x.Elem.Resizing = false);
				Chart.Invalidate();
			}
		}

		/// <summary>Helper class for scattering nodes within the diagram</summary>
		private class ScatterNodesHelper
		{
			private readonly Random m_rng;
			private readonly Options m_opts;
			private readonly List<Link> m_springs;
			private readonly List<Link> m_charges;
			private readonly List<Body> m_body;

			public ScatterNodesHelper(IList<Node> nodes, Options opts)
			{
				m_opts = opts;
				m_rng = new Random();
				m_springs = new List<Link>();
				m_charges = new List<Link>();
				m_body = nodes.Select(x => new Body(x)).OrderByDescending(x => x.LinkCount).ToList();

				// Create a dynamics object for each node
				// Order the nodes by the number of connections to other nodes
				// so that highly connected nodes tend to be near the start.
				var lookup = m_body.ToDictionary(x => x.Node, x => x);

				// Create links between all pairs of nodes
				for (int i = 0; i != nodes.Count; ++i)
				{
					var node0 = nodes[i];
					var others = node0.Connectors.Select(x => x.OtherNode(node0)).ToHashSet(0);
					for (int j = i + 1; j != nodes.Count; ++j)
					{
						var node1 = nodes[j];

						// If there is a connector between node0 and node1
						// then the link is a spring, otherwise it's a charge.
						var link = new Link(lookup[node0], lookup[node1]);
						if (others.Contains(node1))
							m_springs.Add(link);
						else
							m_charges.Add(link);
					}
				}
			}

			/// <summary>True when the forces balance</summary>
			public bool Equalibrium { get; private set; }

			/// <summary>Integrate by the given time step</summary>
			public void Step(float dt)
			{
				CalculateForces();
				Integrate(dt);
			}

			/// <summary>Accumulate forces on the bodies</summary>
			private void CalculateForces()
			{
				// Determine spring forces
				foreach (var link in m_springs)
				{
					var body0 = link.Body0;
					var body1 = link.Body1;

					// Find the minimum separation and the current separation
					var vec = Separation(body0, body1);
					var min_sep = MinSeparation(vec, body0, body1);
					var sep = Math.Max(0, vec.Length - min_sep);

					// Spring force F = -Kx
					const float bias = 100f;
					var spring = -m_opts.Scatter.SpringConstant * (sep - bias);

					// Add the forces to each node
					var f = spring * Math_.Normalise(vec);
					body0.Force -= f;
					body1.Force += f;
				}

				// Determine coulomb forces
				foreach (var link in m_charges)
				{
					var body0 = link.Body0;
					var body1 = link.Body1;

					// Find the minimum separation and the current separation
					var vec = Separation(body0, body1);
					var min_sep = MinSeparation(vec, body0, body1);
					var sep = Math.Max(min_sep, vec.Length);

					// Coulomb force F = kQq/r�, assume all 'charges' are 1
					const float charge = 1000f;
					var coulumb = m_opts.Scatter.CoulombConstant * charge * charge / (sep * sep);

					// Add the forces to each node
					var f = coulumb * Math_.Normalise(vec);
					body0.Force -= f;
					body1.Force += f;
				}

				// Friction forces
				foreach (var body in m_body)
				{
					var friction = m_opts.Scatter.FrictionConstant * body.Vel;
					body.Force -= friction;
				}
			}

			/// <summary>Advance the simulation</summary>
			private void Integrate(float dt)
			{
				Equalibrium = true;

				// Apply forces (2nd order integrator)
				const float mass = 1f;
				foreach (var body in m_body)
				{
					var a = body.Force / mass;
					var v = body.Vel + 0.5f * dt * a;

					Equalibrium &= a.LengthSq < Math_.Sqr(m_opts.Scatter.Equilibrium);

					body.Pos += v * dt + 0.5f * a * Math_.Sqr(dt);
					body.Vel += a;
					body.Force = v2.Zero;
				}

				// Ensure the centroid is always at (0,0)
				var centre = v2.Zero;
				foreach (var body in m_body)
					centre += body.Pos;
				centre /= m_body.Count;
				foreach (var body in m_body)
					body.Pos -= centre;
			}

			// Update the nodes with their new positions
			public void Apply()
			{
				// Treat selected nodes as immoveable
				foreach (var body in m_body)
				{
					if (body.Node.Selected)
					{
						body.Vel = v2.Zero;
						body.Pos = body.Node.PositionXY;
					}
					else
					{
						body.Node.PositionXY = body.Pos;
					}
				}
			}

			/// <summary>Return the separation vector between two bodies.</summary>
			private v2 Separation(Body b0, Body b1)
			{
				var vec = b1.Pos - b0.Pos;
				for (; vec.LengthSq < Math_.TinyF;)
					vec = v2.Random2N(m_rng);
				return vec;
			}

			/// <summary>Finds the minimum separation distance between to nodes for a given direction</summary>
			private float MinSeparation(v2 v, Body n0, Body n1)
			{
				var sz = n1.Size + n0.Size;
				return (Math.Abs(v.x / sz.x) > Math.Abs(v.y / sz.y) ? sz.x : sz.y) + (float)m_opts.NodeMargin;
			}

			/// <summary>Wrapper of 'Node'</summary>
			[DebuggerDisplay("{Node}")]
			private class Body
			{
				public Body(Node node)
				{
					Node = node;
					LinkCount = node.Connectors
						.Where(x => !x.Dangling && !x.Loop)
						.Select(x => x.OtherNode(node))
						.Distinct().Count();

					Pos = node.PositionXY;
					Vel = v2.Zero;
					Force = v2.Zero;
				}

				/// <summary>The wrapped node</summary>
				public Node Node;

				/// <summary>The number of unique nodes this node is connected to</summary>
				public int LinkCount;

				/// <summary>The width/height of the node</summary>
				public v2 Size => Node.Size.xy;

				/// <summary>The position of the node during scattering</summary>
				public v2 Pos;

				/// <summary>The velocity of the node during scattering</summary>
				public v2 Vel;

				/// <summary>Scattering force</summary>
				public v2 Force;
			}
			private class Link
			{
				public Link(Body body0, Body body1)
				{
					Body0 = body0;
					Body1 = body1;
				}

				/// <summary>The connected bodies</summary>
				public Body Body0;
				public Body Body1;
			}
		}
	}
}
