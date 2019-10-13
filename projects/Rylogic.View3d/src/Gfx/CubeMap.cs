﻿//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using Rylogic.Utility;
using HCubeMap = System.IntPtr;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>CubeMap Texture resource wrapper</summary>
		public sealed class CubeMap :IDisposable
		{
			// Notes:
			//  - CubeMap is basically the same as Texture except it can only
			//    be used for environment mapping.

			private bool m_owned;

			/// <summary>Create a texture from an existing texture resource</summary>
			internal CubeMap(HCubeMap handle, bool owned)
			{
				// Note: 'handle' is not the ID3D11Texture2D handle, it's the internal View3DCubeMap pointer.
				m_owned = owned;
				Handle = handle;

				//if (Handle != IntPtr.Zero)
				//	View3D_TextureGetInfo(Handle, out Info);
				//else
				//	Info = new ImageInfo();
			}

			/// <summary>Construct a texture from a resource (file, embeded resource, or stock asset)</summary>
			public CubeMap(string resource)
				: this(resource, 0, 0, TextureOptions.New())
			{ }
			public CubeMap(string resource, TextureOptions options)
				: this(resource, 0, 0, options)
			{ }
			public CubeMap(string resource, int width, int height)
				: this(resource, width, height, TextureOptions.New())
			{ }
			public CubeMap(string resource, int width, int height, TextureOptions options)
			{
				m_owned = true;
				Handle = View3D_CubeMapCreateFromUri(resource, (uint)width, (uint)height, ref options);
				if (Handle == HCubeMap.Zero) throw new Exception($"Failed to create cube map texture from {resource}");
				//View3D_TextureGetInfo(Handle, out Info);
				View3D_TextureSetFilterAndAddrMode(Handle, options.Filter, options.AddrU, options.AddrV);
			}
			public void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
				if (Handle == HCubeMap.Zero) return;
				if (m_owned) View3D_TextureRelease(Handle);
				Handle = HCubeMap.Zero;
				GC.SuppressFinalize(this);
			}

			/// <summary>View3d cube map texture handle</summary>
			public HCubeMap Handle;

			#region Equals
			public static bool operator ==(CubeMap? lhs, CubeMap? rhs)
			{
				return ReferenceEquals(lhs, rhs) || Equals(lhs, rhs);
			}
			public static bool operator !=(CubeMap? lhs, CubeMap? rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(CubeMap? rhs)
			{
				return rhs != null && Handle == rhs.Handle;
			}
			public override bool Equals(object? rhs)
			{
				return Equals(rhs as CubeMap);
			}
			public override int GetHashCode()
			{
				return Handle.GetHashCode();
			}
			#endregion
		}
	}
}
