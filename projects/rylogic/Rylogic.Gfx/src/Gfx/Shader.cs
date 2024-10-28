﻿//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using Rylogic.Utility;
using HShader = System.IntPtr;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>Texture resource wrapper</summary>
		public sealed class Shader :IDisposable
		{
			// Notes:
			//  - Shaders are not tracked by the renderer, you need to keep a reference to the shader while it's being used.
			//  - A "shader" is all parts of the pipeline, VS, GS, PS, etc

			private readonly bool m_owned;

			/// <summary>Create a texture from an existing texture resource</summary>
			internal Shader(HShader handle, bool owned)
			{
				// Note: 'handle' is not the ID3D11Texture2D handle, it's the internal View3DTexture pointer.
				m_owned = owned;
				Handle = handle;
			}
			public Shader(ShaderOptions options)
			{
				m_owned = true;
				Handle = View3D_ShaderCreate(ref options);
				if (Handle == HShader.Zero)
					throw new Exception($"Failed to create shader");
			}
			public Shader(EStockShader stock_shader, string config)
			{
				m_owned = true;
				Handle = View3D_ShaderCreateStock(stock_shader, config);
				if (Handle == HShader.Zero)
					throw new Exception($"Failed to create shader");
			}

			/// <inheritdoc/>
			public void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
				if (Handle == HShader.Zero) return;
				if (m_owned) View3D_ShaderRelease(Handle);
				Handle = HShader.Zero;
			}

			/// <summary>View3d shader handle</summary>
			public HShader Handle;

			/// <summary>Implicit conversion to handle</summary>
			public static implicit operator HShader(Shader shdr)
			{
				return shdr.Handle;
			}

			#region Equals
			public static bool operator == (Shader? lhs, Shader? rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Shader? lhs, Shader? rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Shader? rhs)
			{
				return rhs != null && Handle == rhs.Handle;
			}
			public override bool Equals(object? rhs)
			{
				return Equals(rhs as Shader);
			}
			public override int GetHashCode()
			{
				return Handle.GetHashCode();
			}
			#endregion
		}
	}
}
