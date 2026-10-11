using System;
using System.Runtime.InteropServices;
using Rylogic.Maths;

namespace Rylogic.Gfx;

public sealed partial class View3d
{
	/// <summary>
	/// A grid of detail-normal strengths that varies an object's detail-normal layers over its surface (see Object.DetailStrengthMapSet).
	/// Each texel holds (x, y, z, w): x scales the height of every layer and y adds to the base slope variance, both finite and non-negative;
	/// (z, w) = (cos a, sin a) is a unit vector that turns the layers' pattern by angle 'a' from their u direction toward their v direction,
	/// about the texel's centre. The texture coordinates of a world position 'p' are u = dot(p.xyz, row_u.xyz) + row_u.w and
	/// v = dot(p.xyz, row_v.xyz) + row_v.w, where [0,1] spans the grid. Each fragment blends the layers of the nearest 2x2 texels with bilinear
	/// weights, and positions outside the grid use the nearest edge texels.
	/// Use on the render owner thread while the View3d context is alive.
	/// </summary>
	public sealed class DetailStrengthMap : IDisposable
	{
		/// <summary>Create a 'width' x 'height' grid (each in [1,4096]) of texels (1, 0, 1, 0) with the identity XY projection.</summary>
		public DetailStrengthMap(int width, int height)
		{
			Handle = View3D_DetailStrengthMapCreate(width, height);
			if (Handle == IntPtr.Zero)
				throw new InvalidOperationException("Failed to create the detail strength map.");

			Width = width;
			Height = height;
		}

		/// <summary>The native handle.</summary>
		public IntPtr Handle { get; private set; }

		/// <summary>Grid dimensions.</summary>
		public int Width { get; }
		public int Height { get; }

		/// <summary>Set the world-space projection. Both rows must be finite and their xyz parts must not be parallel.</summary>
		public void Projection(v4 row_u, v4 row_v)
		{
			View3D_DetailStrengthMapProjectionSet(Live, row_u, row_v);
		}

		/// <summary>Copy Width * Height row-major texels to the GPU, visible from the next render.</summary>
		public void Upload(ReadOnlySpan<v4> texels)
		{
			// Pin the span for the duration of the native copy.
			unsafe
			{
				fixed (v4* ptr = texels)
					View3D_DetailStrengthMapUpload(Live, ptr, texels.Length);
			}
		}

		/// <summary>Show the height strength as colour, from blue at 0 through green at 1 to red at 2 or more, instead of the shaded surface.</summary>
		public void Visualise(bool visualise)
		{
			View3D_DetailStrengthMapVisualiseSet(Live, visualise);
		}

		/// <summary>Release this reference; objects that use the map keep their own. Repeated disposal is harmless.</summary>
		public void Dispose()
		{
			View3D_DetailStrengthMapRelease(Handle);
			Handle = IntPtr.Zero;
		}

		/// <summary>The handle, or throw if disposed.</summary>
		private IntPtr Live
		{
			get
			{
				if (Handle == IntPtr.Zero)
					throw new ObjectDisposedException(nameof(DetailStrengthMap));

				return Handle;
			}
		}
	}

	[DllImport(Dll)]
	private static extern IntPtr View3D_DetailStrengthMapCreate(int width, int height);

	[DllImport(Dll)]
	private static extern void View3D_DetailStrengthMapRelease(IntPtr map);

	[DllImport(Dll)]
	private static extern void View3D_DetailStrengthMapProjectionSet(IntPtr map, v4 row_u, v4 row_v);

	[DllImport(Dll)]
	private static extern unsafe void View3D_DetailStrengthMapUpload(IntPtr map, v4* texels, int count);

	[DllImport(Dll)]
	private static extern void View3D_DetailStrengthMapVisualiseSet(IntPtr map, [MarshalAs(UnmanagedType.Bool)] bool visualise);
}
