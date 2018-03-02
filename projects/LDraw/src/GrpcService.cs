using Grpc.Core;
using LDraw.API;
using Rylogic.Extn;
using Rylogic.Graphix;
using Rylogic.Maths;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LDraw
{
	/// <summary>A GRPC service for access to LDraw functionality</summary>
	public class GrpcService : IDisposable
	{
		// Notes:
		//  Threads - The GrpcService is created on the main thread.
		//  RPC calls are handled on worker threads however
		//  
		public GrpcService(Model model)
		{
			Model = model;
			Dispatcher = Dispatcher.CurrentDispatcher;
		}
		public virtual void Dispose()
		{
			Active = false;
			Scene = null;
			Model = null;
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>The scene to add objects to</summary>
		public SceneUI Scene
		{
			get { return m_scene; }
			set
			{
				if (m_scene == value) return;
				if (m_scene != null)
				{

				}
				m_scene = value;
				if (m_scene != null)
				{
				}
			}
		}
		private SceneUI m_scene;

		/// <summary>Enable/Disable the service</summary>
		public bool Active
		{
			get { return m_active != null && !m_active.IsCancellationRequested; }
			set
			{
				if (Active == value) return;
				if (m_active != null)
				{
					m_active.Cancel();
					m_active = null;
					Scene = null;
				}
				m_active = value ? new CancellationTokenSource() : null;
				if (m_active != null)
				{
					if (Model.CurrentScene == null)
						throw new Exception("A scene is required to draw RPC objects in");

					Scene = Model.CurrentScene;
					ActiveConnection(m_active.Token);
					async void ActiveConnection(CancellationToken shutdown)
					{
						await Run(shutdown);
					}
				}
			}
		}
		private CancellationTokenSource m_active;

		/// <summary>The Dispatcher associated with the main thread</summary>
		private Dispatcher Dispatcher;

		/// <summary>Run the service</summary>
		protected async Task Run(CancellationToken shutdown)
		{
			var server = new Server
			{
				Services = { API.LDrawService.BindService(new LDrawService(this)) },
				Ports = { new ServerPort("127.0.0.1", 1976, ServerCredentials.Insecure) },
			};
			using (shutdown.Register(async () => await server.ShutdownAsync()))
			{
				server.Start();
				await server.ShutdownTask;
			}
		}

		/// <summary>LDraw RPC Service</summary>
		public class LDrawService :API.LDrawService.LDrawServiceBase
		{
			private readonly GrpcService m_srv;
			public LDrawService(GrpcService srv)
			{
				m_srv = srv;
			}

			// Windows *************************************

			/// <summary>Return the handle for the current Scene</summary>
			public override Task<WindowCurrentGetReply> WindowCurrentGet(WindowCurrentGetRequest request, ServerCallContext context)
			{
				return InvokeAsync(() =>
				{
					var handle = m_srv.Scene.Window.Handle;
					return new WindowCurrentGetReply { Handle = (ulong)handle };
				});
			}

			/// <summary>Add an object to a scene</summary>
			public override Task<WindowAddObjectReply> WindowAddObject(WindowAddObjectRequest request, ServerCallContext context)
			{
				return InvokeAsync(() =>
				{
					var scene = FindScene((IntPtr)request.WindowHandle);
					if (scene != null)
					{
						var obj = new View3d.Object((IntPtr)request.ObjectHandle);
						scene.Window.AddObject(obj);
					}
					return new WindowAddObjectReply();
				});
			}

			/// <summary>Render the current scene</summary>
			public override Task<WindowRenderReply> WindowRender(WindowRenderRequest request, ServerCallContext context)
			{
				BeginInvoke(() =>
				{
					var scene = FindScene((IntPtr)request.Handle);
					if (scene != null) scene.Invalidate();
				});
				return Task.FromResult(new WindowRenderReply());
			}

			// Objects *************************************

			/// <summary>Create an Ldr object</summary>
			public override Task<ObjectCreateLdrReply> ObjectCreateLdr(ObjectCreateLdrRequest req, ServerCallContext context)
			{
				return InvokeAsync(() =>
				{
					var context_id = req.ContextId.HasValue() ? new Guid(req.ContextId) : (Guid?)null;
					var obj = new View3d.Object(req.LdrScript, req.IsFile, context_id, new View3d.View3DIncludes(req.Includes));
					var result = new ObjectCreateLdrReply { Handle = (ulong)obj.Handle };
					return result;
				});
			}

			/// <summary>Create an instance of an Ldr Object</summary>
			public override Task<ObjectCreateInstanceReply> ObjectCreateInstance(ObjectCreateInstanceRequest request, ServerCallContext context)
			{
				return InvokeAsync(() =>
				{
					var obj = new View3d.Object((IntPtr)request.Handle);
					var nue = obj.CreateInstance();
					var result = new ObjectCreateInstanceReply { Handle = (ulong)nue.Handle };
					return result;
				});
			}

			/// <summary>Delete an Ldr object</summary>
			public override Task<ObjectDeleteReply> ObjectDelete(ObjectDeleteRequest request, ServerCallContext context)
			{
				return InvokeAsync(() =>
				{
					new View3d.Object((IntPtr)request.Handle, true).Dispose();
					return new ObjectDeleteReply();
				});
			}

			/// <summary>Set the object to world transform for an object</summary>
			public override Task<ObjectO2WSetReply> ObjectO2WSet(ObjectO2WSetRequest request, ServerCallContext context)
			{
				BeginInvoke(() =>
				{
					var obj = new View3d.Object((IntPtr)request.Handle, false);
					var o2w = new m4x4(request.O2W.M.ToArray());
					var name = request.Name;
					obj.O2WSet(o2w, name);
				});
				return Task.FromResult(new ObjectO2WSetReply());
			}

			/// <summary>Set the object to parent transform for an object</summary>
			public override Task<ObjectO2PSetReply> ObjectO2PSet(ObjectO2PSetRequest request, ServerCallContext context)
			{
				BeginInvoke(() =>
				{
					var obj = new View3d.Object((IntPtr)request.Handle, false);
					var o2p = new m4x4(request.O2P.M.ToArray());
					var name = request.Name;
					obj.O2PSet(o2p, name);
				});
				return Task.FromResult(new ObjectO2PSetReply());
			}

			// Support *************************************

			#region Support methods

			/// <summary>Helper for marshalling to the main thread</summary>
			private async Task<T> InvokeAsync<T>(Func<T> call)
			{
				return await m_srv.Dispatcher.InvokeAsync(call, DispatcherPriority.Normal, m_srv.m_active.Token);
			}

			/// <summary>BeginInvoke on the main thread</summary>
			private void BeginInvoke(Action call)
			{
				m_srv.Dispatcher.BeginInvoke(call, DispatcherPriority.Normal);
			}

			/// <summary>Find the scene for 'handle'</summary>
			private SceneUI FindScene(IntPtr handle)
			{
				return m_srv.Model.Scenes.FirstOrDefault(x => x.Window.Handle == handle);
			}

			#endregion

		}
	}
}
