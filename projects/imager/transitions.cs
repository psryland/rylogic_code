
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace imager
{
	// A transition should be self contained,
	// From calling 'Do()' it should repeatedly step itself, updating it's models until done.
	// - It will call render as necessary

	// who cleans up the old media? - the transition can't because there's no guarantee it will finish

	// 
	// The caller should recycle transitions (so as to not create models over and over)
	// Transitions must be interruptable (ie. the next transition can start before the previous one has finished)
	// We don't want glitches when a transition ends, so say a transition is always in effect

	/// <summary>Interface to a transition class for managing transitioning from one media file to the next</summary>
	public interface Transition
	{
	}
	
	public class CrossFade :Transition
	{
		/// <summary>The model we're transitioning from</summary>
		private readonly View3D.Object m_model0;
		
		/// <summary>The model we're transitioning to</summary>
		private readonly View3D.Object m_model1;
		
		/// <summary>Swap from 'prev' to 'next'</summary>
		public CrossFade()
		{
			View3D.EditObjectCB edit_cb = EditPhotoCB;
			m_model0 = new View3D.Object("crossfade_model0", 0xFFFFFFFF, 6, 4, edit_cb);
			m_model1 = new View3D.Object("crossfade_model1", 0xFFFFFFFF, 6, 4, edit_cb);
			//view3d.DrawsetAddObject(obj);
		}
		private void EditPhotoCB(int vcount, int icount, View3D.Vertex[] verts, ushort[] indices, ref int new_vcount, ref int new_icount, ref View3D.EPrimType prim_type, ref View3D.Material mat, IntPtr ctx)
		{
			//float w = 0f, h = 0f;
			//IntPtr tex = IntPtr.Zero;
			//if (m_photo != null)
			//{
			//    float aspect = m_photo.m_info.m_width / (float)m_photo.m_info.m_height;
			//    if (aspect >= 1f) { w = 1f ; h = 1f / aspect; } else { w = aspect; h = 1f; }
			//    tex = m_photo.m_handle;
			//}
			//verts[0] = new View3D.Vertex(new v4(-w,  h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.001f, 0.001f));
			//verts[1] = new View3D.Vertex(new v4(-w, -h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.001f, 0.999f));
			//verts[2] = new View3D.Vertex(new v4( w, -h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.999f, 0.999f));
			//verts[3] = new View3D.Vertex(new v4( w,  h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.999f, 0.001f));
			//indices[0] = 3; indices[1] = 0; indices[2] = 1;
			//indices[3] = 1; indices[4] = 2; indices[5] = 3;
			//prim_type = View3D.EPrimType.D3DPT_TRIANGLELIST;
			//mat.m_diff_tex = tex;
		}

		public bool Step()
		{
			return false;
		}
		public void Do()
		{

		}
	}
}
	//{
//    [Flags]
//    public enum ETransitions
//    {
//        None      = 0,
//        Scrolling = 1,
//        Blend     = 2,
//    }

//    /// <summary>Basic transition base class</summary>
//    public class Transition
//    {
//        public static readonly uint AllMask = ~(0xFFFFFFFF << Util.Count(typeof(ETransitions)));

//        /// <summary>Factory create method</summary>
//        public static Transition Create(ETransitions t)
//        {
//            switch (t)
//            {
//            default: return new Transition();
//            case ETransitions.Scrolling: return new Scroll();
//            case ETransitions.Blend: return new Blend();
//            }
//        }

//        /// <summary>Base class for transition data associated with a view</summary>
//        protected abstract class DataBase :IDisposable
//        {
//            public int m_start = Environment.TickCount;
//            public abstract void Dispose();
//        }

//        /// <summary>A map from view control to associated transition data</summary>
//        protected readonly Dictionary<ViewImageControl, DataBase> m_data = new Dictionary<ViewImageControl,DataBase>();

//        /// <summary>Duration of the transition in ms</summary>
//        public int Duration {get; protected set;}

//        /// <summary>Rate that the transition is updated (in ms)</summary>
//        public int Period {get; protected set;}

//        /// <summary>A string description of this transition</summary>
//        public override string ToString() { return "No transition - Simple image swapping."; }

//        /// <summary>Initiate the transition. 'view.Image' is the current image, 'new_image' is the next image</summary>
//        public virtual void Start(ViewImageControl view, Image new_image)
//        {
//            view.Image = new_image;
//        }

//        /// <summary>Start the transition</summary>
//        protected virtual void Start(ViewImageControl view, ViewImageControl.OwnerPaintHandler painter, DataBase data)
//        {
//            if (m_data.ContainsKey(view) && m_data[view] != null) { m_data[view].Dispose(); m_data[view] = null; }
//            view.OwnerPaint = painter;
//            view.RedrawTimer.Interval = Period;
//            view.RedrawTimer.Enabled = true;
//            m_data[view] = data;
//        }

//        /// <summary>Stop the transition</summary>
//        protected virtual void Stop(ViewImageControl view, PaintEventArgs e, Rectangle src_rect, Rectangle dst_rect)
//        {
//            view.OwnerPaint = null;
//            view.RedrawTimer.Enabled = false;
//            view.Render(e.Graphics, src_rect, dst_rect);
//            if (m_data.ContainsKey(view) && m_data[view] != null) { m_data[view].Dispose(); m_data[view] = null; }
//        }

//        /// <summary>Return the fraction (0->1) of the transition duration remaining</summary>
//        protected float RemainingTime(int start_time)
//        {
//            return Math.Min(Environment.TickCount - start_time, Duration) / (float)Duration;
//        }

//        /// <summary>Make a scaled copy of 'img' no greater than x by y pixels</summary>
//        protected static Bitmap ScaledCopy(Image img, int x, int y)
//        {
//            Size sz = img.Size;
//            sz = (sz.Width > sz.Height) ? new Size(x, x*sz.Height/sz.Width) : new Size(y*sz.Width/sz.Height, y);
//            return new Bitmap(img,sz);
//        }

//        /// <summary>Scale a rectangle from 'src' to 'dst'</summary>
//        protected static Rectangle ScaleRect(Rectangle rect, float scalex, float scaley)
//        {
//            rect.X      = (int)(scalex * rect.X);
//            rect.Y      = (int)(scaley * rect.Y);
//            rect.Width  = (int)(scalex * rect.Width);
//            rect.Height = (int)(scaley * rect.Height);
//            return rect;
//        }
//    }

//    /// <summary>A transition that slides pictures into view</summary>
//    public class Scroll :Transition
//    {
//        private class Data :DataBase
//        {
//            public Bitmap m_bm;
//            public Point m_direction;
//            public float m_scalex, m_scaley;
//            public override void Dispose() { if (m_bm != null) {m_bm.Dispose(); m_bm = null;} }
//        }
//        private readonly Random m_rng = new Random(Environment.TickCount);
//        public Scroll()
//        {
//            Duration = 2000;
//            Period = 16;
//        }
//        public override string ToString()
//        {
//            return "Scrolling - Images scroll into view from off screen";
//        }
//        public override void Start(ViewImageControl view, Image new_image)
//        {
//            Size view_sz = view.ClientSize;
//            Bitmap bm = ScaledCopy(new_image, view_sz.Width, view_sz.Height);
			
//            int x,y,ang = m_rng.Next((int)(Math.PI * 2000));
//            double dx = Math.Cos(ang / 1000f);
//            double dy = Math.Sin(ang / 1000f);
//            if (Math.Abs(dx * view_sz.Height) > Math.Abs(dy * view_sz.Width)) { x = dx >= 0.0 ? view_sz.Width  : -view_sz.Width ; y = (int)(x * dy / dx); }
//            else                                                              { y = dy >= 0.0 ? view_sz.Height : -view_sz.Height; x = (int)(y * dx / dy); }

//            Data data = new Data();
//            data.m_direction = new Point(x,y);
//            data.m_bm = bm;
//            data.m_scalex = bm.Width  / (float)new_image.Width;
//            data.m_scaley = bm.Height / (float)new_image.Height;
//            Start(view, Do, data);
//            view.Image = new_image;
//        }
//        private void Do(ViewImageControl view, PaintEventArgs e, Rectangle src_rect, Rectangle dst_rect)
//        {
//            Data data = (Data)m_data[view];
//            if (data == null) { view.Render(e.Graphics, src_rect, dst_rect); return; }

//            // Ok, render it
//            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
//            e.Graphics.InterpolationMode  = InterpolationMode.NearestNeighbor;
//            e.Graphics.SmoothingMode      = SmoothingMode.None;
			
//            // Check the remaining time
//            float t = RemainingTime(data.m_start);
//            dst_rect.Offset((int)(data.m_direction.X * (1f - t)), (int)(data.m_direction.Y * (1f - t)));
//            Rectangle r = ScaleRect(src_rect, data.m_scalex, data.m_scaley);

//            e.Graphics.Clear(view.BackColor);
//            e.Graphics.DrawImage(data.m_bm, dst_rect, r, GraphicsUnit.Pixel);

//            if (t >= 1f) Stop(view, e, src_rect, dst_rect);
//        }
//    }

//    public class Blend :Transition
//    {
//        private class Data :DataBase
//        {
//            public Bitmap m_bm0, m_bm1;
//            public float m_sx0, m_sy0;
//            public float m_sx1, m_sy1;
//            public readonly ColorMatrix m_cm = new ColorMatrix{Matrix00=1, Matrix11=1, Matrix22=1, Matrix33=1, Matrix44=1};
//            public readonly ImageAttributes m_ia = new ImageAttributes();
//            public override void Dispose() { if (m_bm0 != null) {m_bm0.Dispose(); m_bm0=null;} if (m_bm1 != null) {m_bm1.Dispose(); m_bm1=null;} }
//        }
//        public Blend()
//        {
//            Duration = 2000;
//            Period = 16;
//        }
//        public override string ToString()
//        {
//            return "Blend - Fade from one image to the next";
//        }
//        public override void Start(ViewImageControl view, Image new_image)
//        {
//            Size view_sz = view.ClientSize;
//            Data data = new Data();
//            data.m_bm0 = ScaledCopy(view.Image,view_sz.Width,view_sz.Height);
//            data.m_bm1 = ScaledCopy(new_image ,view_sz.Width,view_sz.Height);
//            data.m_sx0 = data.m_bm0.Width  / (float)view.Image.Width;
//            data.m_sy0 = data.m_bm0.Height / (float)view.Image.Height;
//            data.m_sx1 = data.m_bm1.Width  / (float)new_image.Width;
//            data.m_sy1 = data.m_bm1.Height / (float)new_image.Height;
//            base.Start(view, Do, data);
//            view.Image = new_image;
//        }
//        private void Do(ViewImageControl view, PaintEventArgs e, Rectangle src_rect, Rectangle dst_rect)
//        {
//            Data data = (Data)m_data[view];
//            if (data == null) { view.Render(e.Graphics, src_rect, dst_rect); return; }

//            // Ok, render it
//            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
//            e.Graphics.InterpolationMode  = InterpolationMode.NearestNeighbor;
//            e.Graphics.SmoothingMode      = SmoothingMode.None;
//            e.Graphics.CompositingMode    = CompositingMode.SourceOver;

//            float t = RemainingTime(data.m_start);
//            data.m_cm.Matrix33 = t;
//            data.m_ia.SetColorMatrix(data.m_cm);
			
//            Rectangle r0 = ScaleRect(src_rect, data.m_sx0, data.m_sy0);
//            Rectangle r1 = ScaleRect(src_rect, data.m_sx1, data.m_sy1);

//            e.Graphics.Clear(view.BackColor);
//            //e.Graphics.DrawImage(data.m_bm0, dst_rect, r0, GraphicsUnit.Pixel);
//            //e.Graphics.DrawImage(data.m_bm1, dst_rect, r1.X, r1.Y, r1.Width, r1.Height, GraphicsUnit.Pixel, data.m_ia);

//            if (t >= 1f) Stop(view, e, src_rect, dst_rect);
//        }
//    }
//}