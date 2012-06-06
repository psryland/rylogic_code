using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using pr.common;

namespace RyLogViewer
{
	/// <summary>A vertical scroll bar that shows position within the current file</summary>
	public class FileScroll :Control//VScrollBar
	{
		private int m_minimum;
		private int m_maximum;
		private int m_value;
		private int m_large_change;
		private int m_small_change;

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("LargeChange")]
		public int LargeChange
		{
			get { return m_large_change; }
			set { m_large_change = value; Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("SmallChange")]
		public int SmallChange
		{
			get { return m_small_change; }
			set { m_small_change = value; Invalidate(); }
		}
		
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Minimum")]
		public int Minimum
		{
			get { return m_minimum; }
			set { m_minimum = value; Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Maximum")]
		public int Maximum
		{
			get { return m_maximum; }
			set { m_maximum = value; Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Value")]
		public int Value
		{
			get { return m_value; }
			set { m_value = value; }
		}



	
		public event EventHandler Scroll;
		public event EventHandler ValueChanged;


		/// <summary>The byte range of the file that is visible</summary>
		public Range VisibleRange { get; set; }
		
		/// <summary>The total size of the file</summary>
		public long FileSize { get; set; }
		
		public FileScroll()
		{
			VisibleRange = new Range(25,50);
			FileSize = 100;
			
			// We can only draw a custom scroll bar if the scroll bar renderer is supported
			// Otherwise we'll never see OnPaint messages as the OS does the rendering for common controls
			if (!ScrollBarRenderer.IsSupported)
				Visible = false;
				//SetStyle(ControlStyles.UserPaint
				//    |ControlStyles.OptimizedDoubleBuffer
				//    |ControlStyles.AllPaintingInWmPaint
				//    , true);

			// Can't use VScrollBar.. mouse clicks seem to cause the OS to take over rendering
			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint|
				ControlStyles.ResizeRedraw |
				ControlStyles.Selectable |
				ControlStyles.UserPaint, true);
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			//var g = e.Graphics;
			//var b = e.ClipRectangle;
			//ScrollBarRenderer.DrawLowerVerticalTrack(g, b, ScrollBarState.Normal);
			//ScrollBarRenderer.DrawArrowButton(g, b, ScrollBarArrowButtonState.UpNormal);
			//ScrollBarRenderer.DrawArrowButton(g, b, ScrollBarArrowButtonState.DownNormal);
			//var g = e.Graphics;
			//var b = e.ClipRectangle;
			//ScrollBarRenderer.DrawLowerVerticalTrack(g, b, ScrollBarState.Normal);
			//ScrollBarRenderer.DrawArrowButton(g, b, ScrollBarArrowButtonState.UpNormal);
			//ScrollBarRenderer.DrawArrowButton(g, b, ScrollBarArrowButtonState.DownNormal);
			
			//e.Graphics.var r = new VisualStyleRenderer(VisualStyleElement.ScrollBar..ArrowButton.UpNormal);
			base.OnPaint(e);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
		}
	}
}
/*
 using System;
  2  using System.Collections.Generic;
  3  using System.ComponentModel;
  4  using System.Drawing;
  5  using System.Data;
  6  using System.Text;
  7  using System.Windows.Forms;
  8  using System.Windows.Forms.Design;
  9  using System.Diagnostics;
 10  
 11  
 12  namespace CustomControls {
 13  
 14      [Designer(typeof(ScrollbarControlDesigner))]
 15      public class CustomScrollbar : UserControl {
 16  
 17          protected Color moChannelColor = Color.Empty;
 18          protected Image moUpArrowImage = null;
 19          //protected Image moUpArrowImage_Over = null;
 20          //protected Image moUpArrowImage_Down = null;
 21          protected Image moDownArrowImage = null;
 22          //protected Image moDownArrowImage_Over = null;
 23          //protected Image moDownArrowImage_Down = null;
 24          protected Image moThumbArrowImage = null;
 25  
 26          protected Image moThumbTopImage = null;
 27          protected Image moThumbTopSpanImage = null;
 28          protected Image moThumbBottomImage = null;
 29          protected Image moThumbBottomSpanImage = null;
 30          protected Image moThumbMiddleImage = null;
 31  
 32          protected int moLargeChange = 10;
 33          protected int moSmallChange = 1;
 34          protected int moMinimum = 0;
 35          protected int moMaximum = 100;
 36          protected int moValue = 0;
 37          private int nClickPoint;
 38  
 39          protected int moThumbTop = 0;
 40  
 41          protected bool moAutoSize = false;
 42  
 43          private bool moThumbDown = false;
 44          private bool moThumbDragging = false;
 45  
 46          public new event EventHandler Scroll = null;
 47          public event EventHandler ValueChanged = null;
 48  
 49          private int GetThumbHeight()
 50          {
 51              int nTrackHeight = (this.Height - (UpArrowImage.Height + DownArrowImage.Height));
 52              float fThumbHeight = ((float)LargeChange / (float)Maximum) * nTrackHeight;
 53              int nThumbHeight = (int)fThumbHeight;
 54  
 55              if (nThumbHeight > nTrackHeight)
 56              {
 57                  nThumbHeight = nTrackHeight;
 58                  fThumbHeight = nTrackHeight;
 59              }
 60              if (nThumbHeight < 56)
 61              {
 62                  nThumbHeight = 56;
 63                  fThumbHeight = 56;
 64              }
 65  
 66              return nThumbHeight;
 67          }
 68  
 69          public CustomScrollbar() {
 70  
 71              InitializeComponent();
 72              SetStyle(ControlStyles.ResizeRedraw, true);
 73              SetStyle(ControlStyles.AllPaintingInWmPaint, true);
 74              SetStyle(ControlStyles.DoubleBuffer, true);
 75  
 76              moChannelColor = Color.FromArgb(51, 166, 3);
 77              UpArrowImage = Resource.uparrow;
 78              DownArrowImage = Resource.downarrow;
 79              
 80  
 81              ThumbBottomImage = Resource.ThumbBottom;
 82              ThumbBottomSpanImage = Resource.ThumbSpanBottom;
 83              ThumbTopImage = Resource.ThumbTop;
 84              ThumbTopSpanImage = Resource.ThumbSpanTop;
 85              ThumbMiddleImage = Resource.ThumbMiddle;
 86  
 87              this.Width = UpArrowImage.Width;
 88              base.MinimumSize = new Size(UpArrowImage.Width, UpArrowImage.Height + DownArrowImage.Height + GetThumbHeight());
 89          }
 90  
 91          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("LargeChange")]
 92          public int LargeChange {
 93              get { return moLargeChange; }
 94              set { moLargeChange = value;
 95              Invalidate();
 96              }
 97          }
 98  
 99          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("SmallChange")]
100          public int SmallChange {
101              get { return moSmallChange; }
102              set { moSmallChange = value;
103              Invalidate();    
104              }
105          }
106  
107          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Minimum")]
108          public int Minimum {
109              get { return moMinimum; }
110              set { moMinimum = value;
111              Invalidate();
112              }
113          }
114  
115          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Maximum")]
116          public int Maximum {
117              get { return moMaximum; }
118              set { moMaximum = value;
119              Invalidate();
120              }
121          }
122  
123          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Value")]
124          public int Value {
125              get { return moValue; }
126              set { moValue = value;
127  
128              int nTrackHeight = (this.Height - (UpArrowImage.Height + DownArrowImage.Height));
129              float fThumbHeight = ((float)LargeChange / (float)Maximum) * nTrackHeight;
130              int nThumbHeight = (int)fThumbHeight;
131  
132              if (nThumbHeight > nTrackHeight)
133              {
134                  nThumbHeight = nTrackHeight;
135                  fThumbHeight = nTrackHeight;
136              }
137              if (nThumbHeight < 56)
138              {
139                  nThumbHeight = 56;
140                  fThumbHeight = 56;
141              }
142  
143              //figure out value
144              int nPixelRange = nTrackHeight - nThumbHeight;
145              int nRealRange = (Maximum - Minimum)-LargeChange;
146              float fPerc = 0.0f;
147              if (nRealRange != 0)
148              {
149                  fPerc = (float)moValue / (float)nRealRange;
150                  
151              }
152              
153              float fTop = fPerc * nPixelRange;
154              moThumbTop = (int)fTop;
155              
156  
157              Invalidate();
158              }
159          }
160  
161          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Channel Color")]
162          public Color ChannelColor
163          {
164              get { return moChannelColor; }
165              set { moChannelColor = value; }
166          }
167  
168          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
169          public Image UpArrowImage {
170              get { return moUpArrowImage; }
171              set { moUpArrowImage = value; }
172          }
173  
174          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
175          public Image DownArrowImage {
176              get { return moDownArrowImage; }
177              set { moDownArrowImage = value; }
178          }
179  
180          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
181          public Image ThumbTopImage {
182              get { return moThumbTopImage; }
183              set { moThumbTopImage = value; }
184          }
185  
186          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
187          public Image ThumbTopSpanImage {
188              get { return moThumbTopSpanImage; }
189              set { moThumbTopSpanImage = value; }
190          }
191  
192          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
193          public Image ThumbBottomImage {
194              get { return moThumbBottomImage; }
195              set { moThumbBottomImage = value; }
196          }
197  
198          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
199          public Image ThumbBottomSpanImage {
200              get { return moThumbBottomSpanImage; }
201              set { moThumbBottomSpanImage = value; }
202          }
203  
204          [EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Skin"), Description("Up Arrow Graphic")]
205          public Image ThumbMiddleImage {
206              get { return moThumbMiddleImage; }
207              set { moThumbMiddleImage = value; }
208          }
209  
210          protected override void OnPaint(PaintEventArgs e) {
211  
212              e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
213  
214              if (UpArrowImage != null) {
215                  e.Graphics.DrawImage(UpArrowImage, new Rectangle(new Point(0,0), new Size(this.Width, UpArrowImage.Height)));
216              }
217  
218              Brush oBrush = new SolidBrush(moChannelColor);
219              Brush oWhiteBrush = new SolidBrush(Color.FromArgb(255,255,255));
220              
221              //draw channel left and right border colors
222              e.Graphics.FillRectangle(oWhiteBrush, new Rectangle(0,UpArrowImage.Height, 1, (this.Height-DownArrowImage.Height)));
223              e.Graphics.FillRectangle(oWhiteBrush, new Rectangle(this.Width-1, UpArrowImage.Height, 1, (this.Height - DownArrowImage.Height)));
224              
225              //draw channel
226              e.Graphics.FillRectangle(oBrush, new Rectangle(1, UpArrowImage.Height, this.Width-2, (this.Height-DownArrowImage.Height)));
227  
228              //draw thumb
229              int nTrackHeight = (this.Height - (UpArrowImage.Height + DownArrowImage.Height));
230              float fThumbHeight = ((float)LargeChange / (float)Maximum) * nTrackHeight;
231              int nThumbHeight = (int)fThumbHeight;
232  
233              if (nThumbHeight > nTrackHeight) {
234                  nThumbHeight = nTrackHeight;
235                  fThumbHeight = nTrackHeight;
236              }
237              if (nThumbHeight < 56) {
238                  nThumbHeight = 56;
239                  fThumbHeight = 56;
240              }
241  
242              //Debug.WriteLine(nThumbHeight.ToString());
243  
244              float fSpanHeight = (fThumbHeight - (ThumbMiddleImage.Height + ThumbTopImage.Height + ThumbBottomImage.Height)) / 2.0f;
245              int nSpanHeight = (int)fSpanHeight;
246  
247              int nTop = moThumbTop;
248              nTop += UpArrowImage.Height;
249  
250              //draw top
251              e.Graphics.DrawImage(ThumbTopImage, new Rectangle(1, nTop, this.Width - 2, ThumbTopImage.Height));
252  
253              nTop += ThumbTopImage.Height;
254              //draw top span
255              Rectangle rect = new Rectangle(1, nTop, this.Width - 2, nSpanHeight);
256  
257  
258              e.Graphics.DrawImage(ThumbTopSpanImage, 1.0f,(float)nTop, (float)this.Width-2.0f, (float) fSpanHeight*2);
259  
260              nTop += nSpanHeight;
261              //draw middle
262              e.Graphics.DrawImage(ThumbMiddleImage, new Rectangle(1, nTop, this.Width - 2, ThumbMiddleImage.Height));
263  
264  
265              nTop += ThumbMiddleImage.Height;
266              //draw top span
267              rect = new Rectangle(1, nTop, this.Width - 2, nSpanHeight*2);
268              e.Graphics.DrawImage(ThumbBottomSpanImage, rect);
269  
270              nTop += nSpanHeight;
271              //draw bottom
272              e.Graphics.DrawImage(ThumbBottomImage, new Rectangle(1, nTop, this.Width - 2, nSpanHeight));
273              
274              if (DownArrowImage != null) {
275                  e.Graphics.DrawImage(DownArrowImage, new Rectangle(new Point(0, (this.Height-DownArrowImage.Height)), new Size(this.Width, DownArrowImage.Height)));
276              }
277              
278          }
279  
280          public override bool AutoSize {
281              get {
282                  return base.AutoSize;
283              }
284              set {
285                  base.AutoSize = value;
286                  if (base.AutoSize) {
287                      this.Width = moUpArrowImage.Width;
288                  }
289              }
290          }
291  
292          private void InitializeComponent() {
293              this.SuspendLayout();
294              // 
295              // CustomScrollbar
296              // 
297              this.Name = "CustomScrollbar";
298              this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CustomScrollbar_MouseDown);
299              this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CustomScrollbar_MouseMove);
300              this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CustomScrollbar_MouseUp);
301              this.ResumeLayout(false);
302  
303          }
304  
305          private void CustomScrollbar_MouseDown(object sender, MouseEventArgs e) {
306              Point ptPoint = this.PointToClient(Cursor.Position);
307              int nTrackHeight = (this.Height - (UpArrowImage.Height + DownArrowImage.Height));
308              float fThumbHeight = ((float)LargeChange / (float)Maximum) * nTrackHeight;
309              int nThumbHeight = (int)fThumbHeight;
310  
311              if (nThumbHeight > nTrackHeight) {
312                  nThumbHeight = nTrackHeight;
313                  fThumbHeight = nTrackHeight;
314              }
315              if (nThumbHeight < 56) {
316                  nThumbHeight = 56;
317                  fThumbHeight = 56;
318              }
319  
320              int nTop = moThumbTop;
321              nTop += UpArrowImage.Height;
322  
323  
324              Rectangle thumbrect = new Rectangle(new Point(1, nTop), new Size(ThumbMiddleImage.Width, nThumbHeight));
325              if (thumbrect.Contains(ptPoint))
326              {
327                  
328                  //hit the thumb
329                  nClickPoint = (ptPoint.Y - nTop);
330                  //MessageBox.Show(Convert.ToString((ptPoint.Y - nTop)));
331                  this.moThumbDown = true;
332              }
333  
334              Rectangle uparrowrect = new Rectangle(new Point(1, 0), new Size(UpArrowImage.Width, UpArrowImage.Height));
335              if (uparrowrect.Contains(ptPoint))
336              {
337  
338                  int nRealRange = (Maximum - Minimum)-LargeChange;
339                  int nPixelRange = (nTrackHeight - nThumbHeight);
340                  if (nRealRange > 0)
341                  {
342                      if (nPixelRange > 0)
343                      {
344                          if ((moThumbTop - SmallChange) < 0)
345                              moThumbTop = 0;
346                          else
347                              moThumbTop -= SmallChange;
348  
349                          //figure out value
350                          float fPerc = (float)moThumbTop / (float)nPixelRange;
351                          float fValue = fPerc * (Maximum - LargeChange);
352                          
353                              moValue = (int)fValue;
354                          Debug.WriteLine(moValue.ToString());
355  
356                          if (ValueChanged != null)
357                              ValueChanged(this, new EventArgs());
358  
359                          if (Scroll != null)
360                              Scroll(this, new EventArgs());
361  
362                          Invalidate();
363                      }
364                  }
365              }
366  
367              Rectangle downarrowrect = new Rectangle(new Point(1, UpArrowImage.Height+nTrackHeight), new Size(UpArrowImage.Width, UpArrowImage.Height));
368              if (downarrowrect.Contains(ptPoint))
369              {
370                  int nRealRange = (Maximum - Minimum) - LargeChange;
371                  int nPixelRange = (nTrackHeight - nThumbHeight);
372                  if (nRealRange > 0)
373                  {
374                      if (nPixelRange > 0)
375                      {
376                          if ((moThumbTop + SmallChange) > nPixelRange)
377                              moThumbTop = nPixelRange;
378                          else
379                              moThumbTop += SmallChange;
380  
381                          //figure out value
382                          float fPerc = (float)moThumbTop / (float)nPixelRange;
383                          float fValue = fPerc * (Maximum-LargeChange);
384                         
385                              moValue = (int)fValue;
386                          Debug.WriteLine(moValue.ToString());
387  
388                          if (ValueChanged != null)
389                              ValueChanged(this, new EventArgs());
390  
391                          if (Scroll != null)
392                              Scroll(this, new EventArgs());
393  
394                          Invalidate();
395                      }
396                  }
397              }
398          }
399  
400          private void CustomScrollbar_MouseUp(object sender, MouseEventArgs e) {
401              this.moThumbDown = false;
402              this.moThumbDragging = false;
403          }
404  
405          private void MoveThumb(int y) {
406              int nRealRange = Maximum - Minimum;
407              int nTrackHeight = (this.Height - (UpArrowImage.Height + DownArrowImage.Height));
408              float fThumbHeight = ((float)LargeChange / (float)Maximum) * nTrackHeight;
409              int nThumbHeight = (int)fThumbHeight;
410  
411              if (nThumbHeight > nTrackHeight) {
412                  nThumbHeight = nTrackHeight;
413                  fThumbHeight = nTrackHeight;
414              }
415              if (nThumbHeight < 56) {
416                  nThumbHeight = 56;
417                  fThumbHeight = 56;
418              }
419  
420              int nSpot = nClickPoint;
421  
422              int nPixelRange = (nTrackHeight - nThumbHeight);
423              if (moThumbDown && nRealRange > 0) {
424                  if (nPixelRange > 0) {
425                      int nNewThumbTop = y - (UpArrowImage.Height+nSpot);
426                      
427                      if(nNewThumbTop<0)
428                      {
429                          moThumbTop = nNewThumbTop = 0;
430                      }
431                      else if(nNewThumbTop > nPixelRange)
432                      {
433                          moThumbTop = nNewThumbTop = nPixelRange;
434                      }
435                      else {
436                          moThumbTop = y - (UpArrowImage.Height + nSpot);
437                      }
438                     
439                      //figure out value
440                      float fPerc = (float)moThumbTop / (float)nPixelRange;
441                      float fValue = fPerc * (Maximum-LargeChange);
442                      moValue = (int)fValue;
443                      Debug.WriteLine(moValue.ToString());
444  
445                      Application.DoEvents();
446  
447                      Invalidate();
448                  }
449              }
450          }
451  
452          private void CustomScrollbar_MouseMove(object sender, MouseEventArgs e) {
453              if(moThumbDown == true)
454              {
455                  this.moThumbDragging = true;
456              }
457  
458              if (this.moThumbDragging) {
459  
460                  MoveThumb(e.Y);
461              }
462  
463              if(ValueChanged != null)
464                  ValueChanged(this, new EventArgs());
465  
466              if(Scroll != null)
467                  Scroll(this, new EventArgs());
468          }
469  
470      }
471  
472      internal class ScrollbarControlDesigner : System.Windows.Forms.Design.ControlDesigner {
473  
474          
475  
476          public override SelectionRules SelectionRules {
477              get {
478                  SelectionRules selectionRules = base.SelectionRules;
479                  PropertyDescriptor propDescriptor = TypeDescriptor.GetProperties(this.Component)["AutoSize"];
480                  if (propDescriptor != null) {
481                      bool autoSize = (bool)propDescriptor.GetValue(this.Component);
482                      if (autoSize) {
483                          selectionRules = SelectionRules.Visible | SelectionRules.Moveable | SelectionRules.BottomSizeable | SelectionRules.TopSizeable;
484                      }
485                      else {
486                          selectionRules = SelectionRules.Visible | SelectionRules.AllSizeable | SelectionRules.Moveable;
487                      }
488                  }
489                  return selectionRules;
490              }
491          } 
492      }
493  }
 * */