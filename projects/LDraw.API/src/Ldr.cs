using System;
using Grpc.Core;
using Rylogic.Maths;
using Rylogic.Utility;
using HContext = System.IntPtr;
using HGizmo = System.IntPtr;
using HMODULE = System.IntPtr;
using HObject = System.IntPtr;
using HTexture = System.IntPtr;
using HWindow = System.IntPtr;
using HWND = System.IntPtr;

namespace LDraw.API
{
	/// <summary>A runtime RPC interface to an LDraw instance</summary>
	public class Ldr :LDrawService.LDrawServiceClient
	{
		// Notes:
		//  This class wraps the Grpc methods used to communicate with LDraw
		//  It provides an API as similar as possible to the View3d API.

		public Ldr(string ip = "127.0.0.1", int port = 1976)
			:base(new Channel($"{ip}:{port}", ChannelCredentials.Insecure))
		{}

		/// <summary>LDraw SceneUI</summary>
		public class Window
		{
			private readonly Ldr m_ldr;

			public Window(Ldr ldr)
			{
				m_ldr = ldr;
				Handle = (HWindow)m_ldr.WindowCurrentGet(new WindowCurrentGetRequest()).Handle;
			}

			/// <summary>The window handle</summary>
			public HWindow Handle;

			/// <summary>Add an object to this scene</summary>
			public void AddObject(Object obj)
			{
				m_ldr.WindowAddObject(new WindowAddObjectRequest { WindowHandle = (ulong)Handle, ObjectHandle = (ulong)obj.Handle });
			}

			/// <summary>Render the current scene</summary>
			public void Render()
			{
				m_ldr.WindowRender(new WindowRenderRequest { Handle = (ulong)Handle });
			}
		}

		/// <summary>LDraw Object</summary>
		public class Object :IDisposable
		{
			private readonly Ldr m_ldr;

			/// <summary>Create an LDraw object from script</summary>
			public Object(Ldr ldr, string ldr_script, bool is_file, Guid? context_id = null, string includes = null)
			{
				m_ldr = ldr;
				var reply = m_ldr.ObjectCreateLdr(new ObjectCreateLdrRequest
				{
					LdrScript = ldr_script,
					IsFile = true,
					ContextId = context_id?.ToString() ?? string.Empty,
					Includes = includes ?? string.Empty
				});
				Handle = (HObject)reply.Handle;
				Owned = true;
			}
			public Object(Ldr ldr, HObject handle, bool owned)
			{
				m_ldr = ldr;
				Handle = handle;
				Owned = owned;
			}
			public virtual void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finaliser thread");
				if (Handle == HObject.Zero) return;
				if (Owned) m_ldr.ObjectDelete(new ObjectDeleteRequest{ Handle = (ulong)Handle });
				Handle = HObject.Zero;
			}

			/// <summary>The object handle</summary>
			public HObject Handle;

			/// <summary>True if the object is destroyed when this instance is disposed</summary>
			public bool Owned;

			/// <summary>Get/Set the object to world transform</summary>
			public m4x4 O2W
			{
				get { return m4x4.Zero; }
				set
				{
					m_ldr.ObjectO2WSet(new ObjectO2WSetRequest{ Handle = (ulong)Handle, O2W = value.ToM4x4() });
				}
			}

			/// <summary>Get/Set the object to parent transform</summary>
			public m4x4 O2P
			{
				get { return m4x4.Zero; }
				set
				{
					m_ldr.ObjectO2WSet(new ObjectO2WSetRequest { Handle = (ulong)Handle, O2W = value.ToM4x4() });
				}
			}

			/// <summary>Create a new Object that shares the model (but not transform) of this object</summary>
			public Object CreateInstance()
			{
				var reply = m_ldr.ObjectCreateInstance(new ObjectCreateInstanceRequest { Handle = (ulong)Handle });
				return new Object(m_ldr, (HObject)reply.Handle, true);
			}
		}
	}

	public static class Extensions
	{
		public static M4x4 ToM4x4(this m4x4 o2w)
		{
			var m = new M4x4();
			m.M.AddRange(o2w.ToArray());
			return m;
		}
		public static Vec4 ToVec4(this v4 vec)
		{
			var v = new Vec4();
			v.M.AddRange(vec.ToArray());
			return v;
		}
	}
}
