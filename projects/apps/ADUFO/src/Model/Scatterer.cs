using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using ADUFO.DomainObjects;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Maths;

namespace ADUFO;

public class Scatterer :IDisposable
{
	public Scatterer(ChartControl diagram, Sliders sliders)
	{
		Diagram = diagram;
		Sliders = sliders;

		Init();

		Active = true;
	}
	public void Dispose()
	{
		Active = false;
	}

	/// <summary>The diagram containing the objects to scatter</summary>
	private ChartControl Diagram { get; }

	/// <summary>Controls for the simulation</summary>
	private Sliders Sliders { get; }

	/// <summary>Jitter source</summary>
	private Random Rng { get; } = new Random();

	/// <summary>Rigid bodies</summary>
	private List<Body> Bodies { get; } = new List<Body>();

	/// <summary></summary>
	private List<Link> Springs { get; } = new List<Link>();

	/// <summary>Enumerates all pairs of bodies that experience a repulsive force between them</summary>
	private IEnumerable<Link> Charges
	{
		get
		{
			if (Bodies.Count < 2) yield break;
			for (int i = 0; i != Bodies.Count - 1; ++i)
				for (int j = i + 1; j != Bodies.Count; ++j)
					yield return new Link(Bodies[i], Bodies[j]);
		}
	}

	/// <summary>Enable/Disable the sim</summary>
	public bool Active
	{
		get => m_timer != null;
		set
		{
			if (Active == value) return;
			if (m_timer != null)
			{
				m_timer.Stop();
			}
			m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, HandleTick, Dispatcher.CurrentDispatcher) : null;
			if (m_timer != null)
			{
				m_timer.Start();
			}
			void HandleTick(object? sender, EventArgs e)
			{
				Step();
			}
		}
	}
	private DispatcherTimer? m_timer;

	/// <summary>Setup the bodies, springs, and charges from the elements in the chart</summary>
	private void Init()
	{
		m_issue = Diagram.Elements.IssueNumber;

		// Create a dynamics object for each node
		var nodes = Diagram.Elements.OfType<WorkItemElement>()
			.Where(x => x.Chart != null)
			.ToDictionary(x => x, x => new Body(x));

		//// Find the unique set of inter-node connections
		//var conns = nodes.Values.SelectMany(x => x.Node.Connectors)
		//	.Where(x => x.Chart != null)                                            // is in the diagram
		//	.Where(x => !x.Dangling)                                                // both ends connected
		//	.Where(x => nodes.ContainsKey(x.Node0!) && nodes.ContainsKey(x.Node1!)) // both ends are in the diagram
		//	.Distinct(Eql<Connector>.From(Connector.HasSameNodes));                 // only one per pair of nodes

		// Cache the nodes and springs
		Bodies.Assign(nodes.Values);
		//m_springs.Assign(conns.Select(x => new Link(nodes[x.Node0!], nodes[x.Node1!])));
	}
	private int m_issue;

	/// <summary>Step the force simulation</summary>
	private void Step()
	{
		// Recreate the network of bodies, springs, and charges whenever the Elements collection changes
		if (m_issue != Diagram.Elements.IssueNumber)
			Init();

		// On each tick, step the scattering integrator and invalidate the display
		const float dt = 0.05f;
		CalculateForces();
		Integrate(dt);

		// Apply the changes in body positions to the nodes
		foreach (var body in Bodies)
		{
			// Treat selected nodes as immoveable
			if (body.Element.Selected)
			{
				body.Vel = v4.Zero;
				body.Pos = body.Element.O2W.pos;
			}
			else
			{
				body.Element.O2W = m4x4.Translation(body.Pos);
			}
		}

		// Redraw the chart
		Diagram.Invalidate();
	}

	/// <summary>Accumulate forces on the bodies</summary>
	private void CalculateForces()
	{
		// This is just a balance between a force that is linear with distance
		// and a force that is quadratic with distance. Since spring simulations
		// easily become unstable, use a modified spring force function.

		// Determine coulomb forces
		foreach (var link in Charges)
		{
			var body0 = link.Body0;
			var body1 = link.Body1;

			// Find the minimum separation and the current separation
			Separation(body0, body1, out var sep, out var min_sep);

			// A coulomb force (F = kQq/r^2) is quadratically proportional to separation distance.
			// To handle nodes of unknown sizes, set all nodes to have the same charge, regardless of size
			// but make the separation equal to the distance between nearest points. Use charge of 1.
			var dist = Math_.Max(sep.Length - min_sep, 1.0);
			var coulumb = Sliders.WorkStream_to_WorkStream_CoulombConstant / (dist * dist);

			// Add the forces to each node
			var f = coulumb * Math_.Normalise(sep, v4.Zero);
			Debug.Assert(Math_.IsFinite(f));
			body0.Force -= f;
			body1.Force += f;
		}

#if false
// Determine spring forces
		foreach (var link in Springs)
		{
			var body0 = link.Body0;
			var body1 = link.Body1;

			// Find the minimum separation and the current separation
			Separation(body0, body1, out var sep, out var min_sep);

			// A spring force (F = -Kx) is linearly proportional to the deviation from the rest length.
			// To handle nodes of unknown sizes, set the rest length to 'min_sep'. To stop the
			// simulation blowing up, make the force zero when less than 'min_sep' and a constant
			// when above some maximum separation.
			var dist = Math_.Clamp(sep.Length - min_sep, -10 * min_sep, 10 * min_sep);
			var spring = -Sliders.Scatter.SpringConstant * dist;

			// Add the forces to each node
			var f = spring * Math_.Normalise(sep, v4.Zero);
			Debug.Assert(Math_.IsFinite(f));
			body0.Force -= f;
			body1.Force += f;
		}
#endif
		// Friction forces
		foreach (var body in Bodies)
		{
			var friction = Sliders.Drag * body.Vel;
			body.Force -= friction;
		}
	}

	/// <summary>Advance the simulation</summary>
	private void Integrate(float dt)
	{
		// Apply forces (2nd order integrator)
		const float mass = 1f;
		foreach (var body in Bodies)
		{
			var a = body.Force / mass;
			var v = body.Vel + 0.5f * dt * a;

			body.Pos += v * dt + 0.5f * a * Math_.Sqr(dt);
			body.Vel += a;
			body.Force = v4.Zero;
		}

		// Ensure the centroid is always at (0,0,0)
		var centre = v4.Zero;
		foreach (var body in Bodies)
			centre += body.Pos.w0;
		centre /= Bodies.Count;
		foreach (var body in Bodies)
			body.Pos -= centre;
	}

	/// <summary>Return the separation vector between the two bodies and the minimum distance in that direction to separate the bodies.</summary>
	private void Separation(Body b0, Body b1, out v4 sep, out float min_dist)
	{
		var vec = b1.Pos - b0.Pos;
		var size = b1.Size + b0.Size;

		// Treat nodes as AABBs. This function needs to be tolerant of overlapping nodes.
		// In 2D, separate the nodes in the camera view plane. In 3D separate the 3d boxes.
		switch (Diagram.Options.NavigationMode)
		{
			case ChartControl.ENavMode.Scene3D:
			{
				// 'pen' is the penetration depth. Positive for penetration.
				var pen = size - Math_.Abs(vec);

				// The separating axis is the axis with the minimum penetration depth.
				var i = Math_.MinElementIndex(pen.xyz);
				if (pen[i] < 0) // Not overlapping
				{
					sep = vec;
					min_dist = size.Length;
				}
				else
				{
					// Push out of penetration first
					sep = v4.Zero;
					sep[i] = vec[i];
					min_dist = size[i];
				}

				// Sep can be zero if the nodes are 2D. The zero-dimension will be the minimum penetration
				if (sep.LengthSq < Math_.TinyF)
					sep = v4.Random3N(0f, Rng);

				break;
			}
			case ChartControl.ENavMode.Chart2D:
			{
				// Assume the AABBs are aligned with the camera (so 'size' does not need rotating)
				sep = Diagram.Camera.W2O * vec;
				sep.z = 0f;

				if (sep.LengthSq < Math_.TinyF)
					sep = v4.Random2N(0f, 0f, Rng);

				// Find the minimum distance along 'sep' needed to separate the bodies
				min_dist = Math_.Dot(0.5f * size.xy, Math_.Abs(sep.xy)) / sep.xy.Length;
				sep = Diagram.Camera.O2W * sep;
				break;
			}
			default:
			{
				throw new Exception("Unsupported navigation mode");
			}
		}
	}
	
	/// <summary>Wrapper of 'Node'</summary>
	private class Body
	{
		public Body(ChartControl.Element element)
		{
			Element = element;
			//LinkCount = element.Connectors
			//	.Where(x => !x.Dangling && !x.Loop)
			//	.Select(x => x.OtherNode(element))
			//	.Distinct().Count();

			Pos = element.O2W.pos;
			Vel = v4.Zero;
			Force = v4.Zero;
		}

		/// <summary>The wrapped node</summary>
		public ChartControl.Element Element;

		///// <summary>The number of unique nodes this node is connected to</summary>
		//public int LinkCount;

		/// <summary>The width/height of the node</summary>
		public v4 Size => Element.Bounds.Radius;

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
