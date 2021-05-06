using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	/// <summary>Helper class for scattering nodes within the diagram</summary>
	public sealed class NodeScatterer :IDisposable
	{
		private readonly ChartControl m_diagram;
		private readonly Diagram_.Options m_opts;
		private readonly List<Link> m_springs;
		private readonly List<Link> m_charges;
		private readonly List<Body> m_body;
		private DispatcherTimer m_timer;
		private readonly Random m_rng;
		private int m_issue;

		public NodeScatterer(ChartControl diagram, Diagram_.Options opts)
		{
			m_diagram = diagram;
			m_opts = opts;
			m_rng = new Random();
			m_springs = new List<Link>();
			m_charges = new List<Link>();
			m_body = new List<Body>();
			m_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, Step, Dispatcher.CurrentDispatcher);

			Init();

			// Start the sim
			m_timer.Start();
		}
		public void Dispose()
		{
			m_timer.Stop();
		}

		/// <summary>True when the forces balance</summary>
		public bool Equalibrium { get; private set; }

		/// <summary>Create the bodies, springs, and charges from the nodes in the chart</summary>
		private void Init()
		{
			m_body.Clear();
			m_springs.Clear();
			m_charges.Clear();

			// Order the nodes by the number of connections to other nodes
			// so that highly connected nodes tend to be near the start.
			m_issue = m_diagram.Elements.IssueNumber;
			m_body.AddRange(m_diagram.Elements.OfType<Node>().Select(x => new Body(x)));
			m_body.Sort((l,r) => -l.LinkCount.CompareTo(r.LinkCount));

			// Create a dynamics object for each node
			var lookup = m_body.ToDictionary(x => x.Node, x => x);

			// Create links between all pairs of nodes
			for (int i = 0; i != m_body.Count; ++i)
			{
				var node0 = m_body[i].Node;
				var others = node0.Connectors.Select(x => x.OtherNode(node0)).ToHashSet(0);
				for (int j = i + 1; j != m_body.Count; ++j)
				{
					var node1 = m_body[j].Node;

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

		/// <summary>Step the force simulation</summary>
		private void Step(object? sender, EventArgs e)
		{
			// Recreate the network of bodies, springs, and charges whenever the Elements collection changes
			if (m_issue != m_diagram.Elements.IssueNumber)
				Init();

			// On each tick, step the scattering integrator and invalidate the display
			const float dt = 0.05f;
			CalculateForces();
			Integrate(dt);

			// Apply the changes in body positions to the nodes
			foreach (var body in m_body)
			{
				// Treat selected nodes as immoveable
				if (body.Node.Selected)
				{
					body.Vel = v4.Zero;
					body.Pos = body.Node.O2W.pos;
				}
				else
				{
					body.Node.O2W = m4x4.Translation(body.Pos);
				}
			}

			//// Only re-link nodes when not in equilibrium so that they
			//// don't jump around while the nodes aren't moving.
			//if (!Equalibrium)
			//	Diagram_.RelinkNodes(m_diagram);

			// Redraw the chart
			m_diagram.Invalidate();
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
				// If the simulation blows up, ignore the forces
				var force = Math_.IsFinite(body.Force) || body.Force.Length < 1000.0 ? body.Force : v4.Zero;
				var a = force / mass;
				var v = body.Vel + 0.5f * dt * a;

				Equalibrium &= a.LengthSq < Math_.Sqr(m_opts.Scatter.Equilibrium);

				body.Pos += v * dt + 0.5f * a * Math_.Sqr(dt);
				body.Vel += a;
				body.Force = v4.Zero;
			}

			// Ensure the centroid is always at (0,0,0)
			var centre = v4.Zero;
			foreach (var body in m_body)
				centre += body.Pos.w0;
			centre /= m_body.Count;
			foreach (var body in m_body)
				body.Pos -= centre;
		}

		/// <summary>Return the separation vector between two bodies.</summary>
		private v4 Separation(Body b0, Body b1)
		{
			var vec = b1.Pos - b0.Pos;
			if (vec.LengthSq < Math_.TinyF)
				vec = v4.Random3N(0f, m_rng);

			return m_diagram.Options.NavigationMode switch
			{
				ChartControl.ENavMode.Scene3D => vec,
				ChartControl.ENavMode.Chart2D => new v4(vec.x, vec.y, 0, 0),
				_ => throw new Exception("Unknown chart mode")
			};
		}

		/// <summary>Finds the minimum separation distance between to nodes for a given direction</summary>
		private float MinSeparation(v4 v, Body n0, Body n1)
		{
			// Treat nodes as AABBs. This function needs to be tolerant of overlapping nodes.
			// Returns the minimum distance needed to separate the nodes
			var sz = n1.Size + n0.Size;
			var d = Math_.Abs(Math_.Div(v, sz, v4.TinyF));
			var i = Math_.MaxElementIndex(d.xyz);
			return sz[i] + (float)m_opts.NodeMargin;
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

				Pos = node.O2W.pos;
				Vel = v4.Zero;
				Force = v4.Zero;
			}

			/// <summary>The wrapped node</summary>
			public Node Node;

			/// <summary>The number of unique nodes this node is connected to</summary>
			public int LinkCount;

			/// <summary>The width/height of the node</summary>
			public v4 Size => Node.Size;

			/// <summary>The position of the node during scattering</summary>
			public v4 Pos;

			/// <summary>The velocity of the node during scattering</summary>
			public v4 Vel;

			/// <summary>Scattering force</summary>
			public v4 Force;
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
