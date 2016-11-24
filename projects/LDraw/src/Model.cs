using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.common;
using pr.gfx;

namespace LDraw
{
	public class Model :IDisposable
	{
		public Model(MainUI main_ui)
		{
			IncludePaths = new List<string>();
			ContextIds = new List<Guid>();
			Owner = main_ui;
		}
		public void Dispose()
		{ }

		/// <summary>The UI that created this model</summary>
		public MainUI Owner
		{
			get; private set;
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get { return Owner.Settings; }
		}

		/// <summary>The View3d context</summary>
		public View3d View3d
		{
			get { return Owner.View3d; }
		}

		/// <summary>The View3d scene</summary>
		public View3d.Window Window
		{
			[DebuggerStepThrough] get { return m_wnd; }
			set
			{
				if (m_wnd == value) return;
				if (m_wnd != null)
				{
					m_wnd.OnRendering -= HandleSceneRendering;
				}
				m_wnd = value;
				if (m_wnd != null)
				{
					m_wnd.OnRendering += HandleSceneRendering;
				}
			}
		}
		public View3d.Window m_wnd;

		/// <summary>Application include paths</summary>
		public List<string> IncludePaths
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary></summary>
		private List<Guid> ContextIds
		{
			get;
			set;
		}

		/// <summary>Create a new script file</summary>
		public void NewFile(string filepath)
		{
		}

		/// <summary>Add a file source</summary>
		public void OpenFile(string filepath, bool additional)
		{
			// Load a source file and save the context id for that file
			var id = View3d.LoadScriptSource(filepath, additional, async:true, include_paths: IncludePaths.ToArray());
			ContextIds.Add(id);
		}

		/// <summary>Clear the scene</summary>
		public void ClearScene()
		{
			// Remove all objects from the window's drawlist
			Window.RemoveAllObjects();

			// Remove and delete all objects (excluding focus points, selection boxes, etc)
			foreach (var id in ContextIds)
				View3d.DeleteAllObjects(id);

			// Reset the list script source objects
			ContextIds.Clear();
		}

		/// <summary>Add a demo scene to the scene</summary>
		public void CreateDemoScene()
		{
			Window.CreateDemoScene();
		}

		/// <summary>Handle the scene</summary>
		private void HandleSceneRendering(object sender, EventArgs e)
		{
			// Add all objects to the window's drawlist
			foreach (var id in ContextIds)
				Window.AddObjects(id);
		}
	}
}
