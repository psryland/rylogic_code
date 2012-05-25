using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using System.Windows.Forms;
using pr.maths;
using Timer = System.Windows.Forms.Timer;

namespace test.test.ui
{
	public partial class MainForm :Form
	{
		private class Gun
		{
			public event Action<Gun> Bang;
			~Gun()              { Debug.WriteLine("Gun collected"); }
			public void Shoot() { Bang.Raise(this); }
		}
		private class Target
		{
			private readonly string m_name;
			public Target(string name) { m_name = name; }
			~Target()                  { Debug.WriteLine(m_name + " collected"); }
			public void OnHit(Gun gun) { Debug.WriteLine(m_name + " hit!"); }
		}

		private readonly properties.Settings m_app_settings;
		private readonly RecentFiles m_recent;
		private readonly HoverScroll m_hoverscroll;
		private readonly Timer       m_clock;
		private bool m_shutdown = false;

		public event Action<int> BooEvent;

		public MainForm()
		{
			InitializeComponent();
			m_clock = new Timer{Interval=1000,Enabled=true};
			
			m_app_settings = new properties.Settings();
			
			{// Recent Files
				m_recent = new RecentFiles(m_menu_file_recent_files, file => MessageBox.Show("Recent File: " + file));
				m_recent.Import(m_app_settings.RecentFiles);
			}
			
			{//Graph Control
				{
					GraphControl.Series series = new GraphControl.Series(); series.Values.Capacity = 10000;
					series.RenderOptions.m_plot_type = GraphControl.Series.RdrOpts.PlotType.Line;
					series.RenderOptions.m_point_size = 0f;
					series.RenderOptions.m_line_colour = Color.Green;
					series.RenderOptions.m_line_width = 1f;
					for (double x = -Math.PI; x <= Math.PI; x += 2*Math.PI/series.Values.Capacity)
						series.Values.Add(new GraphControl.GraphValue(x,Math.Cos(x)));
					m_graph.Data.Add(series);
				}{
					GraphControl.Series series = new GraphControl.Series(); series.Values.Capacity = 10000;
					series.RenderOptions.m_plot_type = GraphControl.Series.RdrOpts.PlotType.Line;
					series.RenderOptions.m_point_size = 0f;
					series.RenderOptions.m_line_colour = Color.Red;
					series.RenderOptions.m_line_width = 1f;
					for (double x = -Math.PI; x <= Math.PI; x += 2*Math.PI/series.Values.Capacity)
						series.Values.Add(new GraphControl.GraphValue(x,Math.Sin(x)));
					m_graph.Data.Add(series);
				}
				m_graph.SetLabels("Test graph", "xaxis", "yaxis");
				m_graph.FindDefaultRange();
				m_graph.ResetToDefaultRange();
	
				m_graph.Notes.Add(new GraphControl.Note("The origin is here", PointF.Empty));
			}

			{// View3D
				m_view3d.FocusPointVisible = true;
				m_view3d.ResetView();
				m_view3d.Settings = m_app_settings.View3DSettings;
				m_view3d.CustomiseContextMenu += delegate (ContextMenuStrip menu)
				{
					menu.Items.Add("Example Script", null, delegate {m_view3d.ShowExampleScript();});
					menu.Items.Add("Paul Was Here");
				};
				//m_view3d.CreateDemoScene();
				
				View3D.Texture texture = null;
				try { texture = new View3D.Texture(@"q:\media\textures\earth.jpg"); } catch {}
				View3D.Texture texture2 = null;
				using (Bitmap bm = new Bitmap(50,50))
				using (Graphics gfx = Graphics.FromImage(bm))
				{
					gfx.Clear(Color.Yellow);
					gfx.DrawString("Paul is Awesome", SystemFonts.CaptionFont, Brushes.Black, PointF.Empty);
					BitmapData data = bm.LockBits(new Rectangle(Point.Empty, bm.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
					try { texture2 = new View3D.Texture(data.Scan0, (uint)(4 * data.Stride * data.Height), (uint)bm.Width, (uint)bm.Height, 1, View3D.EFormat.D3DFMT_A8R8G8B8); } catch {}
					bm.UnlockBits(data);
				}
				
				Rand rng = new Rand(-1.0, 1.0);
				View3D.EditObjectCB edit_cb = (int vcount, int icount, View3D.Vertex[] verts, ushort[] indices, ref int new_vcount, ref int new_icount, ref View3D.EPrimType model_type, ref View3D.Material mat, IntPtr ctx)=>
				{
					float w = rng.Float(0f, 1f), h = rng.Float(0f, 1f);
					m4x4 i2w = new m4x4(v4.Random3N(rng, 0f), rng.Float(0f, Maths.m_tau), v4.Random3(rng, 1f));
					verts[0].m_vert = i2w.p - w*i2w.x - h*i2w.y;
					verts[1].m_vert = i2w.p + w*i2w.x - h*i2w.y;
					verts[2].m_vert = i2w.p + w*i2w.x + h*i2w.y;
					verts[3].m_vert = i2w.p - w*i2w.x + h*i2w.y;
					verts[0].m_norm = verts[1].m_norm = verts[2].m_norm = verts[3].m_norm = i2w.z;
					verts[0].m_col  = verts[1].m_col  = verts[2].m_col  = verts[3].m_col  = 0xFFFFFFFFU;
					verts[0].m_tex = new v2(0,1);
					verts[1].m_tex = new v2(1,1);
					verts[2].m_tex = new v2(1,0);
					verts[3].m_tex = new v2(0,0);
					indices[0] = 0;
					indices[1] = 1;
					indices[2] = 2;
					indices[3] = 0;
					indices[4] = 2;
					indices[5] = 3;
					new_vcount = vcount;
					new_icount = icount;
					model_type = View3D.EPrimType.D3DPT_TRIANGLELIST;
					if (rng.Next(2) == 0 && texture != null)
						mat.m_diff_tex = texture.m_handle;
					else if (texture2 != null)
						mat.m_diff_tex = texture2.m_handle;
				};
				View3D.Object obj = new View3D.Object("myobj", 0xFFFFFFFF, 6, 4, edit_cb);
				m_view3d.DrawsetAddObject(obj);
				
				View3D.Object obj2 = new View3D.Object("*Box b FF00FF00 { 0.3 }", 0, true);
				m_view3d.DrawsetAddObject(obj2);
				
				m_clock.Tick += (s,e)=>
					{
						if (m_shutdown) return;
						obj.Edit(edit_cb);
						obj2.O2P = new m4x4(v4.Random3N(rng, 0f), rng.Float(0f, Maths.m_tau), v4.Origin);
						m_view3d.Refresh();
					};
				
				// Clean up before shutdown
				FormClosing += (s,e)=>
					{
						obj.Dispose();
						if (texture != null) texture.Dispose();
						if (texture2 != null) texture2.Dispose();
					};
			}

			{// View Video Control
				try
				{
					m_view_video_control.Video = new Video(@"d:\deleteme\vid.avi");
					//m_view_video_control.Video = new Video(@"c:\deleteme\audio.mp3");
				}
				catch (COMException ex)
				{
					MessageBox.Show(ex.Message);
				}
			}

			{//Tree Grid View
				m_tree_grid.Columns.Add(new TreeGridColumn{HeaderText="Tree"});
				m_tree_grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Col1"});
				m_tree_grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Col2"});
				m_tree_grid.Columns.Add(new DataGridViewTrackBarColumn{HeaderText="Col3"});
				m_tree_grid.ImageList = m_image_list;
				TreeGridNode node1     = m_tree_grid.Nodes.Add("node1", "bob", Color.Blue, 20); node1.ImageIndex = 0;
				TreeGridNode node1_1   = node1  .Nodes.Add("node1_1", "Bob's child", Color.Red, 40); node1_1.ImageIndex = 1;
				                         node1  .Nodes.Add("node1_3", "A", Color.Yellow, 60);
				                         node1  .Nodes.Insert(1, "node1_2", "da", Color.Green, 30);
				TreeGridNode node1_1_1 = node1_1.Nodes.Add("node1_1_1", "Bob's child child", Color.Purple, 70);
				                         node1_1.Nodes.Add("node1_1_2", "a", Color.OldLace, 90);
				                         node1_1.Nodes.Add("node1_1_3", "a", Color.PowderBlue, 10);
				TreeGridNode node2     = m_tree_grid.Nodes.Add("node2", "alice", Color.Plum, 40);
				                         node2  .Nodes.Add("Fluff", "something", Color.Wheat, 20);
				m_tree_grid.Nodes.Add("node3", "frank", Color.Orange);
			}

			{// Hoverscoll
				m_hoverscroll = new HoverScroll();
				Application.AddMessageFilter(m_hoverscroll);
			}

			{// Progress Form
				Rand r = new Rand();
				m_btn_chore.Click += delegate
				{
					int result = 0;
					string desc = r.Next(2) == 0 ? "" :
						"This is also a very long description\n"+
						"for the purpose of testing the auto size\n"+
						"ability of the form\n";
					string fill = "".PadRight(r.Next(500), '=');
					ProgressForm task = new ProgressForm(
						"Chore",
						"Doing a long winded task\n"+
						desc+fill,
						(s,e)=>
						{
							BackgroundWorker bgw = (BackgroundWorker)s;
							int pc_complete, report_progress = 0;
							for (int i = 0, iend = 10000; i != iend && !(e.Cancel = bgw.CancellationPending); ++i)
							{
								// Report progress
								if ((pc_complete = (i * 100 / iend)) >= report_progress)
									bgw.ReportProgress(report_progress = pc_complete + 1);
								
								result = i;
								Thread.Sleep(1);
							}
						}){FormBorderStyle = FormBorderStyle.Sizable};
					DialogResult res = task.ShowDialog(this);
					MessageBox.Show("result got to " + result + " and the dialog returned " + res);
				};
			}

			{//VT100 terminal
				m_vt100.Output(m_vt100.TestConsoleString0);
				Timer timer = new Timer{Interval=500, Enabled=true};
				timer.Tick +=(s,e)=>{ if (!m_shutdown) m_vt100.Output(m_vt100.TestConsoleString1); };
			}

			{//Dock Container
				//DockForm f0 = new DockForm{Text="Form0",Visible=true};
				//m_dock.Add(f0);
			}

			{// Event extensions
				BooEvent += (i)=> { MessageBox.Show("boo"+i); };
				BooEvent.Suspend();
				BooEvent.Raise(0);
				BooEvent.Raise(1);
				BooEvent.Raise(2);
				BooEvent.Raise(3);
				BooEvent.Resume();

				// ReSharper disable RedundantAssignment
				// Test weak event handlers
				Gun gun = new Gun();
				Target bob = new Target("Bob");
				Target fred = new Target("Fred");
				gun.Bang += new Action<Gun>(bob.OnHit).MakeWeak((h)=>gun.Bang -= h);
				gun.Bang += fred.OnHit;
				gun.Shoot();
				bob = null;
				fred = null;
				GC.Collect(); Thread.Sleep(100); // bob collected here, but not fred
				gun.Shoot(); // fred still shot here
				// ReSharper restore RedundantAssignment
			}
		}
		
		private void OnFileOpen(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (ofd.ShowDialog(this) != DialogResult.OK) return;
			m_recent.Add(ofd.FileName);
		}

		private void OnFormClosing(object sender, FormClosingEventArgs e)
		{
			m_shutdown = true;

			Application.RemoveMessageFilter(m_hoverscroll);
			m_app_settings.RecentFiles = m_recent.Export();
			m_app_settings.View3DSettings = m_view3d.Settings;
			m_app_settings.Save();

			// clean up the video
			Video video = m_view_video_control.Video;
			m_view_video_control.Video = null;
			video.Dispose();
		}

		private void OnExit(object sender, EventArgs e)
		{
			m_clock.Stop();
			Close();
		}
	}
}