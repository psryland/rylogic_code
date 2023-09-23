using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>A lookup table from style bits to styles</summary>
	[SuppressMessage("Usage", "CA2237:Mark ISerializable types with serializable", Justification = "<Pending>")]
	public class TextStyleMap :Dictionary<ushort, TextStyle>
	{
		public TextStyleMap()
		{
			this[0] = TextStyle.Default;
		}
	}

	/// <summary>Base class for text styles</summary>
	public class TextStyle :TextRunProperties
	{
		public static readonly TextStyle Default = new();
		public TextStyle(Typeface? font = null, double? emsize = null, Brush? fore = null, Brush? back = null)
		{
			if (font != null) Font = font;
			if (emsize != null) EmSize = emsize.Value;
			if (fore != null) ForeColour = fore;
			if (back != null) BackColour = back;
		}

		/// <inheritdoc cref="TextRunProperties.Typeface"/>
		public virtual Typeface Font { get; set; } = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

		/// <inheritdoc cref="TextRunProperties.FontRenderingEmSize"/>
		public virtual double EmSize { get; set; } = 12.0;

		/// <inheritdoc cref="TextRunProperties.FontHintingEmSize"/>
		public virtual double EmHint { get; set; } = 12.0f;

		/// <inheritdoc cref="TextRunProperties.ForegroundBrush"/>
		public virtual Brush ForeColour { get; set; } = Brushes.Black;

		/// <inheritdoc cref="TextRunProperties.BackgroundBrush"/>
		public virtual Brush BackColour { get; set; } = Brushes.Transparent;

		/// <inheritdoc cref="TextRunProperties.CultureInfo"/>
		public virtual CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

		/// <inheritdoc cref="TextRunProperties.TextDecorations"/>
		public virtual TextDecorationCollection Decorations { get; set; } = new TextDecorationCollection();

		/// <inheritdoc cref="TextRunProperties.TextEffects"/>
		public virtual TextEffectCollection Effects { get; set; } = new TextEffectCollection();

		/// <inheritdoc cref="TextRunProperties.NumberSubstitution"/>
		public virtual NumberSubstitution Numbers { get; set; } = new NumberSubstitution();

		#region TextRunProperties
		public override Typeface Typeface => Font;
		public override double FontRenderingEmSize => EmSize;
		public override double FontHintingEmSize => EmHint;
		public override TextDecorationCollection TextDecorations => Decorations;
		public override Brush ForegroundBrush => ForeColour;
		public override Brush BackgroundBrush => BackColour;
		public override CultureInfo CultureInfo => Culture;
		public override TextEffectCollection TextEffects => Effects;
		public override NumberSubstitution NumberSubstitution => Numbers;
		#endregion
	}

	/// <summary>Base class for paragraph styles</summary>
	public class ParaStyle :TextParagraphProperties
	{
		public static readonly ParaStyle Default = new();
		public ParaStyle(TextWrapping? wrapping = null, TextAlignment? alignment = null, double? tab_size = null)
		{
			if (wrapping != null) Wrapping = wrapping.Value;
			if (alignment != null) Alignment = alignment.Value;
			if (tab_size != null) TabSize = tab_size.Value;
		}

		/// <inheritdoc cref="TextParagraphProperties.TextWrapping"/>
		public virtual TextWrapping Wrapping { get; set; } = TextWrapping.NoWrap;

		/// <inheritdoc cref="TextParagraphProperties.TextAlignment"/>
		public virtual TextAlignment Alignment { get; set; } = TextAlignment.Left;

		/// <inheritdoc cref="TextParagraphProperties.FlowDirection"/>
		public virtual FlowDirection Flow { get; set; } = FlowDirection.LeftToRight;

		/// <inheritdoc cref="TextParagraphProperties.LineHeight"/>
		public virtual double Height { get; set; } = double.NaN;

		/// <inheritdoc cref="TextParagraphProperties.FirstLineInParagraph"/>
		public virtual bool FirstLine { get; set; } = false;

		/// <inheritdoc cref="TextParagraphProperties.DefaultIncrementalTab"/>
		public virtual double TabSize { get; set; } = 20.0;

		/// <inheritdoc cref="TextParagraphProperties.Indent"/>
		public virtual double IndentSize { get; set; } = 0.0;

		#region TextParagraphProperties
		public override TextWrapping TextWrapping => Wrapping;
		public override TextAlignment TextAlignment => Alignment;
		public override FlowDirection FlowDirection => Flow;
		public override double DefaultIncrementalTab => TabSize;
		public override double LineHeight => Height;
		public override bool FirstLineInParagraph => FirstLine;
		public override TextRunProperties DefaultTextRunProperties => TextStyle.Default;
		public override TextMarkerProperties TextMarkerProperties => MarkerStyle.Default;
		public override double Indent => IndentSize;
		#endregion
	}

	/// <summary>Base class for marker styles</summary>
	public class MarkerStyle :TextMarkerProperties
	{
		public static readonly MarkerStyle Default = new();
		public MarkerStyle()
		{
		}

		#region TextMarkerProperties
		public override double Offset => 0.0;
		public override TextSource TextSource => null!;
		#endregion
	}
}