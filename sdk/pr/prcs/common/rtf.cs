//***************************************************
// Rtf string builder
//  Copyright © Rylogic Ltd 2012
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace pr.common
{
	/// <summary>Namespace for RTF string generation</summary>
	public static class Rtf
	{
		[Flags] public enum EFontStyle
		{
			Regular   = 0,
			Bold      = 1 << 0,
			Italic    = 1 << 1,
			Underline = 1 << 2,
			Strikeout = 1 << 3,
			Super     = 1 << 4,
			Sub       = 1 << 5,
		}
		
		/// <summary>Text alignment</summary>
		public enum EAlign
		{
			Left        = 0,
			Centre      = 1,
			Right       = 2,
			Justify     = 3,
			Distributed = 4,
		}

		/// <summary>Font descriptions</summary>
		public static class FontDesc
		{
			#region Font Description Strings
			public const string AgencyFB                             = @"\fnil\fcharset0 Agency FB;";
			public const string Algerian                             = @"\fnil\fcharset0 Algerian;";
			public const string Arial                                = @"\fnil\fcharset0 Arial;";
			public const string ArialBlack                           = @"\fnil\fcharset0 Arial Black;";
			public const string ArialNarrow                          = @"\fnil\fcharset0 Arial Narrow;";
			public const string ArialRoundedMTBold                   = @"\fnil\fcharset0 Arial Rounded MT Bold;";
			public const string BaskervilleOldFace                   = @"\fnil\fcharset0 Baskerville Old Face;";
			public const string Bauhaus93                            = @"\fnil\fcharset0 Bauhaus 93;";
			public const string BellMT                               = @"\fnil\fcharset0 Bell MT;";
			public const string BerlinSansFBDemi                     = @"\fnil\fcharset0 Berlin Sans FB Demi;";
			public const string BernardMTCondensed                   = @"\fnil\fcharset0 Bernard MT Condensed;";
			public const string BlackadderITC                        = @"\fnil\fcharset0 Blackadder ITC;";
			public const string BodoniMT                             = @"\fnil\fcharset0 Bodoni MT;";
			public const string BodoniMTBlack                        = @"\fnil\fcharset0 Bodoni MT Black;";
			public const string BodoniMTPosterCompressed             = @"\fnil\fcharset0 Bodoni MT Poster Compressed;";
			public const string BookAntiqua                          = @"\fnil\fcharset0 Book Antiqua;";
			public const string BookmanOldStyle                      = @"\fnil\fcharset0 Bookman Old Style;";
			public const string BookshelfSymbol7                     = @"\fnil\fcharset2 Bookshelf Symbol 7;";
			public const string BradleyHandITC                       = @"\fnil\fcharset0 Bradley Hand ITC;";
			public const string BritannicBold                        = @"\fnil\fcharset0 Britannic Bold;";
			public const string Broadway                             = @"\fnil\fcharset0 Broadway;";
			public const string BrushScriptMT                        = @"\fnil\fcharset0 Brush Script MT;";
			public const string Calibri                              = @"\fnil\fcharset0 Calibri;";
			public const string CalistoMT                            = @"\fnil\fcharset0 Calisto MT;";
			public const string Cambria                              = @"\fnil\fcharset0 Cambria;";
			public const string CambriaMath                          = @"\fnil\fcharset0 Cambria Math;";
			public const string Candara                              = @"\fnil\fcharset0 Candara;";
			public const string Centaur                              = @"\fnil\fcharset0 Centaur;";
			public const string CenturyGothic                        = @"\fswiss\fprq2\fcharset0 Century Gothic;";
			public const string CenturySchoolbook                    = @"\fnil\fcharset0 Century Schoolbook;";
			public const string Chiller                              = @"\fnil\fcharset0 Chiller;";
			public const string ColonnaMT                            = @"\fnil\fcharset0 Colonna MT;";
			public const string ComicSansMS                          = @"\fnil\fcharset0 Comic Sans MS;";
			public const string Consolas                             = @"\fnil\fcharset0 Consolas;";
			public const string Constantia                           = @"\fnil\fcharset0 Constantia;";
			public const string CooperBlack                          = @"\fnil\fcharset0 Cooper Black;";
			public const string CopperplateGothicBold                = @"\fnil\fcharset0 Copperplate Gothic Bold;";
			public const string CopperplateGothicLight               = @"\fnil\fcharset0 Copperplate Gothic Light;";
			public const string CordiaNew                            = @"\fswiss\fprq2\fcharset0 Cordia New;";
			public const string Corbel                               = @"\fnil\fcharset0 Corbel;";
			public const string Courier                              = @"\fnil\fcharset0 Courier;";
			public const string CourierNew                           = @"\fnil\fcharset0 Courier New;";
			public const string CurlzMT                              = @"\fnil\fcharset0 Curlz MT;";
			public const string DejaVuSans                           = @"\fnil\fcharset0 DejaVu Sans;";
			public const string DejaVuSansCondensed                  = @"\fnil\fcharset0 DejaVu Sans Condensed;";
			public const string DejaVuSansLight                      = @"\fnil\fcharset0 DejaVu Sans Light;";
			public const string DejaVuSansMono                       = @"\fnil\fcharset0 DejaVu Sans Mono;";
			public const string DejaVuSerif                          = @"\fnil\fcharset0 DejaVu Serif;";
			public const string DejaVuSerifCondensed                 = @"\fnil\fcharset0 DejaVu Serif Condensed;";
			public const string EdwardianScriptITC                   = @"\fnil\fcharset0 Edwardian Script ITC;";
			public const string Elephant                             = @"\fnil\fcharset0 Elephant;";
			public const string EngraversMT                          = @"\fnil\fcharset0 Engravers MT;";
			public const string ErasBoldITC                          = @"\fnil\fcharset0 Eras Bold ITC;";
			public const string ErasDemiITC                          = @"\fnil\fcharset0 Eras Demi ITC;";
			public const string ErasLightITC                         = @"\fnil\fcharset0 Eras Light ITC;";
			public const string ErasMediumITC                        = @"\fnil\fcharset0 Eras Medium ITC;";
			public const string FelixTitling                         = @"\fnil\fcharset0 Felix Titling;";
			public const string Fixedsys                             = @"\fnil\fcharset0 Fixedsys;";
			public const string FootlightMTLight                     = @"\fnil\fcharset0 Footlight MT Light;";
			public const string Forte                                = @"\fnil\fcharset0 Forte;";
			public const string FranklinGothicBook                   = @"\fnil\fcharset0 Franklin Gothic Book;";
			public const string FranklinGothicDemi                   = @"\fnil\fcharset0 Franklin Gothic Demi;";
			public const string FranklinGothicDemiCond               = @"\fnil\fcharset0 Franklin Gothic Demi Cond;";
			public const string FranklinGothicHeavy                  = @"\fnil\fcharset0 Franklin Gothic Heavy;";
			public const string FranklinGothicMedium                 = @"\fnil\fcharset0 Franklin Gothic Medium;";
			public const string FranklinGothicMediumCond             = @"\fnil\fcharset0 Franklin Gothic Medium Cond;";
			public const string FreestyleScript                      = @"\fnil\fcharset0 Freestyle Script;";
			public const string FrenchScriptMT                       = @"\fnil\fcharset0 French Script MT;";
			public const string Gabriola                             = @"\fnil\fcharset0 Gabriola;";
			public const string Garamond                             = @"\fnil\fcharset0 Garamond;";
			public const string GentiumBasic                         = @"\fnil\fcharset0 Gentium Basic;";
			public const string GentiumBookBasic                     = @"\fnil\fcharset0 Gentium Book Basic;";
			public const string Georgia                              = @"\fnil\fcharset0 Georgia;";
			public const string Gigi                                 = @"\fnil\fcharset0 Gigi;";
			public const string GillSansMT                           = @"\fnil\fcharset0 Gill Sans MT;";
			public const string GillSansMTCondensed                  = @"\fnil\fcharset0 Gill Sans MT Condensed;";
			public const string GillSansMTExtCondensedBold           = @"\fnil\fcharset0 Gill Sans MT Ext Condensed Bold;";
			public const string GillSansUltraBold                    = @"\fnil\fcharset0 Gill Sans Ultra Bold;";
			public const string GillSansUltraBoldCondensed           = @"\fnil\fcharset0 Gill Sans Ultra Bold Condensed;";
			public const string GloucesterMTExtraCondensed           = @"\fnil\fcharset0 Gloucester MT Extra Condensed;";
			public const string GoudyOldStyle                        = @"\fnil\fcharset0 Goudy Old Style;";
			public const string GoudyStout                           = @"\fnil\fcharset0 Goudy Stout;";
			public const string Haettenschweiler                     = @"\fnil\fcharset0 Haettenschweiler;";
			public const string HarlowSolidItalic                    = @"\fnil\fcharset0 Harlow Solid Italic;";
			public const string Harrington                           = @"\fnil\fcharset0 Harrington;";
			public const string HighTowerText                        = @"\fnil\fcharset0 High Tower Text;";
			public const string HYSWLongFangSong                     = @"\fnil\fcharset134 HYSWLongFangSong;";
			public const string Impact                               = @"\fnil\fcharset0 Impact;";
			public const string ImprintMTShadow                      = @"\fnil\fcharset0 Imprint MT Shadow;";
			public const string InformalRoman                        = @"\fnil\fcharset0 Informal Roman;";
			public const string Jokerman                             = @"\fnil\fcharset0 Jokerman;";
			public const string JuiceITC                             = @"\fnil\fcharset0 Juice ITC;";
			public const string KristenITC                           = @"\fnil\fcharset0 Kristen ITC;";
			public const string KunstlerScript                       = @"\fnil\fcharset0 Kunstler Script;";
			public const string LiberationSansNarrow                 = @"\fnil\fcharset0 Liberation Sans Narrow;";
			public const string LucidaBright                         = @"\fnil\fcharset0 Lucida Bright;";
			public const string LucidaCalligraphy                    = @"\fnil\fcharset0 Lucida Calligraphy;";
			public const string LucidaConsole                        = @"\fnil\fcharset0 Lucida Console;";
			public const string LucidaFax                            = @"\fnil\fcharset0 Lucida Fax;";
			public const string LucidaHandwriting                    = @"\fnil\fcharset0 Lucida Handwriting;";
			public const string LucidaSans                           = @"\fnil\fcharset0 Lucida Sans;";
			public const string LucidaSansTypewriter                 = @"\fnil\fcharset0 Lucida Sans Typewriter;";
			public const string LucidaSansUnicode                    = @"\fnil\fcharset0 Lucida Sans Unicode;";
			public const string Magneto                              = @"\fnil\fcharset0 Magneto;";
			public const string MaiandraGD                           = @"\fnil\fcharset0 Maiandra GD;";
			public const string MaturaMTScriptCapitals               = @"\fnil\fcharset0 Matura MT Script Capitals;";
			public const string MicrosoftSansSerif                   = @"\fnil\fcharset0 Microsoft Sans Serif;";
			public const string Mistral                              = @"\fnil\fcharset0 Mistral;";
			public const string Modern                               = @"\fnil\fcharset255 Modern;";
			public const string ModernNo20                           = @"\fnil\fcharset0 Modern No. 20;";
			public const string MonotypeCorsiva                      = @"\fnil\fcharset0 Monotype Corsiva;";
			public const string MSOutlook                            = @"\fnil\fcharset2 MS Outlook;";
			public const string MSReferenceSansSerif                 = @"\fnil\fcharset0 MS Reference Sans Serif;";
			public const string MSReferenceSpecialty                 = @"\fnil\fcharset2 MS Reference Specialty;";
			public const string MSSansSerif                          = @"\fnil\fcharset0 MS Sans Serif;";
			public const string MSSerif                              = @"\fnil\fcharset0 MS Serif;";
			public const string MTExtra                              = @"\fnil\fcharset2 MT Extra;";
			public const string NiagaraEngraved                      = @"\fnil\fcharset0 Niagara Engraved;";
			public const string NiagaraSolid                         = @"\fnil\fcharset0 Niagara Solid;";
			public const string OCRAExtended                         = @"\fnil\fcharset0 OCR A Extended;";
			public const string OldEnglishTextMT                     = @"\fnil\fcharset0 Old English Text MT;";
			public const string Onyx                                 = @"\fnil\fcharset0 Onyx;";
			public const string OpenSymbol                           = @"\fnil\fcharset0 OpenSymbol;";
			public const string PalaceScriptMT                       = @"\fnil\fcharset0 Palace Script MT;";
			public const string PalatinoLinotype                     = @"\fnil\fcharset0 Palatino Linotype;";
			public const string Papyrus                              = @"\fnil\fcharset0 Papyrus;";
			public const string Parchment                            = @"\fnil\fcharset0 Parchment;";
			public const string Perpetua                             = @"\fnil\fcharset0 Perpetua;";
			public const string PerpetuaTitlingMT                    = @"\fnil\fcharset0 Perpetua Titling MT;";
			public const string Playbill                             = @"\fnil\fcharset0 Playbill;";
			public const string PoorRichard                          = @"\fnil\fcharset0 Poor Richard;";
			public const string Pristina                             = @"\fnil\fcharset0 Pristina;";
			public const string RageItalic                           = @"\fnil\fcharset0 Rage Italic;";
			public const string Ravie                                = @"\fnil\fcharset0 Ravie;";
			public const string Rockwell                             = @"\fnil\fcharset0 Rockwell;";
			public const string RockwellCondensed                    = @"\fnil\fcharset0 Rockwell Condensed;";
			public const string RockwellExtraBold                    = @"\fnil\fcharset0 Rockwell Extra Bold;";
			public const string Roman                                = @"\fnil\fcharset255 Roman;";
			public const string Script                               = @"\fnil\fcharset255 Script;";
			public const string ScriptMTBold                         = @"\fnil\fcharset0 Script MT Bold;";
			public const string SegoePrint                           = @"\fnil\fcharset0 Segoe Print;";
			public const string SegoeScript                          = @"\fnil\fcharset0 Segoe Script;";
			public const string SegoeUI                              = @"\fnil\fcharset0 Segoe UI;";
			public const string SegoeUILight                         = @"\fnil\fcharset0 Segoe UI Light;";
			public const string SegoeUISemibold                      = @"\fnil\fcharset0 Segoe UI Semibold;";
			public const string SegoeUISymbol                        = @"\fnil\fcharset0 Segoe UI Symbol;";
			public const string ShowcardGothic                       = @"\fnil\fcharset0 Showcard Gothic;";
			public const string SmallFonts                           = @"\fnil\fcharset0 Small Fonts;";
			public const string SnapITC                              = @"\fnil\fcharset0 Snap ITC;";
			public const string Stencil                              = @"\fnil\fcharset0 Stencil;";
			public const string Symbol                               = @"\fnil\fcharset2 Symbol;";
			public const string Symbola                              = @"\fnil\fcharset0 Symbola;";
			public const string System                               = @"\fnil\fcharset0 System;";
			public const string Tahoma                               = @"\fnil\fcharset0 Tahoma;";
			public const string TempusSansITC                        = @"\fnil\fcharset0 Tempus Sans ITC;";
			public const string TeraSpecial                          = @"\fnil\fcharset2 Tera Special;";
			public const string Terminal                             = @"\fnil\fcharset255 Terminal;";
			public const string TimesNewRoman                        = @"\fnil\fcharset0 Times New Roman;";
			public const string TimesRoman                           = @"\froman Tms Rmn;";
			public const string TrebuchetMS                          = @"\fnil\fcharset0 Trebuchet MS;";
			public const string TwCenMT                              = @"\fnil\fcharset0 Tw Cen MT;";
			public const string TwCenMTCondensed                     = @"\fnil\fcharset0 Tw Cen MT Condensed;";
			public const string TwCenMTCondensedExtraBold            = @"\fnil\fcharset0 Tw Cen MT Condensed Extra Bold;";
			public const string Verdana                              = @"\fnil\fcharset0 Verdana;";
			public const string VinerHandITC                         = @"\fnil\fcharset0 Viner Hand ITC;";
			public const string Vivaldi                              = @"\fnil\fcharset0 Vivaldi;";
			public const string VladimirScript                       = @"\fnil\fcharset0 Vladimir Script;";
			public const string Webdings                             = @"\fnil\fcharset2 Webdings;";
			public const string WideLatin                            = @"\fnil\fcharset0 Wide Latin;";
			public const string Wingdings                            = @"\fnil\fcharset2 Wingdings;";
			public const string Wingdings2                           = @"\fnil\fcharset2 Wingdings 2;";
			public const string Wingdings3                           = @"\fnil\fcharset2 Wingdings 3;";
			#endregion
		}

		/// <summary>All rtf elements implement this</summary>
		public abstract class Content
		{
			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public abstract void ToRtf(StrBuild sb, Content parent);
		}

		/// <summary>A rectangle used to define margins and padding</summary>
		public class Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
			
			public Rect() {}
			public Rect(int l, int t, int r, int b) { Left = l; Top = t; Right = r; Bottom = b; }
			public Rect(Rect rhs)                   { Left = rhs.Left; Top = rhs.Top; Right = rhs.Right; Bottom = rhs.Bottom; }
			public bool AllZero                     { get { return Left == 0 && Top == 0 && Right == 0 && Bottom == 0; } }
			public static Rect Zero                 { get { return new Rect(0,0,0,0); } }
		}

		/// <summary>Represents the state of all rtf style control words</summary>
		public class TextStyle
		{
			/// <summary>The default style</summary>
			public static TextStyle Default { get { return m_default; } }
			private static readonly TextStyle m_default = new TextStyle();

			/// <summary>The index of the font to use. Use rtf.FontIndex(Font.Arial) to get the font index</summary>
			public int FontIndex { get; set; }

			/// <summary>The size of the font in 'points'. Default is 12</summary>
			public int FontSize { get; set; }

			/// <summary>The font style for bold, italics, underlining, or strike through</summary>
			public EFontStyle FontStyle { get; set; }
			
			/// <summary>The background colour. Use rtf.ColourIndex(Color.Black) to get the colour index</summary>
			public int BackColourIndex { get; set; }

			/// <summary>The font colour. Use rtf.ColourIndex(Color.Black) to get the colour index</summary>
			public int ForeColourIndex { get; set; }

			public TextStyle() :this(0, 10, EFontStyle.Regular) {}
			public TextStyle(int font_index, int font_size = 12, EFontStyle font_style = EFontStyle.Regular)
			{
				FontIndex       = font_index;
				FontSize        = font_size;
				FontStyle       = font_style;
				BackColourIndex = 0;
				ForeColourIndex = 0;
			}
			public TextStyle(TextStyle rhs)
			{
				FontIndex       = rhs.FontIndex       ;
				FontSize        = rhs.FontSize        ;
				FontStyle       = rhs.FontStyle       ;
				BackColourIndex = rhs.BackColourIndex ;
				ForeColourIndex = rhs.ForeColourIndex ;
			}

			/// <summary>Writes control words for the differences in style begin 'prev' and 'next'</summary>
			public static void Write(StrBuild sb, TextStyle next, TextStyle prev)
			{
				if (next.FontStyle != prev.FontStyle)
				{
					if (next.FontStyle == EFontStyle.Regular)
						sb.Append(StrBuild.EType.Control, @"\plain");
					else
					{
						var diff = next.FontStyle ^ prev.FontStyle;
						if ((diff & EFontStyle.Bold     ) != 0) sb.Append(StrBuild.EType.Control, (next.FontStyle & EFontStyle.Bold     ) != 0 ? @"\b"      : @"\b0"         );
						if ((diff & EFontStyle.Italic   ) != 0) sb.Append(StrBuild.EType.Control, (next.FontStyle & EFontStyle.Italic   ) != 0 ? @"\i"      : @"\i0"         );
						if ((diff & EFontStyle.Underline) != 0) sb.Append(StrBuild.EType.Control, (next.FontStyle & EFontStyle.Underline) != 0 ? @"\ul"     : @"\ulnone"     );
						if ((diff & EFontStyle.Strikeout) != 0) sb.Append(StrBuild.EType.Control, (next.FontStyle & EFontStyle.Strikeout) != 0 ? @"\strike" : @"\strike0"    );
						if ((diff & EFontStyle.Super    ) != 0) sb.Append(StrBuild.EType.Control, (next.FontStyle & EFontStyle.Super    ) != 0 ? @"\super"  : @"\nosupersub" );
						if ((diff & EFontStyle.Sub      ) != 0) sb.Append(StrBuild.EType.Control, (next.FontStyle & EFontStyle.Sub      ) != 0 ? @"\sub"    : @"\nosupersub" );
					}
				}
				
				if (next.ForeColourIndex != prev.ForeColourIndex) sb.AppendFormat(StrBuild.EType.Control, @"\cf{0}" ,next.ForeColourIndex);
				if (next.BackColourIndex != prev.BackColourIndex) sb.AppendFormat(StrBuild.EType.Control, @"\highlight{0}" ,next.BackColourIndex); // \cbN doesn't work, MS didn't implement it...fail
				
				if (next.FontIndex != prev.FontIndex) sb.AppendFormat(StrBuild.EType.Control, @"\f{0}"  ,next.FontIndex);
				if (next.FontSize  != prev.FontSize ) sb.AppendFormat(StrBuild.EType.Control, @"\fs{0}" ,next.FontSize * 2); // \fs is in 'half-points'
			}
		}

		/// <summary>Describes a single edge of a border for a paragraph</summary>
		public class BorderStyle
		{
			/// <summary>Border sides</summary>
			public ESide Side { get; set; }
			[Flags] public enum ESide
			{
				Left       = 1 << 0,
				Top        = 1 << 1,
				Right      = 1 << 2,
				Bottom     = 1 << 3,
				Horizontal = 1 << 4,
				Vertical   = 1 << 5,
				All        = Left|Top|Right|Bottom
			}

			/// <summary>Border types</summary>
			public BType Type { get; set; }
			public sealed class BType
			{
				public const string None                 = @"brdrnone"; // No border.
				public const string SingleThickness      = @"\brdrs"; // Single-thickness border.
				public const string DoubleThickness      = @"\brdrth"; // Double-thickness border.
				public const string Shadowed             = @"\brdrsh"; // Shadowed border.
				public const string Double               = @"\brdrdb"; // Double border.
				public const string Dot                  = @"\brdrdot"; // Dotted border.
				public const string Dash                 = @"\brdrdash"; // Dashed border.
				public const string Hair                 = @"\brdrhair"; // Hairline border.
				public const string Inset                = @"\brdrinset"; // Inset border.
				public const string Outset               = @"\brdroutset"; // Outset border.
				public const string DashSmall            = @"\brdrdashsm"; // Dash border (small).
				public const string DotDash              = @"\brdrdashd"; // Dot dash border.
				public const string DotDotDash           = @"\brdrdashdd"; // Dot dot dash border.
				public const string Triple               = @"\brdrtriple"; // Triple border.
				public const string ThickThinSmall       = @"\brdrtnthsg"; // Thick thin border (small).
				public const string ThinThickSmall       = @"\brdrthtnsg"; // Thin thick border (small).
				public const string ThinThickThinSmall   = @"\brdrtnthtnsg"; // Thin thick thin border (small).
				public const string ThickThinMedium      = @"\brdrtnthmg"; // Thick thin border (medium).
				public const string ThinThickMedium      = @"\brdrthtnmg"; // Thin thick border (medium).
				public const string ThinThickThinMedium  = @"\brdrtnthtnmg"; // Thin thick thin border (medium).
				public const string ThickThinLarge       = @"\brdrtnthlg"; // Thick thin border (large).
				public const string ThinThickLarge       = @"\brdrthtnlg"; // Thin thick border (large).
				public const string ThinThickThinLarge   = @"\brdrtnthtnlg"; // Thin thick thin border (large).
				public const string Wavy                 = @"\brdrwavy"; // Wavy border.
				public const string DoubleWavy           = @"\brdrwavydb"; // Double wavy border.
				public const string Striped              = @"\brdrdashdotstr"; // Striped border.
				public const string Emboss               = @"\brdremboss"; // Emboss border.
				public const string Engrave              = @"\brdrengrave"; // Engrave border.
				public const string Frame                = @"\brdrframe"; // Border resembles a "Frame."
				
				private string m_value;
				public override string ToString()                   { return m_value; }
				public static implicit operator BType(string value) { return new BType{m_value = value}; }
				public static implicit operator string(BType value) { return value.m_value; }
			}

			/// <summary>
			/// Width (in twips) of the pen used to draw the paragraph border line.
			/// Width cannot be greater than 75. To obtain a larger border width, the \brdth control word can be used to obtain a width double that of 'Width'.</summary>
			public int Width { get; set; }

			/// <summary>The color of the paragraph border, specified as an index into the color table</summary>
			public int ColourIndex { get; set; }

			public BorderStyle() :this(ESide.All, BType.SingleThickness, 1, 0) {}
			public BorderStyle(ESide side, BType type, int width, int colour_index)
			{
				Side        = side;
				Type        = type;
				Width       = width;
				ColourIndex = colour_index;
			}
			public BorderStyle(BorderStyle rhs)
			{
				Side        = rhs.Side        ;
				Type        = rhs.Type        ;
				Width       = rhs.Width       ;
				ColourIndex = rhs.ColourIndex ;
			}

			/// <summary>Writes a border definition to 'sb'</summary>
			public void ToRtf(StrBuild sb, StrBuild.EFor elem)
			{
				// Output for each side
				for (int i = 0; i != 4; ++i)
				{
					int side = 1 << i;
					if (((int)Side & side) == 0) continue;
					StrBuild.RtfBorder(sb, (ESide)side, Type, Width, ColourIndex, elem);
				}
			}
		}

		/// <summary>Represents the state of all rtf style control words for a paragraph</summary>
		public class ParagraphStyle
		{
			/// <summary>The default style</summary>
			public static ParagraphStyle Default { get { return m_default; } }
			private static readonly ParagraphStyle m_default = new ParagraphStyle();

			/// <summary>Paragraph alignment. Left, centre, right, or justify. Note: justify is not supported by RichTextBox</summary>
			public EAlign Alignment { get; set; }

			/// <summary>The indenting to apply to each line (in 100th's of a character unit)</summary>
			public int LineIndent { get; set; }

			/// <summary>The indenting to apply to the first line of a paragraph (in 100th's of a character unit)</summary>
			public int FirstIndent { get; set; }

			/// <summary>The indenting to apply from the right (in 100th's of a character unit)</summary>
			public int LineIndentRight { get; set; }

			/// <summary>Line spacing (in 100th's of a character unit)</summary>
			public int LineSpacing { get; set; }

			/// <summary>Paragraph spacing (in 100th's of a character unit)</summary>
			public int ParaSpacing { get; set; }

			/// <summary>Controls automatic hyphenation for the paragraph. Append 1 or nothing to toggle property on; append 0 to turn it off.</summary>
			public bool Hyphenation { get; set; }

			/// <summary>The borders of the paragraph</summary>
			public List<BorderStyle> Border { get; private set; }

			public ParagraphStyle() :this(EAlign.Left) {}
			public ParagraphStyle(EAlign align, int line_indent = 0, int first_line = 0, int line_spacing = 0, int para_spacing = 0)
			{
				Alignment       = align;
				LineIndent      = line_indent;
				FirstIndent     = first_line;
				LineIndentRight = 0;
				LineSpacing     = line_spacing;
				ParaSpacing     = para_spacing;
				Hyphenation     = false;
				Border          = new List<BorderStyle>();
			}
			public ParagraphStyle(ParagraphStyle rhs)
			{
				Alignment       = rhs.Alignment  ;
				LineIndent      = rhs.LineIndent ;
				FirstIndent     = rhs.FirstIndent;
				LineSpacing     = rhs.LineSpacing;
				LineIndentRight = rhs.LineIndentRight;
				ParaSpacing     = rhs.ParaSpacing;
				Hyphenation     = rhs.Hyphenation;
				Border          = new List<BorderStyle>(rhs.Border);
			}

			/// <summary>Writes control words for the differences in style begin 'prev' and 'next'</summary>
			public void ToRtf(StrBuild sb, bool in_table)
			{
				if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
				sb.Append(StrBuild.EType.Control, @"\pard");
				if (in_table) sb.Append(StrBuild.EType.Control,@"\intbl");
				StrBuild.RtfAlignment(sb, Alignment, StrBuild.EFor.Paragraph);
				
				if (FirstIndent     != 0) sb.AppendFormat(StrBuild.EType.Control, @"\fi{0}" ,FirstIndent);
				if (LineIndent      != 0) sb.AppendFormat(StrBuild.EType.Control, @"\li{0}" ,LineIndent );
				if (LineIndentRight != 0) sb.AppendFormat(StrBuild.EType.Control, @"\ri{0}" ,LineIndentRight);
				if (LineSpacing     != 0) sb.AppendFormat(StrBuild.EType.Control, @"\sl{0}\slmult1" ,LineSpacing * 2); // \sl is in half units
				if (ParaSpacing     != 0) sb.AppendFormat(StrBuild.EType.Control, @"\sa{0}" ,ParaSpacing * 2); // \sa is in half units
				if (Hyphenation) sb.Append(StrBuild.EType.Control, @"\hyphpar");
				
				foreach (var b in Border)
					b.ToRtf(sb, StrBuild.EFor.Paragraph);
			}
		}

		/// <summary>An object for constructing rtf strings</summary>
		public class Builder :IAppendable<Builder>
		{
			private readonly Root m_root;
			private readonly FontTable m_font_table;
 			private readonly ColourTable m_colour_table;

			public Builder()
			{
				m_root           = new Root();
				m_font_table     = new FontTable();
				m_colour_table   = new ColourTable();
				DefaultParaStyle = ParagraphStyle.Default;
				TextStyle        = TextStyle.Default;
				
				m_root.Content.Add(m_font_table);
				m_root.Content.Add(m_colour_table);
				m_font_table.Add(new Font());
			}

			/// <summary>The default style to use when creating paragraphs</summary>
			public ParagraphStyle DefaultParaStyle { get; set; }

			/// <summary>The current style used when appending text</summary>
			public TextStyle TextStyle { get; set; }

			/// <summary>Returns the contained content as an rtf string</summary>
			public override string ToString()
			{
 				var sb = new StrBuild();
				m_root.ToRtf(sb);
				return sb.ToString();
			}

			/// <summary>Returns the index of a font in the font table corresponding to 'font_desc'</summary>
			public int FontIndex(string font_desc)
			{
				return m_font_table.Add(font_desc);
			}

			/// <summary>Returns the index of a colour in the colour table corresponding to 'colour'</summary>
			public int ColourIndex(Color colour)
			{
				return m_colour_table.Add(colour) + 1;
			}

			/// <summary>Append rtf content</summary>
			public Builder Append(Paragraph para)     { m_root.Content.Add(para); return this; }
			public Builder Append(PageBreak pbreak)   { m_root.Content.Add(pbreak); return this; }
			public Builder Append(BulletList blist)   { m_root.Content.Add(blist); return this; }
			public Builder Append(Table table)        { m_root.Content.Add(table); return this; }
			public Builder Append(EmbeddedImage img)  { m_root.Content.Add(img); return this; }
			
			// Special handling for text.. auto concatenate
			public Builder Append(TextStyle style)    { TextStyle = style; return this; }
			public Builder Append(string str)         { Append(new TextSpan(str, TextStyle)); return this; }
			public Builder Append(TextSpan text)
			{
				// Try to append the text to the last paragraph
				Paragraph para = null;
				bool merge = m_root.Content.Count != 0 && (para = m_root.Content.Last() as Paragraph) != null && ReferenceEquals(para.ParaStyle, DefaultParaStyle);
				if (!merge) para = new Paragraph(DefaultParaStyle, TextStyle);
				
				// Concatenate the text
				para.Append(text);
				if (!merge) Append(para);
				return this;
			}
		}

		/// <summary>Mix in functionality for Append methods</summary>
		public interface IAppendable<out TBase> where TBase:IAppendable<TBase>
		{
			TBase Append(string str);
		}

		/// <summary>The rtf header string.</summary>
		private class Root
		{
			/// <summary>Nested content</summary>
			public readonly List<Content> Content = new List<Content>();

			/// <summary>The version of the rtf specification</summary>
			private int Version { get; set; }

			/// <summary>The character set used</summary>
			private string CharSet { get; set; }

			/// <summary>The code page used for unicode characters</summary>
			private int AnsiCodePage { get; set; }

			/// <summary>The default font index to use</summary>
			private int DefaultFontIndex { get; set; }

			/// <summary>The view mode of the document</summary>
			private int Viewkind { get; set; }

			/// <summary>Additional RTF header control words not given explicitly (use \chars)</summary>
			private string AdditionalControlWords { get; set; }

			/// <summary>Information about who created this rtf string</summary>
			private string Generator { get { return @"{\*\generator RylogicLimited prcs;}"; } }

			public Root()
			{
				Version                = 1;
				CharSet                = "ansi";
				AnsiCodePage           = 1252;
				DefaultFontIndex       = 0;
				Viewkind               = 4;
				AdditionalControlWords = "";
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public void ToRtf(StrBuild sb)
			{
				// e.g: @"\rtf1\ansi\deff0"; @"\rtf1\ansi\ansicpg1252\deff0\deflang3081";
				sb.AppendFormat(StrBuild.EType.Control, @"{{\rtf{0}\{1}\ansicpg{2}\deff{3}\viewkind{4}{5}{6}" ,Version ,CharSet ,AnsiCodePage ,DefaultFontIndex ,Viewkind ,AdditionalControlWords ,Generator);
				sb.Append(StrBuild.EType.Control, @"\pard");
				foreach (var c in Content)
				{
					Debug.Assert(!(c is TextSpan));
					c.ToRtf(sb, null);
				}
				sb.Append(StrBuild.EType.Control, @"}");
			}
		}

		/// <summary>A collection of fonts used in the rtf string</summary>
		private class FontTable :Content
		{
			/// <summary>Fonts in the font table</summary>
			private readonly List<Font> m_fonts = new List<Font>();

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				if (m_fonts.Count == 0) return;
				if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
				sb.Append(StrBuild.EType.Control, @"{\fonttbl");
				foreach (var f in m_fonts) f.ToRtf(sb, this);
				sb.Append(StrBuild.EType.Control, @"}");
			}

			/// <summary>Add a font to the font table. Returns the index of the font in the table</summary>
			public int Add(Font font)
			{
				for (int i = 0, iend = m_fonts.Count; i != iend; ++i)
				{
					if (string.CompareOrdinal(m_fonts[i].Desc, font.Desc) != 0) continue;
					Debug.Assert(m_fonts[i].Index == i);
					return m_fonts[i].Index;
				}
				font.Index = m_fonts.Count;
				m_fonts.Add(font);
				return font.Index;
			}

			/// <summary>Add a font by string description. Returns the index of the font in the table</summary>
			public int Add(string font)
			{
				return Add(new Font(font));
			}
		}

		/// <summary>A collection of colours used in the rtf string</summary>
		private class ColourTable :Content
		{
			/// <summary>The colour description</summary>
			private readonly List<Color> m_colours = new List<Color>();

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
				sb.Append(StrBuild.EType.Control, @"{\colortbl;");
				foreach (var c in m_colours) sb.AppendFormat(StrBuild.EType.Control, @"\red{0}\green{1}\blue{2};" ,c.R ,c.G, c.B);
				sb.Append(StrBuild.EType.Control, @"}");
			}

			/// <summary>Add a colour to the colour table. Returns the index of the colour in the table</summary>
			public int Add(Color colour)
			{
				for (int i = 0, iend = m_colours.Count; i != iend; ++i)
				{
					if (!m_colours[i].Equals(colour)) continue;
					return i;
				}
				m_colours.Add(colour);
				return m_colours.Count - 1;
			}
		}

		/// <summary>A font entry in the font table</summary>
		private class Font :Content
		{
			/// <summary>The index of this font in the font table</summary>
			public int Index { get; set; }

			/// <summary>The font description string</summary>
			public string Desc { get; private set; }
			
			public Font() :this(FontDesc.Arial) {}
			public Font(string desc) { Index = -1; Desc  = desc; }

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				Debug.Assert(Index != -1, "This font has not been added to the font table yet");
				sb.AppendFormat(StrBuild.EType.Control, @"{{\f{0}{1}}}",Index ,Desc);
			}
		}

		/// <summary>A collection of TextSpans that form a paragraph</summary>
		public class Paragraph :Content, IAppendable<Paragraph>
		{
			private readonly List<TextSpan> m_spans = new List<TextSpan>();

			/// <summary>Style that applies to the paragraph</summary>
			public ParagraphStyle ParaStyle { get; set; }

			/// <summary>Default text style for this paragraph</summary>
			public TextStyle Style { get; set; }

			/// <summary>Set to true when this paragraph is part of a table</summary>
			public bool InTable { get; set; }

			public Paragraph() :this(ParagraphStyle.Default, TextStyle.Default) {}
			public Paragraph(ParagraphStyle para_style) :this(para_style, TextStyle.Default) {}
			public Paragraph(TextStyle text_style) :this(ParagraphStyle.Default, text_style) {}
			public Paragraph(ParagraphStyle para_style, TextStyle style)
			{
				ParaStyle = para_style;
				Style     = style;
				InTable   = false;
			}

			/// <summary>Constructs a paragraph containing a single string</summary>
			public Paragraph(string str) :this() { Append(str); }
			public Paragraph(string str, ParagraphStyle para_style) :this(para_style, TextStyle.Default) { Append(str); }
			public Paragraph(string str, TextStyle text_style) :this(ParagraphStyle.Default, text_style) { Append(str); }
			public Paragraph(string str, ParagraphStyle para_style, TextStyle style) :this(para_style, style) { Append(str); }

			/// <summary>Constructs a paragraph containing a single string</summary>
			public Paragraph(TextSpan text) :this() { Append(text); }
			public Paragraph(TextSpan text, ParagraphStyle para_style) :this(para_style, TextStyle.Default) { Append(text); }
			public Paragraph(TextSpan text, TextStyle text_style) :this(ParagraphStyle.Default, text_style) { Append(text); }
			public Paragraph(TextSpan text, ParagraphStyle para_style, TextStyle style) :this(para_style, style) { Append(text); }

			public Paragraph Append(TextStyle style)  { Style = style; return this; }
			public Paragraph Append(string str)       { return Append(new TextSpan(str, Style)); }
			public Paragraph Append(TextSpan text)
			{
				// Try to merge the content to the last one added
				if (m_spans.Count != 0 && m_spans.Last().Merge(text))
					return this;
				
				m_spans.Add(text);
				return this;
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				ParaStyle.ToRtf(sb, InTable);
				TextStyle.Write(sb, Style, TextStyle.Default);
				foreach (var s in m_spans) s.ToRtf(sb, this);
				TextStyle.Write(sb, TextStyle.Default, Style);
				sb.Append(StrBuild.EType.Control, @"\par");
			}
		}

		/// <summary>Inserts a page break</summary>
		public class PageBreak :Content
		{
			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent) { sb.Append(StrBuild.EType.Control, @"\page"); }
		}

		/// <summary>A block of text that uses a specific style</summary>
		public class TextSpan :Content ,IAppendable<TextSpan>
		{
			/// <summary>A recycled string builder to reduce allocs</summary>
			private static readonly StringBuilder TmpSB = new StringBuilder();

			/// <summary>Collects the text for the span</summary>
			private readonly StringBuilder m_sb;

			/// <summary>The text string</summary>
			public string Text { get { return m_sb.ToString(); } }

			/// <summary>The style applied to the text</summary>
			public TextStyle Style { get; set; }

			public TextSpan() :this("", TextStyle.Default) {}
			public TextSpan(string text) :this(text, TextStyle.Default) {}
			public TextSpan(string text, TextStyle style)
			{
				m_sb = new StringBuilder(Sanitise(text));
				Style = style;
			}

			public TextSpan Append(string str)
			{
				m_sb.Append(Sanitise(str));
				return this;
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				var para = parent as Paragraph;
				var parent_style = para != null ? para.Style : TextStyle.Default;
				var text = Text;
				
				TextStyle.Write(sb, Style, parent_style);
				int s,e; for (s = 0; (e = text.IndexOf('\n', s)) != -1; s = e+1)
				{
					sb.Append(StrBuild.EType.Content, text, s, e-s)
					  .AppendLine(StrBuild.EType.Control, @"\line");
				}
				if (s != text.Length) sb.Append(StrBuild.EType.Content, text, s, text.Length - s);
				TextStyle.Write(sb, parent_style, Style);
			}

			/// <summary>Merges two content objects into one. Return true if the merge is successful, false if 'other' cannot be merged</summary>
			public bool Merge(TextSpan rhs)
			{
				// We can merge text spans if they share the same style
				if (!ReferenceEquals(Style, rhs.Style)) return false;
				Append(rhs.Text);
				return true;
			}

			/// <summary>Escapes and sanitises a plain text string</summary>
			public static string Sanitise(string value)
			{
				TmpSB.Clear();
				TmpSB.EnsureCapacity(value.Length + 100);

				for (int i = 0, iend = value.Length; i != iend; ++i)
				{
					char ch = value[i];
					
					// Standardise newlines to a single '\n' character
					if      (ch == '\r') { TmpSB.Append('\n'); if (i+1 != iend && value[i+1] == '\n') ++i; }
					else if (ch == '\n') { TmpSB.Append('\n'); }
					
					// If this is a unicode character > 255, replace with it's \u1234 code
					else if (ch > 255)   { TmpSB.Append(@"\u").Append((int)ch).Append("?"); }
					
					// Escape '{', '}', and '\' characters
					else if (ch == '\\') { TmpSB.Append(@"\\"); }
					else if (ch == '{')  { TmpSB.Append(@"\{"); }
					else if (ch == '}')  { TmpSB.Append(@"\}"); }
					
					else TmpSB.Append(ch);
				}
				return TmpSB.ToString();
			}
		}

		/// <summary>A list of strings displayed as a bullet point or numbered list</summary>
		public class BulletList :Content
		{
			/// <summary>The text for each bullet point</summary>
			private readonly List<TextSpan> m_points;

			/// <summary>Bullet points are controlled using paragraph styles</summary>
			public ParagraphStyle ParaStyle { get; set; }

			/// <summary>The font to use in the bullet point list</summary>
			public TextStyle TextStyle { get; set; }

			/// <summary>True to use numbers, false to use bullet points</summary>
			public bool Numbered { get; set; }

			/// <summary>The ascii character to use as the bullet point</summary>
			public byte BulletCharacter { get; set; }

			public BulletList() :this(false, new List<string>()) {}
			public BulletList(IEnumerable<string> points) :this(false, points) {}
			public BulletList(bool numbered) :this(numbered, new string[0]) {}
			public BulletList(bool numbered, IEnumerable<string> points)
			{
				m_points        = points.Select(s => new TextSpan(s)).ToList();
				ParaStyle       = new ParagraphStyle{FirstIndent = -360, LineIndent = 720, LineSpacing = 120};
				TextStyle       = TextStyle.Default;
				Numbered        = numbered;
				BulletCharacter = 0xB7;
			}

			/// <summary>Add text for a bullet point</summary>
			public BulletList Add(TextSpan point)
			{
				m_points.Add(point);
				return this;
			}

			/// <summary>Add text for a bullet point</summary>
			public BulletList Add(string point)
			{
				return Add(new TextSpan(point));
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				ParaStyle.ToRtf(sb, false);
				TextStyle.Write(sb, TextStyle, TextStyle.Default);
				if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
				
				// Output basic text for the bullet point
				bool first_time = true; int i = 1;
				foreach (var p in m_points)
				{
					if (Numbered) sb.AppendFormat(StrBuild.EType.Control, @"{{\pntext\f{0} {1}.\tab}}" ,TextStyle.FontIndex ,i++);
					else          sb.AppendFormat(StrBuild.EType.Control, @"{{\pntext\'{0:X}\tab}}", BulletCharacter);
					
					// Output a bulleted paragraph control word for rtf readers that understand it.
					// Note: have to pass '{' and '}' as format parameters to solve a quirk of the
					// string escaping algorithm. '{{' and '}}' are replace by '{' and '}' before
					// the string is checked for format specifiers.
					if (first_time)
					{
						if (Numbered) sb.AppendFormat(StrBuild.EType.Control, @"{{\*\pn\pnlvlbody\pnf{0}\pnindent0\pnstart1\pndec{{\pntxta.}}}}" ,TextStyle.FontIndex);
						else          sb.AppendFormat(StrBuild.EType.Control, @"{{\*\pn\pnlvlblt\pnf{0}\pnindent0{{\pntxtb\'{1:X}{2}" ,TextStyle.FontIndex, (int)BulletCharacter, "}}");
						first_time = false;
					}
					
					// Write the bullet point text
					sb.Append(StrBuild.EType.Content, p.Text).AppendLine(StrBuild.EType.Control,@"\par");
				}
			}
		}

		/// <summary>
		/// Represents a table in the rtf doc.
		/// There is no RTF table group; instead, tables are specified as paragraph properties.
		/// A table is represented as a sequence of table rows. A table row is a continuous sequence of
		/// paragraphs partitioned into cells. The table row begins with the \trowd control word and ends
		///  with the \row control word. Every paragraph that is contained in a table row must have the
		/// \intbl control word specified or inherited from the previous paragraph. A cell may have more
		/// than one paragraph in it; the cell is terminated by a cell mark (the \cell control word), and
		/// the row is terminated by a row mark (the \row control word). Table rows can also be positioned.
		/// In this case, every paragraph in a table row must have the same positioning controls (see the apoctl 
		/// controls in the Positioned Objects and Frames sub-section of this RTF Specification). Table properties
		/// may be inherited from the previous row; therefore, a series of table rows may be introduced by a single tbldef.<para/>
		/// Note: the RichTextBox control seems to have very limited support for tables</summary>
		public class Table :Content
		{
			/// <summary>Style for a table row</summary>
			public class RowStyle
			{
				public static RowStyle Default { get { return m_default ?? (m_default = new RowStyle()); } }
				private static RowStyle m_default;

				public RowStyle()
				{
					RowHeight  = 0;
					LeftOffset = 44;
					Alignment  = EAlign.Left;
					Margin     = Rect.Zero;
					Padding    = new Rect(55,55,55,55);
				}
				public RowStyle(RowStyle rhs)
				{
					RowHeight  = rhs.RowHeight;
					LeftOffset = rhs.LeftOffset;
					Alignment  = rhs.Alignment;
					Margin     = new Rect(rhs.Margin);
					Padding    = new Rect(rhs.Padding);
				}

				/// <summary>The height of the table row. Default is 0 which should auto wrap the contained text</summary>
				public int RowHeight { get; set; }

				/// <summary>Position of the leftmost edge of the row with respect to the left edge of its column (in twips)</summary>
				public int LeftOffset { get; set; }

				/// <summary>Row alignment</summary>
				public EAlign Alignment { get; set; }

				/// <summary>The margin around each cell (in twips)</summary>
				public Rect Margin { get; set; }

				/// <summary>The padding within each cell (in twips)</summary>
				public Rect Padding { get; set; }

				/// <summary>Write this style as rtf into 'sb'</summary>
				public void ToRtf(StrBuild sb)
				{
					// Row left offset
					if (LeftOffset != 0)
					{
						if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
						sb.AppendFormat(StrBuild.EType.Control, @"\trgaph{0}" ,LeftOffset);
						sb.AppendFormat(StrBuild.EType.Control, @"\trleft{0}" ,Padding.Left - LeftOffset);
					}
					
					// Row alignment
					StrBuild.RtfAlignment(sb, Alignment, StrBuild.EFor.Row);
					
					// Row height
					if (RowHeight != 0)
						sb.AppendFormat(StrBuild.EType.Control, @"\trrh{0}" ,RowHeight);
					
					// Margin
					if (!Margin.AllZero)
					{
						if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
						sb.AppendFormat(StrBuild.EType.Control,
							@"\trspdfl3\trspdl{0}"+"\n"+
							@"\trspdft3\trspdt{1}"+"\n"+
							@"\trspdfr3\trspdr{2}"+"\n"+
							@"\trspdfb3\trspdb{3}"+"\n"
							,Margin.Left ,Margin.Top ,Margin.Right ,Margin.Bottom);
					}
					
					// Padding
					if (!Padding.AllZero)
					{
						if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
						sb.AppendFormat(StrBuild.EType.Control,
							@"\trpaddfl3\trpaddl{0}"+"\n"+
							@"\trpaddft3\trpaddt{1}"+"\n"+
							@"\trpaddfr3\trpaddr{2}"+"\n"+
							@"\trpaddfb3\trpaddb{3}"+"\n"
							,Padding.Left ,Padding.Top ,Padding.Right ,Padding.Bottom);
					}
				}
			}

			/// <summary>Style for a table cell</summary>
			public class CellStyle
			{
				/// <summary>The default style</summary>
				public static CellStyle Default { get { return m_default ?? (m_default = new CellStyle()); } }
				private static CellStyle m_default;

				/// <summary>Vertical alignment within the cell</summary>
				public EVerticalAlign VerticalAlignment { get; set; }
				public sealed class EVerticalAlign
				{
					public const string Default = Top;
					public const string Top     = @"\clvertalt"; // Text is top-aligned in cell (the default).
					public const string Centre  = @"\clvertalc"; // Text is centered vertically in cell.
					public const string Bottom  = @"\clvertalb"; // Text is bottom-aligned in cell.

					private string m_value;
					public override string ToString()                            { return m_value; }
					public static implicit operator EVerticalAlign(string value) { return new EVerticalAlign{m_value = value}; }
					public static implicit operator string(EVerticalAlign value) { return value.m_value; }
				}

				/// <summary>The visible borders of the cell</summary>
				public List<BorderStyle> Borders { get; set; }

				/// <summary>The padding within each cell (in twips). Overrides the row padding</summary>
				public Rect Padding { get; set; }

				/// <summary>The size of the cell (in twips) (Ignored if AutoFit is true)</summary>
				public int CellSize { get; set; }

				/// <summary>Text flow</summary>
				public ETextFlow TextFlow { get; set; }
				public class ETextFlow
				{
					public const string Default                       = Left2Right_Top2Bottom;
					public const string Left2Right_Top2Bottom         = @"\cltxlrtb";  // Text in a cell flows from left to right and top to bottom (default).
					public const string Right2Left_Top2Bottom         = @"\cltxtbrl";  // Text in a cell flows right to left and top to bottom.
					public const string Left2Right_Bottom2Top         = @"\cltxbtlr";  // Text in a cell flows left to right and bottom to top.
					public const string Left2Right_Top2BottomVertical = @"\cltxlrtbv"; // Text in a cell flows left to right and top to bottom, vertical.
					public const string Right2Left_Top2BottomVertical = @"\cltxtbrlv"; // Text in a cell flows top to bottom and right to left, vertical.
				
					private string m_value;
					public override string ToString()                       { return m_value; }
					public static implicit operator ETextFlow(string value) { return new ETextFlow{m_value = value}; }
					public static implicit operator string(ETextFlow value) { return value.m_value; }
				}

				public CellStyle()
				{
					VerticalAlignment  = EVerticalAlign.Default;
					Borders            = new List<BorderStyle>{new BorderStyle()};
					Padding            = Rect.Zero;
					CellSize           = 1000;
					TextFlow           = ETextFlow.Default;
				}
				public CellStyle(CellStyle rhs)
				{
					VerticalAlignment  = rhs.VerticalAlignment;
					Borders            = new List<BorderStyle>(rhs.Borders);
					Padding            = new Rect(rhs.Padding);
					CellSize           = rhs.CellSize;
					TextFlow           = rhs.TextFlow;
				}

				/// <summary>Write this style as rtf into 'sb'</summary>
				public void ToRtf(StrBuild sb, ref int row_size, RowStyle rstyle, bool restore)
				{
					if (!sb.LineStart)
						sb.AppendLine(StrBuild.EType.Control);
					
					// Vertical alignment
					if (VerticalAlignment != EVerticalAlign.Default)
						sb.Append(StrBuild.EType.Control, VerticalAlignment);

					// Custom cell padding
					if (!Padding.AllZero)
						sb.AppendFormat(StrBuild.EType.Control,
							@"\clpadfl3\clpadft3\clpadfb3\clpadfr3" + // 0Null.  Ignore \clpadr in favor of \trgaph (Word 97 style cell padding).
							@"\clpadl{0}\clpadt{1}\clpadr{2}\clpadb{3}"
							,Padding.Left ,Padding.Top ,Padding.Right ,Padding.Bottom);

					// Borders
					foreach (var b in Borders) b.ToRtf(sb, StrBuild.EFor.Cell);
					sb.AppendLine(StrBuild.EType.Control);

					// Text flow
					if (TextFlow != ETextFlow.Default)
						sb.Append(StrBuild.EType.Control, TextFlow);

					// Cell size
					row_size += CellSize;
					sb.AppendFormat(StrBuild.EType.Control, @"\cellx{0}" ,row_size);
				}
			}
			
			/// <summary>A row in the table</summary>
			public class Row
			{
				/// <summary>Constructs a row containing 'cells' cells</summary>
				protected Row(int cells, RowStyle style, Func<Cell> new_cell)
				{
					Style = style;
					Cells = new Cell[cells];
					for (int i = 0; i != cells; ++i)
						Cells[i] = new_cell();
				}

				/// <summary>The style to apply to this row</summary>
				public RowStyle Style { get; set; }

				/// <summary>The cells within the row</summary>
				public Cell[] Cells { get; private set; }
				
				/// <summary>Cell indexer</summary>
				public Cell this[int cell] { get { return Cells[cell]; } }
				
				/// <summary>Writes rtf for this row into 'sb'</summary>
				public void ToRtf(StrBuild sb, Content parent)
				{
					if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
					sb.Append(StrBuild.EType.Control, @"\trowd");
					Style.ToRtf(sb);
					int row_size = Style.LeftOffset;
					foreach (var c in Cells) c.Style.ToRtf(sb, ref row_size, Style, false);
					foreach (var c in Cells) c.ToRtf(sb, parent);
					if (!sb.LineStart) sb.AppendLine(StrBuild.EType.Control);
					sb.Append(StrBuild.EType.Control, @"\row");
				}
			}

			/// <summary>A cell within a row</summary>
			public class Cell
			{
				private readonly List<Content> m_content;
				protected Cell() :this(CellStyle.Default) {}
				protected Cell(CellStyle style)
				{
					m_content = new List<Content>();
					Style = style;
				}

				/// <summary>The style for this cell</summary>
				public CellStyle Style { get; set; }

				/// <summary>Append content to the cell</summary>
				public Cell Append(Paragraph content)
				{
					content.InTable = true;
					m_content.Add(content);
					return this;
				}
				public Cell Append(TextSpan text)  { return Append(new Paragraph(text)); }
				public Cell Append(string str)     { return Append(new TextSpan(str)); }
				
				/// <summary>Writes rtf for this cell into 'sb'</summary>
				public void ToRtf(StrBuild sb, Content parent)
				{
					foreach (var c in m_content) c.ToRtf(sb, parent);
					sb.Remove(@"\par");
					sb.Append(StrBuild.EType.Control, @"\cell");
				}
			}

			// Private classes so that only the Table can create rows/cells
			private class RowInternal  :Row  { public RowInternal(int cells, RowStyle style, Func<Cell> new_cell) :base(cells, style, new_cell) {} }
			private class CellInternal :Cell { public CellInternal(CellStyle style) :base(style) {} }
			private readonly List<Row> m_rows; // The rows of the table

			/// <summary>The default style to use for rows</summary>
			public RowStyle DefaultRowStyle { get; set; }

			/// <summary>The default style to use for cells</summary>
			public CellStyle DefaultCellStyle { get; set; }

			/// <summary>Builds a table with 'rows' rows, each 'cell's columns wide</summary>
			public Table(int rows, int cells)
			{
				DefaultRowStyle = RowStyle.Default;
				DefaultCellStyle = CellStyle.Default;
				m_rows = new List<Row>(rows);
				for (int r = 0; r != rows; ++r)
					AddRow(cells);
			}

			/// <summary>Add a row to the table</summary>
			public Row AddRow(int cells)
			{
				var row = new RowInternal(cells, DefaultRowStyle, ()=>new CellInternal(DefaultCellStyle));
				m_rows.Add(row);
				return row;
			}

			/// <summary>Access a row in the table</summary>
			public Row this[int row]
			{
				get { return m_rows[row]; }
			}

			/// <summary>Access a cell in the table</summary>
			public Cell this[int row, int cell]
			{
				get { return m_rows[row].Cells[cell]; }
			}

			/// <summary>Set 'style' as the cell style for each cell in column 'column_index'</summary>
			public void SetColumnStyle(int column_index, CellStyle style)
			{
				foreach (var r in m_rows)
				{
					if (column_index < 0 || column_index >= r.Cells.Length) continue;
					r.Cells[column_index].Style = style;
				}
			}

			/// <summary>Write the table as rtf to 'sb'</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				foreach (var r in m_rows)
					r.ToRtf(sb, this);
			}
		}

		/// <summary>An embedded image</summary>
		public class EmbeddedImage :Content
		{
			private static readonly string[] Hex = Enumerable.Range(0, 256).Select(x => x.ToString("X2")).ToArray();
			private readonly StringBuilder m_sb;
			
			// ReSharper disable UnusedMember.Local
			[Flags] private enum EmfToWmfFlags
			{
				Default          = 0, // Use the default conversion
				EmbedEmf         = 1, // Embedded the source of the EMF metafile within the resulting WMF metafile
				IncludePlaceable = 2, // Place a 22-byte header in the resulting WMF file. The header is required for the metafile to be considered place-able.
				NoXORClip        = 4, // Don't simulate clipping by using the XOR operator.
			};
			private enum MapMode // Descriptions can be found with documentation of Windows GDI function SetMapMode
			{
				MM_TEXT        = 1,
				MM_LOMETRIC    = 2,
				MM_HIMETRIC    = 3,
				MM_LOENGLISH   = 4,
				MM_HIENGLISH   = 5,
				MM_TWIPS       = 6,
				MM_ISOTROPIC   = 7,
				MM_ANISOTROPIC = 8,
			}
			// ReSharper restore UnusedMember.Local

			/// <summary>Device context safe handle </summary>
			private class HDCHandle :SafeHandle
			{
				private readonly Graphics m_gfx;
				public HDCHandle(Graphics gfx) : base(IntPtr.Zero, true) { m_gfx = gfx; SetHandle(m_gfx.GetHdc()); }
				protected override bool ReleaseHandle()                  { m_gfx.ReleaseHdc(handle); return true; }
				public override bool IsInvalid                           { get { return handle == IntPtr.Zero; } }
				public IntPtr Handle                                     { get { return handle; } }
			}

			/// <summary>Use the EmfToWmfBits function from GDI+ to convert an enhanced metafile to a windows metafile</summary>
			/// <param name="emf">The handle of the enhanced metafile to convert</param>
			/// <param name="wmf_out_size">The size of the buffer used to store the return wmf</param>
			/// <param name="wmf_out">An array of bytes used to hold the returned wmf</param>
			/// <param name="mapping_mode">The mapping mode of the image.</param>
			/// <param name="flags">Flags used to specify the format of the wmf returned</param>
			[DllImport("gdiplus.dll")] private static extern uint GdipEmfToWmfBits(IntPtr emf, uint wmf_out_size, byte[] wmf_out, int mapping_mode, EmfToWmfFlags flags);

			public EmbeddedImage(Image image) :this() { Add(image); }
			public EmbeddedImage()
			{
				m_sb = new StringBuilder();
				ParaStyle = ParagraphStyle.Default;
			}
			
			/// <summary>Paragraph style properties for the embedded image</summary>
			public ParagraphStyle ParaStyle { get; set; }

			/// <summary>Add an image to embed</summary>
			public void Add(Image image)
			{
				// Some credit to Anton for this code:
				// ----------------------------------------------------------------------------------------
				//    _                ___        _..-._   Date: 12/11/08    23:32
				//    \`.|\..----...-'`   `-._.-'' _.-..'
				//    /  ' `         ,       __.-''
				//    )/` _/     \   `-_,   /     Solution: RTFLib
				//    `-'" `"\_  ,_.-;_.-\_ ',    Project : RTFLib
				//        _.-'_./   {_.'   ; /    Author  : Anton
				//       {_.-``-'         {_/     Assembly: 1.0.0.0
				//                                Copyright © 2005-2008, Rogue Trader/MWM
				// ----------------------------------------------------------------------------------------
				// The Graphics Context's resolution is simply the current resolution at which
				// windows is being displayed.  Normally it's 96 dpi, but instead of assuming
				// I just added the code.
				//
				// According to Ken Howe at pbdr.com, "Twips are screen-independent units
				// used to ensure that the placement and proportion of screen elements in
				// your screen application are the same on all display systems."
				//
				// Units Used
				// ----------
				// 1 Twip = 1/20 Point
				// 1 Point = 1/72 Inch
				// 1 Twip = 1/1440 Inch
				//
				// 1 Inch = 2.54 cm
				// 1 Inch = 25.4 mm
				// 1 Inch = 2540 (0.01)mm
				
				// Creates the RTF control string that describes the image being embedded.
				// This description specifies that the image is an MM_ANISOTROPIC metafile,
				// meaning that both X and Y axes can be scaled independently. The control
				// string also gives the images current dimensions, and its target dimensions,
				// so if you want to control the size of the image being inserted, this would
				// be the place to do it. The prefix should have the form:
				// {\pict\wmetafile8\picw[A]\pich[B]\picwgoal[C]\pichgoal[D]
				//
				// where:
				// A   = current width of the metafile in hundredths of millimetres (0.01mm)
				//     = Image Width in Inches * Number of (0.01mm) per inch
				//     = (Image Width in Pixels / Graphics Context's Horizontal Resolution) * 2540
				//     = (Image Width in Pixels / Graphics.DpiX) * 2540
				// B   = current height of the metafile in hundredths of millimetres (0.01mm)
				//     = Image Height in Inches * Number of (0.01mm) per inch
				//     = (Image Height in Pixels / Graphics Context's Vertical Resolution) * 2540
				//     = (Image Height in Pixels / Graphics.DpiX) * 2540
				// C   = target width of the metafile in twips
				//     = Image Width in Inches * Number of twips per inch
				//     = (Image Width in Pixels / Graphics Context's Horizontal Resolution) * 1440
				//     = (Image Width in Pixels / Graphics.DpiX) * 1440
				// D   = target height of the metafile in twips
				//     = Image Height in Inches * Number of twips per inch
				//     = (Image Height in Pixels / Graphics Context's Horizontal Resolution) * 1440
				//     = (Image Height in Pixels / Graphics.DpiX) * 1440

				using (var gfx = Graphics.FromImage(image))
				using (var stream = new MemoryStream())
				{
					const int HMM_PER_INCH   = 2540; // The number of hundredths of millimetres (0.01 mm) in an inch
					const int TWIPS_PER_INCH = 1440; // The number of twips in an inch. For more information, see GetImagePrefix() method.

					// Get the horizontal and vertical resolutions at which the object is being displayed
					float x_dpi = gfx.DpiX, y_dpi = gfx.DpiY;
					var picw     = (int)Math.Round(HMM_PER_INCH   * image.Width  / x_dpi); // Calculate the current width of the image in (0.01)mm
					var pich     = (int)Math.Round(HMM_PER_INCH   * image.Height / y_dpi); // Calculate the current height of the image in (0.01)mm
					var picwgoal = (int)Math.Round(TWIPS_PER_INCH * image.Width  / x_dpi); // Calculate the target width of the image in twips
					var pichgoal = (int)Math.Round(TWIPS_PER_INCH * image.Height / y_dpi); // Calculate the target height of the image in twips

					// Create the wmf and append its bytes in HEX format
					// Wraps the image in an emf by drawing the image onto the graphics context,
					// then converts the emf to a wmf, and finally appends the bits of the wmf
					// in hex to the string buffer
					
					// Draw the image into the emf
					using (var hdc = new HDCHandle(gfx))
					using (var meta_file = new Metafile(stream, hdc.Handle))
					{
						// Draw the image into the meta file
						using (var gfx2 = Graphics.FromImage(meta_file))
							gfx2.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));

						// Get the handle of the Enhanced Metafile
						// We don't need to free this handle, because the metafile owns it
						var emf = meta_file.GetHenhmetafile();
						
						// Calling with null for the buffer returns the required buffer size
						var buf_size = GdipEmfToWmfBits(emf, 0, null, (int)MapMode.MM_ANISOTROPIC, EmfToWmfFlags.Default);
						var buffer = new byte[buf_size];

						// Convert the emf to an wmf
						GdipEmfToWmfBits(emf, buf_size, buffer, (int)MapMode.MM_ANISOTROPIC, EmfToWmfFlags.Default);

						// Append to the rtf string
						m_sb.EnsureCapacity(2*buffer.Length + 100);
						m_sb.AppendFormat(@"{{\pict\wmetafile8\picw{0}\pich{1}\picwgoal{2}\pichgoal{3} " ,picw ,pich ,picwgoal, pichgoal);
						int i = 0; foreach (byte b in buffer)
						{
							if (i-- == 0) { m_sb.Append('\n'); i = 31; }
							m_sb.Append(Hex[b]);
							
						}
						m_sb.Append(@"}");
					}
				}
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				ParaStyle.ToRtf(sb, false);
				sb.Append(StrBuild.EType.Control, m_sb.ToString());
				sb.AppendLine(StrBuild.EType.Control,@"\par");
			}
		}

		/// <summary>A string builder class with a couple of extra state variables</summary>
		public class StrBuild
		{
			private readonly StringBuilder m_sb = new StringBuilder();

			/// <summary>Rtf string data is either control words or content</summary>
			public enum EType { Control, Content }

			/// <summary>True when we're at the start of a new line</summary>
			public bool LineStart { get; private set; }

			/// <summary>True if a whitespace delimiter is needed before the next lot of rtf content</summary>
			public bool DelimNeeded { get; private set; }

			/// <summary>The length of the contained string</summary>
			public int Length { get { return m_sb.Length; } }

			/// <summary>Return the last character added to the string builder</summary>
			public char LastChar { get { return m_sb.Length != 0 ? m_sb[m_sb.Length - 1] : default(Char); } }

			/// <summary>Append a control words or content</summary>
			private StrBuild Append(EType type, string str, int start, int length, bool newline)
			{
				// If this is content being added, and previously control words were added, add a delimiter
				if (type != EType.Control && DelimNeeded && !LineStart)
					m_sb.Append(' ');
				
				// Add the string
				m_sb.Append(str, start, length);
				if (newline) m_sb.Append('\n');
				
				// A delimiter will be needed if this was a control word that didn't end with a '}'
				DelimNeeded = type == EType.Control && LastChar != '}';
				LineStart = LastChar == '\n';
				return this;
			}
			public StrBuild Append      (EType type, string str)                                        { return Append(type, str, 0, str.Length, false); }
			public StrBuild Append      (EType type, string str, int start, int length)                 { return Append(type, str, start, length, false); }
			public StrBuild AppendLine  (EType type)                                                    { return Append(type, string.Empty, 0, 0, true); }
			public StrBuild AppendLine  (EType type, string str)                                        { return Append(type, str, 0, str.Length, true); }
			public StrBuild AppendFormat(EType type, string fmt, object arg0)                           { return Append(type, string.Format(fmt, arg0)); }
			public StrBuild AppendFormat(EType type, string fmt, object arg0, object arg1)              { return Append(type, string.Format(fmt, arg0, arg1)); }
			public StrBuild AppendFormat(EType type, string fmt, object arg0, object arg1, object arg2) { return Append(type, string.Format(fmt, arg0, arg1, arg2)); }
			public StrBuild AppendFormat(EType type, string fmt, params object[] args)                  { return Append(type, string.Format(fmt, args)); }

			public override string ToString() { return m_sb.ToString(); }

			/// <summary>RTF elements</summary>
			public enum EFor
			{
				Paragraph,
				Row,
				Cell,
			}

			// Helper methods
			internal static string Prefix(EFor elem)
			{
				switch (elem)
				{
				default: throw new ArgumentOutOfRangeException("elem");
				case EFor.Paragraph: return "";
				case EFor.Row:       return "tr";
				case EFor.Cell:      return "cl";
				}
			}

			/// <summary>Write an rtf alignment control word</summary>
			internal static StrBuild RtfAlignment(StrBuild sb, EAlign align, EFor elem)
			{
				string alignstr;
				switch (align)
				{
				default: throw new ArgumentOutOfRangeException("align");
				case EAlign.Left:         alignstr = @"\"+Prefix(elem)+"ql"; break;
				case EAlign.Centre:       alignstr = @"\"+Prefix(elem)+"qc"; break;
				case EAlign.Right:        alignstr = @"\"+Prefix(elem)+"qr"; break;
				case EAlign.Justify:      alignstr = @"\"+Prefix(elem)+"qj"; break;
				case EAlign.Distributed:  alignstr = @"\"+Prefix(elem)+"qd"; break;
				}
				sb.Append(EType.Control, alignstr);
				return sb;
			}

			/// <summary>Write an rtf border description</summary>
			public static StrBuild RtfBorder(StrBuild sb, BorderStyle.ESide side, BorderStyle.BType type, int width, int colour_index, EFor elem)
			{
				string sidestr;
				switch (side)
				{
				default: throw new ArgumentOutOfRangeException();
				case BorderStyle.ESide.Left:       sidestr = @"\"+Prefix(elem)+"brdrl"; break;
				case BorderStyle.ESide.Top:        sidestr = @"\"+Prefix(elem)+"brdrt"; break;
				case BorderStyle.ESide.Right:      sidestr = @"\"+Prefix(elem)+"brdrr"; break;
				case BorderStyle.ESide.Bottom:     sidestr = @"\"+Prefix(elem)+"brdrb"; break;
				case BorderStyle.ESide.Horizontal: sidestr = @"\"+Prefix(elem)+"brdrh"; break;
				case BorderStyle.ESide.Vertical:   sidestr = @"\"+Prefix(elem)+"brdrv"; break;
				}
				if (!sb.LineStart) sb.AppendLine(EType.Control);
				sb.Append(EType.Control, sidestr);
				sb.Append(EType.Control, type);
				sb.AppendFormat(EType.Control, @"\brdrw{0}" ,width);
				sb.AppendFormat(EType.Control, @"\brdrcf{0}" ,colour_index);
				return sb;
			}

			/// <summary>Remove 'text' from the end of the string builder (if it's there)</summary>
			public void Remove(string text)
			{
				if (m_sb.Length < text.Length) return;
				var tail = m_sb.ToString(m_sb.Length - text.Length, text.Length);
				if (string.Compare(text, tail, StringComparison.Ordinal) != 0) return;
				m_sb.Length -= text.Length;
			}
		}
	}
	
	/// <summary>Append mix in functionality for Rtf Content types can concatenate strings</summary>
	public static class RtfBuilderAppendMixin
	{
		public static T Append<T>    (this Rtf.IAppendable<T> This, string str, int start, int count) where T:Rtf.IAppendable<T>   { return This.Append(str.Substring(start, count)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, byte    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, sbyte   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, char    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, short   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, ushort  x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, int     x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, uint    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, long    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, ulong   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, float   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, double  x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T Append<T>    (this Rtf.IAppendable<T> This, decimal x)                        where T:Rtf.IAppendable<T>   { return This.Append(x.ToString(CultureInfo.InvariantCulture)); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This  )                                 where T:Rtf.IAppendable<T>   { return This.Append('\n'); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, string  x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, byte    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, sbyte   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, char    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, short   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, ushort  x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, int     x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, uint    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, long    x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, ulong   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, float   x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, double  x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
		public static T AppendLine<T>(this Rtf.IAppendable<T> This, decimal x)                        where T:Rtf.IAppendable<T>   { return This.Append(x).AppendLine(); }
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using common;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestRtf
		{
			[Test] public static void RtfDoc()
			{
				var rtf = new Rtf.Builder();
				rtf.Append("A basic string\n");
			
				rtf.TextStyle = new Rtf.TextStyle
				{
					FontIndex = rtf.FontIndex(Rtf.FontDesc.CourierNew),
					FontStyle = Rtf.EFontStyle.Italic|Rtf.EFontStyle.Underline,
					ForeColourIndex = rtf.ColourIndex(Color.Red),
					BackColourIndex = rtf.ColourIndex(Color.Green)
				};
				rtf.Append("Text in CourierNew font in Red/Green plus italic and underlined\n");
			
				rtf.TextStyle = Rtf.TextStyle.Default;
				rtf.AppendLine("A normal string with a new line");
			
				rtf.TextStyle = new Rtf.TextStyle
				{
					FontIndex = rtf.FontIndex(Rtf.FontDesc.MSSansSerif),
					FontSize = 24,
					FontStyle = Rtf.EFontStyle.Underline|Rtf.EFontStyle.Bold,
					ForeColourIndex = rtf.ColourIndex(Color.Blue),
				};
				rtf.Append("Bold big blue values of other types:\n");
			
				rtf.DefaultParaStyle = new Rtf.ParagraphStyle{LineIndent = 400};
				rtf.TextStyle = new Rtf.TextStyle
				{
					FontIndex = rtf.FontIndex(Rtf.FontDesc.ComicSansMS),
					FontSize = 10,
					FontStyle = Rtf.EFontStyle.Regular,
					BackColourIndex = rtf.ColourIndex(Color.DarkGray)
				};
				rtf.Append("byte.MaxValue     : ").Append(byte.MaxValue     ).AppendLine();
				rtf.Append("sbyte.MinValue    : ").Append(sbyte.MinValue    ).AppendLine();
				rtf.Append("'X'               : ").Append('X'               ).AppendLine();
				rtf.Append("short.MinValue    : ").Append(short.MinValue    ).AppendLine();
				rtf.Append("ushort.MaxValue   : ").Append(ushort.MaxValue   ).AppendLine();
				rtf.Append("int.MinValue      : ").Append(int.MinValue      ).AppendLine();
				rtf.Append("uint.MaxValue     : ").Append(uint.MaxValue     ).AppendLine();
				rtf.Append("long.MinValue     : ").Append(long.MinValue     ).AppendLine();
				rtf.Append("ulong.MaxValue    : ").Append(ulong.MaxValue    ).AppendLine();
				rtf.Append("float.MinValue    : ").Append(float.MinValue    ).AppendLine();
				rtf.Append("double.MaxValue   : ").Append(double.MaxValue   ).AppendLine();
				rtf.Append("decimal.MaxValue  : ").Append(decimal.MaxValue  ).AppendLine();
			
				rtf.DefaultParaStyle = new Rtf.ParagraphStyle{Alignment = Rtf.EAlign.Right};
				rtf.TextStyle = new Rtf.TextStyle{ForeColourIndex = rtf.ColourIndex(Color.DarkMagenta)};
				rtf.AppendLine("I'm right aligned");
			
				rtf.DefaultParaStyle = Rtf.ParagraphStyle.Default;
				rtf .Append(new Rtf.TextStyle{FontStyle = Rtf.EFontStyle.Super})
					.Append("Superscript")
					.Append(Rtf.TextStyle.Default)
					.Append("Words")
					.Append(new Rtf.TextStyle{FontStyle = Rtf.EFontStyle.Sub})
					.Append("Subscript");

				var para = new Rtf.Paragraph{ParaStyle = new Rtf.ParagraphStyle{FirstIndent = 800, LineIndent = 400, ParaSpacing = 200, Hyphenation = true}};
				para.Append("I'm the start of a paragraph. ")
					.Append("I'm some more words in the middle of the paragraph. These words make the paragraph fairly long which is good for testing. ")
					.Append("I'm the end of the paragraph.");
				rtf.Append(para);
			
				rtf.Append(new Rtf.PageBreak());
			
				para = new Rtf.Paragraph(para.ParaStyle);
				para.Append("I'm a new short paragraph.");
				rtf.Append(para);

				var bulletlist = new Rtf.BulletList(false);
				bulletlist.Add("My first point");
				bulletlist.Add(new Rtf.TextSpan("My next point"){Style = new Rtf.TextStyle{FontStyle = Rtf.EFontStyle.Bold}});
				rtf.Append(bulletlist);

				var numberedlist = new Rtf.BulletList(true);
				numberedlist.Add("First");
				numberedlist.Add("Second");
				rtf.Append(numberedlist);

				using (var bm = Image.FromFile(@"z:\pictures\gekko\Smiling gekko 150x121.jpg"))
				{
					var img = new Rtf.EmbeddedImage(bm);
					rtf.Append(img);
				}

				var table = new Rtf.Table(3,3);
				table[0,0].Append(new Rtf.Paragraph("Hello")).Append(new Rtf.Paragraph("World"));
				table[0,1].Append("0x1");
				table[0,2].Append("0x2");
				table[1,0].Append("1x0");
				table[1,1].Append("This is the middle cell");
				table[1,2].Append("1x2");
				table[2,0].Append("2x0");
				table[2,1].Append("2x1");
				table[2,2].Append("The last cell");
				table.SetColumnStyle(0, new Rtf.Table.CellStyle{CellSize = 2000});
				table.SetColumnStyle(1, new Rtf.Table.CellStyle{CellSize = 4000});
				table.SetColumnStyle(2, new Rtf.Table.CellStyle{CellSize = 3000});
				rtf.Append(table);

				var str = rtf.ToString();
				File.WriteAllText("tmp.rtf", str);
			}
		}
	}
}

#endif



#if SHIT_VERSION
	// ----------------------------------------------------------------------------------------
//    _                ___        _..-._   Date: 12/11/08    23:32
//    \`.|\..----...-'`   `-._.-'' _.-..'
//    /  ' `         ,       __.-''
//    )/` _/     \   `-_,   /     Solution: RTFLib
//    `-'" `"\_  ,_.-;_.-\_ ',    Project : RTFLib
//        _.-'_./   {_.'   ; /    Author  : Anton
//       {_.-``-'         {_/     Assembly: 1.0.0.0
//                                Copyright © 2005-2008, Rogue Trader/MWM
// ----------------------------------------------------------------------------------------

	public enum EFont
	{
		Arial,
		ArialBlack,
		BookmanOldStyle,
		Broadway,
		CenturyGothic,
		Consolas,
		CordiaNew,
		CourierNew,
		FontTimesNewRoman,
		Garamond,
		Georgia,
		Impact,
		LucidaConsole,
		Symbol,
		WingDings,
		MSSansSerif
	}

	[Flags] public enum ECellBorderSide
	{
		None = 0,
		Left = 0x01,
		Right = 0x02,
		Top = 0x04,
		Bottom = 0x08,
		Default = 0x0F,
		DoubleThickness = 0x10,
		DoubleBorder = 0x20
	}

	[Flags] public enum ECellAlignment
	{
		None = 0,
		/// <summary>Content is vertically aligned at the bottom, and horizontally aligned at the centre.</summary>
		BottomCenter = 512,
		/// <summary>Content is vertically aligned at the bottom, and horizontally aligned on the left.</summary>
		BottomLeft = 256,
		/// <summary>Content is vertically aligned at the bottom, and horizontally aligned on the right.</summary>
		BottomRight = 1024,
		/// <summary>Content is vertically aligned in the middle, and horizontally aligned at the centre.</summary>
		MiddleCenter = 32,
		/// <summary>Content is vertically aligned in the middle, and horizontally aligned on the left.</summary>
		MiddleLeft = 16,
		/// <summary>Content is vertically aligned in the middle, and horizontally aligned on the right.</summary>
		MiddleRight = 64,
		/// <summary>Content is vertically aligned at the top, and horizontally aligned at the centre.</summary>
		TopCenter = 2,
		/// <summary>Content is vertically aligned at the top, and horizontally aligned on the left.</summary>
		TopLeft = 1,
		/// <summary>Content is vertically aligned at the top, and horizontally aligned on the right.</summary>
		TopRight = 4
	}

	public class Style
	{
		public EFont           Font            { get; set; }
		public float           FontSize        { get; set; }
		public FontStyle       FontStyle       { get; set; }
		public Color           BackColour      { get; set; }
		public Color           ForeColour      { get; set; }
		public int             LineIndent      { get; set; }
		public int             LineIndentFirst { get; set; }
		public StringAlignment Alignment       { get; set; }
	}

	/// <summary>Rich Text String Builder</summary>
	public class Builder :IDisposable
	{
		private static readonly char[] CharsNeedingEscaping = new[] {'{', '}', '\\'};
		private readonly List<EFont> m_font_table;
		private readonly List<Color> m_color_table;
		private readonly StringFormat m_sf;
		private readonly StringBuilder m_sb;
		private int m_font_index;
		private bool m_unwrapped;

		private Builder(EFont default_font, float default_font_size)
		{
			m_font_table  = new List<EFont>();
			m_color_table = new List<Color>();
			m_sb          = new StringBuilder();
				
			DefaultFont       = default_font;
			DefaultFontSize   = default_font_size;
			DefaultFontStyle  = FontStyle.Regular;
			DefaultForeColour = SystemColors.WindowText;
			DefaultBackColour = SystemColors.Window;
			DefaultStyle      = new Style
				{
					Font       = DefaultFont,
					FontSize   = DefaultFontSize,
					FontStyle  = DefaultFontStyle,
					ForeColour = DefaultForeColour,
					BackColour = DefaultBackColour,
				};

			Font         = DefaultFont;
			FontSize     = DefaultFontSize;
			FontStyle    = DefaultFontStyle;
				
			m_color_table.Add(DefaultForeColour);
			m_color_table.Add(DefaultBackColour);
			BackColour = DefaultBackColour;
			ForeColour = DefaultForeColour;
				
			LineIndentFirst = 0;
			LineIndent = 0;
				
			m_sf = (StringFormat)StringFormat.GenericDefault.Clone();
			m_sf.FormatFlags = StringFormatFlags.NoWrap;
			m_sf.Trimming    = StringTrimming.Word;
		}
		public Builder() :this(EFont.Arial) {}
		public Builder(EFont default_font) :this(default_font, 20F) {}
		public Builder(float default_font_size) : this(EFont.Arial, default_font_size) {}
		~Builder() { Dispose(false); }
		public void Dispose()
		{
			Dispose(true);
		}
		protected void Dispose(bool disposing)
		{
			if (!disposing) return;
			GC.SuppressFinalize(this);
		}

		/// <summary>The money shot, turns the collected data into an rtf string</summary>
		public override string ToString()
		{
			TmpSB.Clear();
				
			// Add the rtf header
			TmpSB.Append("{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang3081");
				
			// Add the font table
			TmpSB.Append("{\\fonttbl");
			for (int i = 0; i < m_font_table.Count; i++)
				TmpSB.Append(FontString(m_font_table[i], i));
			TmpSB.AppendLine("}");

			// Add the colour table
			TmpSB.Append("{\\colortbl ;");
			foreach (Color item in m_color_table)
				TmpSB.AppendFormat("\\red{0}\\green{1}\\blue{2};", item.R, item.G, item.B);
			TmpSB.AppendLine("}");

			TmpSB.Append("\\viewkind4\\uc1\\pard\\plain\\f0");
			TmpSB.AppendFormat("\\fs{0} ", DefaultFontSize);
			TmpSB.AppendLine();

			TmpSB.Append(m_sb.ToString());
			TmpSB.Append("}");
			return TmpSB.ToString();
		}

		public void Clear()
		{
			m_sb.Clear();
			m_font_table.Clear();
			m_color_table.Clear();
			m_font_index = FontIndex(DefaultFont);
			FontSize = DefaultFontSize;
			FontStyle = DefaultFontStyle;
			BackColour = DefaultBackColour;
			ForeColour = DefaultForeColour;
		}
		public int Length { get { throw new NotImplementedException(); } }

		// Defaults
		public EFont     DefaultFont       { get; private set; }
		public float     DefaultFontSize   { get; private set; }
		public FontStyle DefaultFontStyle  { get; private set; }
		public Color     DefaultForeColour { get; private set; }
		public Color     DefaultBackColour { get; private set; }
		public Style     DefaultStyle      { get; private set; }

		// Controls
		public Style           Style           { get; set; }
		public EFont           Font            { get { return m_font_table[m_font_index]; } set { m_font_index = FontIndex(value); } }
		public float           FontSize        { get; set; }
		public FontStyle       FontStyle       { get; set; }
		public Color           BackColour      { get; set; }
		public Color           ForeColour      { get; set; }
		public int             LineIndent      { get; set; }
		public int             LineIndentFirst { get; set; }
		public StringAlignment Alignment       { get { return m_sf.Alignment; } set { m_sf.Alignment = value; } }
			
		/// <summary>Append a normal (non-rtf) string</summary>
		public Builder Append(string value)
		{
			if (string.IsNullOrEmpty(value)) return this;
			using (new FormatWrap(this))
			{
				value = Sanitise(value);
				if (value.IndexOf(Environment.NewLine, StringComparison.Ordinal) < 0) m_sb.Append(value);
				else
				{
					string[] lines = value.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
					foreach (string line in lines)
					{
						m_sb.Append(line);
						m_sb.Append("\\line ");
					}
				}
			}
			return this;
		}
		public Builder Append(string value, int start_index, int count)                            { return Append(value.Substring(start_index, count)); }
		public Builder Append(char value)                                                          { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(char value, int repeat_count)                                        { TmpSB.Clear(); TmpSB.Append(value, repeat_count); return Append(TmpSB.ToString()); }
		public Builder Append(char[] value)                                                        { TmpSB.Clear(); TmpSB.Append(value); return Append(TmpSB.ToString()); }
		public Builder Append(short value)                                                         { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(double value)                                                        { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(float value)                                                         { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(int value)                                                           { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(long value)                                                          { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(bool value)                                                          { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(byte value)                                                          { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(decimal value)                                                       { return Append(value.ToString(CultureInfo.InvariantCulture)); }
		public Builder Append(object value)                                                        { return Append(value.ToString()); }
		public Builder AppendFormat(string format, object arg0)                                    { return Append(string.Format(format, arg0)); }
		public Builder AppendFormat(string format, object arg0, object arg1)                       { return Append(string.Format(format, arg0, arg1)); }
		public Builder AppendFormat(string format, object arg0, object arg1, object arg2)          { return Append(string.Format(format, arg0, arg1, arg2)); }
		public Builder AppendFormat(string format, params object[] args)                           { return Append(string.Format(format, args)); }
		public Builder AppendFormat(IFormatProvider provider, string format, params object[] args) { return Append(string.Format(provider, format, args)); }
		public Builder AppendLevel(int level)                                                      { m_sb.AppendFormat("\\level{0} ", level); return this; }
		public Builder AppendLine(string value)                                                    { using (new ParaWrap(this)) { Append(value); m_sb.AppendLine("\\line"); } return this; }
		public Builder AppendLine()                                                                { m_sb.AppendLine("\\line"); return this; }
		public Builder AppendLineFormat(string format, object arg0)                                { return AppendLine(string.Format(format, arg0)); }
		public Builder AppendLineFormat(string format, object arg0, object arg1)                   { return AppendLine(string.Format(format, arg0, arg1)); }
		public Builder AppendLineFormat(string format, object arg0, object arg1, object arg2)      { return AppendLine(string.Format(format, arg0, arg1, arg2)); }
		public Builder AppendLineFormat(string format, params object[] args)                       { return AppendLine(string.Format(format, args)); }
		public Builder AppendPage()                                                                { using (new ParaWrap(this)) { m_sb.AppendLine("\\page"); } return this; }
		public Builder AppendPara()                                                                { using (new ParaWrap(this)) { m_sb.AppendLine("\\par "); } return this; }
		public Builder AppendRTF(string rtf)                                                       { if (!string.IsNullOrEmpty(rtf)) { m_sb.Append(rtf); } return this; }

		/* this needs work, it has to allow for custom fonts,
		/// <summary>Appends the RTF document.</summary>
		public Builder AppendRTFDocument(string rtf)
		{
			if (string.IsNullOrEmpty(rtf)) return this;
			if (rtf.ColourIndex("viewkind4", StringComparison.Ordinal) > 0)
			{
				try
				{
					string rtfc = GetColoursFromRtf(rtf);
					string rtff = GetFontsFromRtf(rtfc);
					string texttoadd = GetConcentratedText(rtff);
					AppendRTF(texttoadd);
				}
				catch (ArgumentException ex)
				{
					AppendLine("RTF Document ERROR:");
					AppendLine(ex.Message);
				}
			}
			return this;
		}

		private static string GetConcentratedText(string rtf)
		{
			const string find = @"\viewkind4";
			int start, end, end2;
			start = rtf.ColourIndex(find, StringComparison.Ordinal);
			start = rtf.ColourIndex("\\pard", start, StringComparison.Ordinal);
			end   = rtf.LastIndexOf("}", StringComparison.Ordinal);
			end2  = rtf.LastIndexOf("\\par", StringComparison.Ordinal);
			if (end2 > 0 && ((end - end2) < 8)) end = end2;
			return rtf.Substring(start + 5, end - start - 5);
		}

		private string GetColoursFromRtf(string rtf)
		{
			var RegexObj = new Regex("\\{\\\\colortbl ;(?<colour>\\\\red(?<red>\\d{0,3})\\\\green(?<green>\\d{0,3})\\\\blue(?<blue>\\d{0,3});)*\\}");
			Match MatchResults = RegexObj.Match(rtf);
			var replaces = new Dictionary<int, int>();
			while (MatchResults.Success)
			{
				Group GroupObj = MatchResults.Groups["red"];
				if (GroupObj.Success)
				{
					for (int i = 0; i < GroupObj.Captures.Count; i++)
					{
						int redval, greenval, blueval;
						if (int.TryParse(MatchResults.Groups["red"].Captures[i].Value, out redval) &&
							int.TryParse(MatchResults.Groups["green"].Captures[i].Value, out greenval) &&
							int.TryParse(MatchResults.Groups["blue"].Captures[i].Value, out blueval))
						{
							Color c = Color.FromArgb(redval, greenval, blueval);
							int index = ColourIndex(c);
							if ((i + 1) == index) continue;
							replaces.Add(i + 1, index);
						}
					}
				}
				MatchResults = MatchResults.NextMatch();
			}
			if (replaces.Count > 0)
			{
				//delegate string MatchEvaluator(System.Text.RegularExpressions.Match match)
				rtf = Regex.Replace(rtf, "(?:\\\\cf)([0-9]{1,2})", delegate(Match match)
					{
						int val;
						if (int.TryParse(match.Groups[1].Value, out val) && val > 0)
						{
							if (replaces.ContainsKey(val))
							{
								return string.Format(@"\cf{0}", replaces[val]);
							}
						}
						return match.Value;
					});

				//delegate string MatchEvaluator(System.Text.RegularExpressions.Match match)
				rtf = Regex.Replace(rtf, "(?:\\\\highlight)([0-9]{1,2})", delegate(Match match)
					{
						int val;
						if (int.TryParse(match.Groups[1].Value, out val) && val > 0)
						{
							if (replaces.ContainsKey(val))
							{
								return string.Format(@"\highlight{0}", replaces[val]);
							}
						}
						return match.Value;
					});
			}
			return rtf;
		}

		private string GetFontsFromRtf(string rtf)
		{
			// Regex that almost works for MS Word
			// \{\\fonttbl(?<raw>\{\\f(?<findex>\d{1,3})(\\fbidi )?\\(?<fstyle>fswiss|fnil|froman|fmodern|fscript|fdecor)\\fcharset(?<fcharset>\d{1,3}) ?(?<fprq>\\fprq\d{1,3} ?)(\{\\\*\\panose [\dabcdef]{20}\}\d?)?(?<FONT>[@\w -]+)(\\'\w\w)?(\{\\\*\\falt \\'\w\w\\'\w\w\\'\w\w\\'\w\w\})?;\}(\r\n)?)+
			var RegexObj = new Regex(@"\{\\fonttbl(?<raw>\{\\f(?<findex>\d{1,3}) ?\\(?<fstyle>fswiss|fnil|froman|fmodern|fscript|fdecor|ftech|fbidi) ?(?<fprq>\\fprq\d{1,2} ?)?(?<fcharset>\\fcharset\d{1,3})? (?<FONT>[\w -]+);\})+\}\r?\n", RegexOptions.Multiline);
			Match MatchResults = RegexObj.Match(rtf);
			var replaces = new Dictionary<int, int>();
			Group GroupObj = MatchResults.Groups["raw"];
			for (int i = 0; i < GroupObj.Captures.Count; i++)
			{
				string raw = GroupObj.Captures[i].Value;
				//string font = MatchResults.Groups["FONT"].Captures[newdocindex].Value;
				//Capture cap = MatchResults.Groups["findex"].Captures[i];

				//have to replace findex with {0}
				raw = string.Concat("{", Regex.Replace(raw, "\\{\\\\f\\d{1,3}", "{\\f{0}"), "}");

				string curdocstringindex = MatchResults.Groups["findex"].Captures[i].Value;

				int sourceindex;
				if (int.TryParse(curdocstringindex, out sourceindex))
				{
					int destinationindex = FontIndex(raw);
					if (destinationindex != sourceindex)
					{
						replaces.Add(sourceindex, destinationindex);
					}
				}
			}
			if (replaces.Count > 0)
			{
				rtf = Regex.Replace(rtf, "(?<!\\{)(\\\\f)(\\d{1,3})( ?|\\\\)", match =>
					{
						int sourceindex2;
						if (int.TryParse(match.Groups[2].Value, out sourceindex2))
						{
							if (replaces.ContainsKey(sourceindex2))
							{
								string rep = string.Format("\\f{0}{1}", replaces[sourceindex2], match.Groups[3].Value);
								return rep;
							}
						}
						return match.Value;
					});
			}
			return rtf;
		}*/

		public IEnumerable<Builder> EnumerateCells(RowDefinition row_def, CellDefinition[] cell_def)
		{
			using (IRow ie = CreateRow(row_def, cell_def))
			{
				IEnumerator<IBuilderContent> ie2 = ie.GetEnumerator();
				while (ie2.MoveNext())
				{
					using (IBuilderContent item = ie2.Current)
					{
						yield return item.Content;
					}
				}
			}
		}

		/// <summary>Converts a known font into rtf font definition</summary>
		private static string FontString(EFont font, int index)
		{
			string s;
			switch (font)
			{
			default: throw new ArgumentOutOfRangeException("font");
			case EFont.Arial:             s = @"{{\f{0}\fswiss\fprq2\fcharset0 Arial;}}"; break;
			case EFont.ArialBlack:        s = @"{{\f{0}\fswiss\fprq2\fcharset0 Arial Black;}}"; break;
			case EFont.BookmanOldStyle:   s = @"{{\f{0}\froman\fprq2\fcharset0 Bookman Old Style;}}"; break;
			case EFont.Broadway:          s = @"{{\f{0}\fdecor\fprq2\fcharset0 Broadway;}}"; break;
			case EFont.CenturyGothic:     s = @"{{\f{0}\fswiss\fprq2\fcharset0 Century Gothic;}}"; break;
			case EFont.Consolas:          s = @"{{\f{0}\fmodern\fprq1\fcharset0 Consolas;}}"; break;
			case EFont.CordiaNew:         s = @"{{\f{0}\fswiss\fprq2\fcharset0 Cordia New;}}"; break;
			case EFont.CourierNew:        s = @"{{\f{0}\fmodern\fprq1\fcharset0 Courier New;}}"; break;
			case EFont.FontTimesNewRoman: s = @"{{\f{0}\froman\fcharset0 Times New Roman;}}"; break;
			case EFont.Garamond:          s = @"{{\f{0}\froman\fprq2\fcharset0 Garamond;}}"; break;
			case EFont.Georgia:           s = @"{{\f{0}\froman\fprq2\fcharset0 Georgia;}}"; break;
			case EFont.Impact:            s = @"{{\f{0}\fswiss\fprq2\fcharset0 Impact;}}"; break;
			case EFont.LucidaConsole:     s = @"{{\f{0}\fmodern\fprq1\fcharset0 Lucida Console;}}"; break;
			case EFont.MSSansSerif:       s = @"{{\f{0}\fswiss\fprq2\fcharset0 MS Reference Sans Serif;}}"; break;
			case EFont.Symbol:            s = @"{{\f{0}\ftech\fcharset0 Symbol;}}"; break;
			case EFont.WingDings:         s = @"{{\f{0}\fnil\fprq2\fcharset2 Wingdings;}}"; break;
			}
			return string.Format(s, index);
		}

		/// <summary>Returns the index for 'font' in the font table. 'font' is added if its a new one.</summary>
		private int FontIndex(EFont font)
		{
			var idx = m_font_table.IndexOf(font);
			if (idx >= 0) return idx;
			m_font_table.Add(font);
			return m_font_table.Count - 1;
		}

		///// <summary>Returns the index for 'font' in the font table. 'font' is added if its a new one.</summary>
		//private int FontIndex(string font)
		//{
		//    if (string.IsNullOrEmpty(font)) return 0;
		//    int index = m_raw_fonts.ColourIndex(font);
		//    return index < 0 ? m_raw_fonts.Add(font) : index;
		//}

		/// <summary>Gets the index of the colour. Important for merging ColorTables</summary>
		private int ColourIndex(Color colour)
		{
			var idx = m_color_table.IndexOf(colour);
			if (idx >= 0) return idx;
			m_color_table.Add(colour);
			return m_color_table.Count - 1;
		}

		/// <summary>Inserts the image.</summary>
		public Builder InsertImage(Image image)
		{
			new EmbedImage(this).InsertImage(image);
			return this;
		}

		[DebuggerStepThrough]
		public Builder PrependLineFormatIf(string format, ref bool appended, object arg0)
		{
			return PrependLineIf(string.Format(format, arg0), ref appended);
		}

		[DebuggerStepThrough]
		public Builder PrependLineFormatIf(string format, ref bool appended, params object[] args)
		{
			string formated = string.Format(format, args);
			return PrependLineIf(formated, ref appended);
		}

		[DebuggerStepThrough]
		public Builder PrependLineFormatIf(string format, ref bool appended, object arg0, object arg1, object arg2)
		{
			string formated = string.Format(format, arg0, arg1, arg2);
			return PrependLineIf(formated, ref appended);
		}

		[DebuggerStepThrough]
		public Builder PrependLineIf(ref bool appended)
		{
			if (appended)
			{
				AppendLine();
			}
			appended = true;
			return this;
		}

		[DebuggerStepThrough]
		public Builder PrependLineIf(string value, ref bool appended)
		{
			if (appended)
			{
				AppendLine();
				Append(value);
			}
			else
			{
				Append(value);
			}
			appended = true;
			return this;
		}

		public Builder Reset()
		{
			m_sb.AppendLine("\\pard");
			return this;
		}

		public IDisposable FormatLock()
		{
			return new BuilderUnwrapped(this);
		}

		public IRow CreateRow(RowDefinition rowDefinition, CellDefinition[] cellDefinitions)
		{
			return new Row(this, rowDefinition, cellDefinitions);
		}

		/// <summary>Escapes and sanitises a string</summary>
		private string Sanitise(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;
				
			// Escape any characters that need escaping
			if (value.IndexOfAny(CharsNeedingEscaping) >= 0)
				value = value.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
				
			// Replace any Unicode characters with their \u1234 value
			if (value.Any(x => x > 255))
			{
				var sb = new StringBuilder(value.Length + 20);
				foreach (char t in value)
				{
					if (t <= 255) sb.Append(t);
					else
					{
						sb.Append("\\u");
						sb.Append((int) t);
						sb.Append("?");
					}
				}
				value = sb.ToString();
			}
			return value;
		}

		/// <summary>Wraps Builder for formatting changes allowing injection of appropriate rtf codes to revert format after each Append (string) call</summary>
		private class FormatWrap :IDisposable
		{
			private readonly Builder m_builder;

			public FormatWrap(Builder builder)
			{
				m_builder = builder;
				if (m_builder.m_unwrapped)
					return;

				StringBuilder sb = m_builder.m_sb;
				int len = m_builder.m_sb.Length;

				if      (m_builder.m_sf.Alignment == StringAlignment.Center) sb.Append("\\qc");
				else if (m_builder.m_sf.Alignment == StringAlignment.Far   ) sb.Append("\\qr");

				if ((m_builder.FontStyle & FontStyle.Bold     ) == FontStyle.Bold     ) sb.Append("\\b");
				if ((m_builder.FontStyle & FontStyle.Italic   ) == FontStyle.Italic   ) sb.Append("\\i");
				if ((m_builder.FontStyle & FontStyle.Underline) == FontStyle.Underline) sb.Append("\\ul");
				if ((m_builder.FontStyle & FontStyle.Strikeout) == FontStyle.Strikeout) sb.Append("\\strike");

				if (Math.Abs(m_builder.FontSize - m_builder.DefaultFontSize) > float.Epsilon)
				{
					sb.AppendFormat("\\fs{0}", m_builder.FontSize);
				}
				if (m_builder.m_font_index != 0)
				{
					sb.AppendFormat("\\f{0}", m_builder.m_font_index);
				}
				if (m_builder.ForeColour != m_builder.DefaultForeColour)
				{
					sb.AppendFormat("\\cf{0}", m_builder.ColourIndex(m_builder.ForeColour));
				}
				if (m_builder.BackColour != m_builder.DefaultBackColour)
				{
					sb.AppendFormat("\\highlight{0}", m_builder.ColourIndex(m_builder.BackColour));
				}

				if (sb.Length > len)
				{
					sb.Append(" ");
				}
			}

			~FormatWrap()
			{
				Dispose(false);
			}

			private void Dispose(bool disposing)
			{
				if (m_builder != null && !m_builder.m_unwrapped)
				{
					StringBuilder sb = m_builder.m_sb;

					int len = sb.Length;
					if ((m_builder.FontStyle & FontStyle.Bold) == FontStyle.Bold)
					{
						sb.Append("\\b0");
					}
					if ((m_builder.FontStyle & FontStyle.Italic) == FontStyle.Italic)
					{
						sb.Append("\\i0");
					}
					if ((m_builder.FontStyle & FontStyle.Underline) == FontStyle.Underline)
					{
						sb.Append("\\ulnone");
					}
					if ((m_builder.FontStyle & FontStyle.Strikeout) == FontStyle.Strikeout)
					{
						sb.Append("\\strike0");
					}

					m_builder.FontStyle = FontStyle.Regular;

					if (Math.Abs(m_builder.FontSize - m_builder.DefaultFontSize) > float.Epsilon)
					{
						m_builder.FontSize = m_builder.DefaultFontSize;
						sb.AppendFormat("\\fs{0} ", m_builder.DefaultFontSize);
					}
					if (m_builder.m_font_index != 0)
					{
						sb.Append("\\f0");
						m_builder.m_font_index = 0;
					}

					if (m_builder.ForeColour != m_builder.DefaultForeColour)
					{
						m_builder.ForeColour = m_builder.DefaultForeColour;
						sb.Append("\\cf0");
					}
					if (m_builder.BackColour != m_builder.DefaultBackColour)
					{
						m_builder.BackColour = m_builder.DefaultBackColour;
						sb.Append("\\highlight0");
					}
					//if (m_builder._alignment != StringAlignment.Near )
					//{
					//    m_builder._alignment = StringAlignment.Near;
					//    sb.Append("\\ql");
					//}
					if (sb.Length > len)
					{
						sb.Append(" ");
					}
				}
				if (disposing)
				{
					GC.SuppressFinalize(this);
				}
			}

			public void Dispose()
			{
				Dispose(true);
			}
		}
			
		/// <summary>Exposes an underlying Builder</summary>
		public interface IBuilderContent :IDisposable
		{
			Builder Content { get; }
		}

		/// <summary>Cell In Table Row</summary>
		public class Cell :IBuilderContent
		{
			private readonly CellDefinition m_cell_def;
			private Builder m_builder;
			private bool m_first_access_content;

			public Cell(Builder builder, CellDefinition cell_def)
			{
				m_builder = builder;
				m_cell_def = cell_def;
				m_first_access_content = true;
			}
			~Cell()
			{
				Dispose(false);
			}

			protected void Dispose(bool disposing)
			{
				if (disposing && m_builder != null)
				{
					m_builder.m_sb.AppendLine("\\cell ");
				}
				m_builder = null;
				if (disposing)
				{
					GC.SuppressFinalize(this);
				}
			}

			public void Dispose()
			{
				Dispose(true);
			}

			public Builder Content
			{
				get
				{
					if (m_first_access_content)
					{
						//par in table
						switch (m_cell_def.Alignment)
						{
						case ECellAlignment.TopCenter:
						case ECellAlignment.BottomCenter:
						case ECellAlignment.MiddleCenter: m_builder.m_sb.Append("\\qc "); break;
						case ECellAlignment.TopLeft:
						case ECellAlignment.MiddleLeft:
						case ECellAlignment.BottomLeft:  m_builder.m_sb.Append("\\ql "); break;
						case ECellAlignment.TopRight:
						case ECellAlignment.BottomRight:
						case ECellAlignment.MiddleRight: m_builder.m_sb.Append("\\qr "); break;
						}
						m_first_access_content = false;
					}
					return m_builder;
				}
			}
		}

		/// <summary>Cancels persistent Formatting Changes on an unwrapped Builder exposed by the FormatLock on Builder</summary>
		private class BuilderUnwrapped :IDisposable
		{
			private readonly Builder m_builder;
			private readonly FormatWrap m_wrapped;

			public BuilderUnwrapped(Builder builder)
			{
				m_wrapped = new FormatWrap(builder);
				m_builder = builder;
				m_builder.m_unwrapped = true;
			}
			~BuilderUnwrapped()
			{
				Dispose(false);
			}
			private void Dispose(bool disposing)
			{
				if (m_builder != null)
				{
					m_wrapped.Dispose();
					m_builder.m_unwrapped = false;
				}
				if (disposing)
				{
					GC.SuppressFinalize(this);
				}
			}
			public void Dispose()
			{
				Dispose(true);
			}
		}

		/// <summary>Wraps Builder injecting appropriate rtf codes after paragraph append</summary>
		private class ParaWrap :IDisposable
		{
			private readonly Builder m_builder;

			public ParaWrap(Builder builder)
			{
				m_builder = builder;
				int len = m_builder.m_sb.Length;
				if      (m_builder.m_sf.Alignment == StringAlignment.Center) m_builder.m_sb.Append("\\qc");
				else if (m_builder.m_sf.Alignment == StringAlignment.Far   ) m_builder.m_sb.Append("\\qr");
				if (m_builder.LineIndentFirst >   0) m_builder.m_sb.Append("\\fi" + m_builder.LineIndentFirst);
				if (m_builder.LineIndent      >   0) m_builder.m_sb.Append("\\li" + m_builder.LineIndent);
				if (m_builder.m_sb.Length     > len) m_builder.m_sb.Append(" ");
			}
			~ParaWrap()
			{
				Dispose(false);
			}
			private void Dispose(bool disposing)
			{
				if (m_builder != null && !m_builder.m_unwrapped)
				{
					if (m_builder.m_sf.Alignment != StringAlignment.Near || m_builder.LineIndent > 0 || m_builder.LineIndentFirst > 0)
					{
						m_builder.LineIndentFirst = 0;
						m_builder.LineIndent = 0;
						m_builder.m_sf.Alignment = StringAlignment.Near;
						m_builder.m_sb.Append("\\pard ");
					}
				}
				if (disposing)
				{
					GC.SuppressFinalize(this);
				}
			}
			public void Dispose()
			{
				Dispose(true);
			}
		}

		/// <summary>Injects Cell Rtf Codes</summary>
		private class CellDefinitionBuilder
		{
			private readonly Builder m_builder;
			private readonly StringBuilder m_sb;
			private readonly CellDefinition m_cell_def;

			internal CellDefinitionBuilder(Builder builder, StringBuilder sb, CellDefinition cell_def)
			{
				m_builder = builder;
				m_sb = sb;
				m_cell_def = cell_def;

				CellAlignment();
				TableCellBorderSide();
				//Pad();

				m_sb.AppendFormat("\\cellx{0}", (int) (m_cell_def.CellWidth*TWIPSA4) + m_cell_def.X);
				//_definitionBuilder.AppendFormat("\\clwWidth{0}", m_cell_def.CellWidth);

				//_definitionBuilder.Append("\\cltxlrtb\\clFitText");

				m_sb.AppendLine();

				//Cell text flow
			}

			public CellDefinition CellDefinition
			{
				get { return m_cell_def; }
			}
				
			private string BorderDef()
			{
				var sb = new StringBuilder();
				sb.Append((m_cell_def.BorderSide & ECellBorderSide.DoubleThickness) == ECellBorderSide.DoubleThickness ? "\\brdrth" : "\\brdrs");
					
				if ((m_cell_def.BorderSide & ECellBorderSide.DoubleBorder) == ECellBorderSide.DoubleBorder)
				{
					sb.Append("\\brdrdb");
				}
				sb.Append("\\brdrw");
				sb.Append(m_cell_def.BorderWidth);

				sb.Append("\\brdrcf");
				sb.Append(m_builder.ColourIndex(m_cell_def.BorderColour));

				return sb.ToString();
			}

			private void CellAlignment()
			{
				switch (m_cell_def.Alignment)
				{
				case ECellAlignment.BottomCenter:
				case ECellAlignment.BottomLeft:
				case ECellAlignment.BottomRight:
					m_sb.Append("\\clvertalb"); //\\qr
					break;
				case ECellAlignment.MiddleCenter:
				case ECellAlignment.MiddleLeft:
				case ECellAlignment.MiddleRight:
					m_sb.Append("\\clvertalc"); //\\qr
					break;
				case ECellAlignment.TopCenter:
				case ECellAlignment.TopLeft:
				case ECellAlignment.TopRight:
					m_sb.Append("\\clvertalt"); //\\qr
					break;
				}
			}

			private void Pad()
			{
				if (m_cell_def.Padding != Padding.Empty)
				{
					StringBuilder sb = m_sb;
					sb.AppendFormat("\\clpadfl3\\clpadl{0}", m_cell_def.Padding.Left);
					sb.AppendFormat("\\clpadlr3\\clpadr{0}", m_cell_def.Padding.Right);
					sb.AppendFormat("\\clpadlt3\\clpadt{0}", m_cell_def.Padding.Top);
					sb.AppendFormat("\\clpadlb3\\clpadb{0}", m_cell_def.Padding.Bottom);
				}
			}

			private void TableCellBorderSide()
			{
				ECellBorderSide _rTFBorderSide = m_cell_def.BorderSide;

				if (_rTFBorderSide != ECellBorderSide.None)
				{
					StringBuilder sb = m_sb;
					string bd = BorderDef();
					if (_rTFBorderSide == ECellBorderSide.None)
					{
						sb.Append("\\brdrnil");
					}
					else
					{
						if ((_rTFBorderSide & ECellBorderSide.Left) == ECellBorderSide.Left)
						{
							sb.Append("\\clbrdrl").Append(bd);
						}
						if ((_rTFBorderSide & ECellBorderSide.Right) == ECellBorderSide.Right)
						{
							sb.Append("\\clbrdrr").Append(bd);
						}
						if ((_rTFBorderSide & ECellBorderSide.Top) == ECellBorderSide.Top)
						{
							sb.Append("\\clbrdrt").Append(bd);
						}
						if ((_rTFBorderSide & ECellBorderSide.Bottom) == ECellBorderSide.Bottom)
						{
							sb.Append("\\clbrdrb").Append(bd);
						}
					}
				}
			}
		}

		public class EmbedImage
		{
			// ReSharper disable UnusedMember.Local
				
			// Not used in this application.  Descriptions can be found with documentation
			// of Windows GDI function SetMapMode
			private const string FF_UNKNOWN = "UNKNOWN";

			// The number of hundredths of millimeters (0.01 mm) in an inch
			// For more information, see GetImagePrefix() method.
			private const int HMM_PER_INCH = 2540;
			private const int MM_ANISOTROPIC = 8;
			private const int MM_HIENGLISH = 5;
			private const int MM_HIMETRIC = 3;
			private const int MM_ISOTROPIC = 7;
			private const int MM_LOENGLISH = 4;
			private const int MM_LOMETRIC = 2;
			private const int MM_TEXT = 1;
			private const int MM_TWIPS = 6;

			// Ensures that the metafile maintains a 1:1 aspect ratio

			// The number of twips in an inch
			// For more information, see GetImagePrefix() method.
			private const int TWIPS_PER_INCH = 1440;
			// ReSharper restore UnusedMember.Local

			private readonly Builder m_builder;
			private readonly StringBuilder m_sb;
			private const string RTF_IMAGE_POST = "}\r\n";

			public EmbedImage(Builder builder)
			{
				m_builder = builder;
				m_sb = new StringBuilder();
			}

			/// <summary>
			/// Use the EmfToWmfBits function in the GDI+ specification to convert a
			/// Enhanced Metafile to a Windows Metafile
			/// </summary>
			/// <param name="_hEmf">
			/// A handle to the Enhanced Metafile to be converted
			/// </param>
			/// <param name="_bufferSize">
			/// The size of the buffer used to store the Windows Metafile bits returned
			/// </param>
			/// <param name="_buffer">
			/// An array of bytes used to hold the Windows Metafile bits returned
			/// </param>
			/// <param name="_mappingMode">
			/// The mapping mode of the image.  This control uses MM_ANISOTROPIC.
			/// </param>
			/// <param name="_flags">
			/// Flags used to specify the format of the Windows Metafile returned
			/// </param>
			[DllImport("gdiplus.dll")]
			private static extern uint GdipEmfToWmfBits(IntPtr _hEmf, uint _bufferSize, byte[] _buffer, int _mappingMode, EmfToWmfBitsFlags _flags);

			public void InsertImage(Image image)
			{
				// The horizontal resolution at which the control is being displayed
				float xDpi;

				// The vertical resolution at which the control is being displayed
				float yDpi;

				using (Graphics graphics = Graphics.FromImage(image))
				{
					xDpi = graphics.DpiX;
					yDpi = graphics.DpiY;
				}

				// Create the image control string and append it to the RTF string
				WriteImagePrefix(image, xDpi, yDpi);

				// Create the Windows Metafile and append its bytes in HEX format
				WriteRtfImage(image);

				// Close the RTF image control string
				m_sb.Append(RTF_IMAGE_POST);

				m_builder.m_sb.Append(m_sb.ToString());
			}

			/// <summary>
			/// Creates the RTF control string that describes the image being inserted.
			/// This description (in this case) specifies that the image is an
			/// MM_ANISOTROPIC metafile, meaning that both X and Y axes can be scaled
			/// independently.  The control string also gives the images current dimensions,
			/// and its target dimensions, so if you want to control the size of the
			/// image being inserted, this would be the place to do it. The prefix should
			/// have the form ...
			///
			/// {\pict\wmetafile8\picw[A]\pich[B]\picwgoal[C]\pichgoal[D]
			///
			/// where ...
			///
			/// A   = current width of the metafile in hundredths of millimeters (0.01mm)
			///     = Image Width in Inches * Number of (0.01mm) per inch
			///     = (Image Width in Pixels / Graphics Context's Horizontal Resolution) * 2540
			///     = (Image Width in Pixels / Graphics.DpiX) * 2540
			///
			/// B   = current height of the metafile in hundredths of millimeters (0.01mm)
			///     = Image Height in Inches * Number of (0.01mm) per inch
			///     = (Image Height in Pixels / Graphics Context's Vertical Resolution) * 2540
			///     = (Image Height in Pixels / Graphics.DpiX) * 2540
			///
			/// C   = target width of the metafile in twips
			///     = Image Width in Inches * Number of twips per inch
			///     = (Image Width in Pixels / Graphics Context's Horizontal Resolution) * 1440
			///     = (Image Width in Pixels / Graphics.DpiX) * 1440
			///
			/// D   = target height of the metafile in twips
			///     = Image Height in Inches * Number of twips per inch
			///     = (Image Height in Pixels / Graphics Context's Horizontal Resolution) * 1440
			///     = (Image Height in Pixels / Graphics.DpiX) * 1440
			///
			/// </summary>
			/// <remarks>
			/// The Graphics Context's resolution is simply the current resolution at which
			/// windows is being displayed.  Normally it's 96 dpi, but instead of assuming
			/// I just added the code.
			///
			/// According to Ken Howe at pbdr.com, "Twips are screen-independent units
			/// used to ensure that the placement and proportion of screen elements in
			/// your screen application are the same on all display systems."
			///
			/// Units Used
			/// ----------
			/// 1 Twip = 1/20 Point
			/// 1 Point = 1/72 Inch
			/// 1 Twip = 1/1440 Inch
			///
			/// 1 Inch = 2.54 cm
			/// 1 Inch = 25.4 mm
			/// 1 Inch = 2540 (0.01)mm
			/// </remarks>
			private void WriteImagePrefix(Image _image, float xDpi, float yDpi)
			{
				// Get the horizontal and vertical resolutions at which the object is
				// being displayed

				// Calculate the current width of the image in (0.01)mm
				var picw = (int) Math.Round((_image.Width/xDpi)*HMM_PER_INCH);

				// Calculate the current height of the image in (0.01)mm
				var pich = (int) Math.Round((_image.Height/yDpi)*HMM_PER_INCH);

				// Calculate the target width of the image in twips
				var picwgoal = (int) Math.Round((_image.Width/xDpi)*TWIPS_PER_INCH);

				// Calculate the target height of the image in twips
				var pichgoal = (int) Math.Round((_image.Height/yDpi)*TWIPS_PER_INCH);

				// Append values to RTF string
				m_sb.Append(@"{\pict\wmetafile8");
				m_sb.Append(@"\picw");
				m_sb.Append(picw);
				m_sb.Append(@"\pich");
				m_sb.Append(pich);
				m_sb.Append(@"\picwgoal");
				m_sb.Append(picwgoal);
				m_sb.Append(@"\pichgoal");
				m_sb.Append(pichgoal);
				m_sb.Append(" ");
			}

			/// <summary>
			/// Wraps the image in an Enhanced Metafile by drawing the image onto the
			/// graphics context, then converts the Enhanced Metafile to a Windows
			/// Metafile, and finally appends the bits of the Windows Metafile in HEX
			/// to a string and returns the string.</summary>
			private void WriteRtfImage(Image image)
			{
				// Used to store the enhanced metafile
				MemoryStream _stream = null;

				// Used to create the metafile and draw the image
				Graphics _graphics = null;

				// The enhanced metafile
				Metafile _metaFile = null;

				// Handle to the device context used to create the metafile
				IntPtr _hdc;

				try
				{
					using (_stream = new MemoryStream())
					{
						// Get a graphics context from the RichTextBox
						using (_graphics = Graphics.FromImage(image))
						{
							// Get the device context from the graphics context
							_hdc = _graphics.GetHdc();

							// Create a new Enhanced Metafile from the device context
							_metaFile = new Metafile(_stream, _hdc);

							// Release the device context
							_graphics.ReleaseHdc(_hdc);
						}

						// Get a graphics context from the Enhanced Metafile
						using (_graphics = Graphics.FromImage(_metaFile))
						{
							// Draw the image on the Enhanced Metafile
							_graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
						}
						byte[] _buffer = null;
						using (_metaFile)
						{
							// Get the handle of the Enhanced Metafile
							IntPtr _hEmf = _metaFile.GetHenhmetafile();

							// A call to EmfToWmfBits with a null buffer return the size of the
							// buffer need to store the WMF bits.  Use this to get the buffer
							// size.
							uint _bufferSize = GdipEmfToWmfBits(_hEmf, 0, null, MM_ANISOTROPIC, EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);

							// Create an array to hold the bits
							_buffer = new byte[_bufferSize];

							// A call to EmfToWmfBits with a valid buffer copies the bits into the
							// buffer an returns the number of bits in the WMF.
							uint _convertedSize = GdipEmfToWmfBits(_hEmf, _bufferSize, _buffer, MM_ANISOTROPIC, EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);
						}
						// Append the bits to the RTF string
						for (int i = 0; i < _buffer.Length; ++i)
						{
							m_sb.Append(String.Format("{0:X2}", _buffer[i]));
						}
						if (_stream != null)
						{
							_stream.Flush();
							_stream.Close();
						}
					}
				}
				finally
				{
					if (_graphics != null)
					{
						_graphics.Dispose();
					}
					if (_metaFile != null)
					{
						_metaFile.Dispose();
					}
					if (_stream != null)
					{
						_stream.Flush();
						_stream.Close();
					}
				}
			}

			// ReSharper disable UnusedMember.Local
			private enum EmfToWmfBitsFlags
			{
				// Use the default conversion
				EmfToWmfBitsFlagsDefault = 0x00000000,

				// Embedded the source of the EMF metafiel within the resulting WMF
				// metafile
				EmfToWmfBitsFlagsEmbedEmf = 0x00000001,

				// Place a 22-byte header in the resulting WMF file.  The header is
				// required for the metafile to be considered placeable.
				EmfToWmfBitsFlagsIncludePlaceable = 0x00000002,

				// Don't simulate clipping by using the XOR operator.
				EmfToWmfBitsFlagsNoXORClip = 0x00000004
			};
		}
		// ReSharper restore UnusedMember.Local

		internal const int TWIPSA4 = 11907;
		internal const int TWIPSA4V = 16840;

		/// <summary>Row Interface</summary>
		public interface IRow :IDisposable, IEnumerable<IBuilderContent> {}

		/// <summary>Rich Table Row</summary>
		private class Row :IRow
		{
			private readonly List<CellDefinitionBuilder> m_cells;
			private readonly RowDefinition m_row_def;
			private readonly CellDefinition[] m_cell_def;
			private readonly StringBuilder m_sb;
			private Builder m_builder;

			public Row(Builder builder, RowDefinition row_def, CellDefinition[] cell_def)
			{
				m_sb       = new StringBuilder();
				m_row_def  = row_def;
				m_cell_def = cell_def;
				m_builder  = builder;

				m_sb.Append("\\trowd\\trgaph115\\trleft-115");
				m_sb.AppendLine("\\trautofit1"); //AutoFit: ON
				TableCellBorderSide();
				BorderDef();
				//Pad();

				// \trhdr   Table row header. This row should appear at the top of every page on which the current table appears.
				// \trkeep  Keep table row together. This row cannot be split by a page break. This property is assumed to be off unless the control word is present.
				//\trleftN  Position in twips of the leftmost edge of the table with respect to the left edge of its column.
				//\trqc Centers a table row with respect to its containing column.
				//\trql Left-justifies a table row with respect to its containing column.
				//\trqr Right-justifies a table row with respect to its containing column.
				//\trrhN    Height of a table row in twips. When 0, the height is sufficient for all the text in the line; when positive, the height is guaranteed to be at least the specified height; when negative, the absolute value of the height is used, regardless of the height of the text in the line.
				//\trpaddbN Default bottom cell margin or padding for the row.
				//\trpaddlN Default left cell margin or padding for the row.
				//\trpaddrN Default right cell margin or padding for the row.
				//\trpaddtN Default top cell margin or padding for the row.
				//\trpaddfbN    Units for \trpaddbN:
				//0 Null. Ignore \trpaddbN in favor of \trgaphN (Word 97 style padding).
				//3 Twips.
				//\trpaddflN    Units for \trpaddlN:
				//0 Null. Ignore \trpaddlN in favor of \trgaphN (Word 97 style padding).
				//3 Twips.
				//\trpaddfrN    Units for \trpaddrN:
				//0 Null. Ignore \trpaddrN in favor of \trgaphN (Word 97 style padding).
				//3 Twips.
				//\trpaddftN    Units for \trpaddtN:
				//0 Null. Ignore \trpaddtN in favor of \trgaphN (Word 97 style padding).
				//3 Twips.

				m_cells = new List<CellDefinitionBuilder>();
				int x = 0;
				foreach (var item in m_cell_def)
				{
					item.X = x;
					x += (int)(item.CellWidth*TWIPSA4);
					m_cells.Add(new CellDefinitionBuilder(m_builder, m_sb, item));
				}
				m_builder.m_sb.Append(m_sb.ToString());
			}
			~Row()
			{
				Dispose(false);
			}

			private string BorderDef()
			{
				var sb = new StringBuilder();
				ECellBorderSide _rTFBorderSide = m_row_def.BorderSide;
				if ((_rTFBorderSide & ECellBorderSide.DoubleThickness) == ECellBorderSide.DoubleThickness)
				{
					sb.Append("\\brdrth");
				}
				else
				{
					sb.Append("\\brdrs");
				}
				if ((_rTFBorderSide & ECellBorderSide.DoubleBorder) == ECellBorderSide.DoubleBorder)
				{
					sb.Append("\\brdrdb");
				}
				sb.Append("\\brdrw");
				sb.Append(m_row_def.BorderWidth);

				sb.AppendFormat("\\brdrc{0}", m_builder.ColourIndex(m_row_def.BorderColour));
				sb.AppendLine();

				return sb.ToString();
			}

			private void Dispose(bool disposing)
			{
				if (m_builder != null)
				{
					m_builder.m_sb.AppendLine("\\row");
					//_builder._sb.AppendLine();
					m_builder.m_sb.AppendLine("{");
					m_builder.m_sb.Append(m_sb.ToString());

					m_builder.m_sb.AppendLine("}");
				}
				m_builder = null;
				if (disposing)
				{
					GC.SuppressFinalize(this);
				}
			}

			private void Pad()
			{
				if (m_row_def.Padding != Padding.Empty)
				{
					StringBuilder sb = m_sb;
					sb.AppendFormat("\\trpaddfl3\\trpaddl{0}", m_row_def.Padding.Left);
					sb.AppendFormat("\\trpaddfr3\\trpaddr{0}", m_row_def.Padding.Right);
					sb.AppendFormat("\\trpaddft3\\trpaddt{0}", m_row_def.Padding.Top);
					sb.AppendFormat("\\trpaddfb3\\trpaddb{0}", m_row_def.Padding.Bottom);
				}
			}

			private void TableCellBorderSide()
			{
				ECellBorderSide _rTFBorderSide = m_row_def.BorderSide;

				if (_rTFBorderSide != ECellBorderSide.None)
				{
					StringBuilder sb = m_sb;
					string bd = BorderDef();

					if ((_rTFBorderSide & ECellBorderSide.Left) == ECellBorderSide.Left)
					{
						sb.Append("\\trbrdrl").Append(bd);
					}
					if ((_rTFBorderSide & ECellBorderSide.Right) == ECellBorderSide.Right)
					{
						sb.Append("\\trbrdrr").Append(bd);
					}
					if ((_rTFBorderSide & ECellBorderSide.Top) == ECellBorderSide.Top)
					{
						sb.Append("\\trbrdrt").Append(bd);
					}
					if ((_rTFBorderSide & ECellBorderSide.Bottom) == ECellBorderSide.Bottom)
					{
						sb.Append("\\trbrdrb").Append(bd);
					}
					//sb.Append("\\trbrdrh\\brdrs\\brdrw10");
					//sb.Append("\\trbrdrv\\brdrs\\brdrw10");

					sb.AppendLine();
				}
			}

			public IEnumerator<IBuilderContent> GetEnumerator()
			{
				m_builder.m_sb.AppendLine("\\pard\\intbl\\f0");
				foreach (CellDefinitionBuilder item in m_cells)
				{
					yield return new Cell(m_builder, item.CellDefinition);
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public void Dispose()
			{
				Dispose(true);
			}
		}
	}

	/// <summary>Definition of rich text table row</summary>
	public class RowDefinition
	{
		public ECellAlignment Alignment    { get; private set; }
		public Padding    Padding      { get; private set; }
		public ECellBorderSide BorderSide   { get; private set; }
		public int        BorderWidth  { get; private set; }
		public Color      BorderColour { get; private set; }
		public int        RowWidth     { get; set; }
			
		public RowDefinition(int row_width, ECellAlignment alignment, ECellBorderSide border_side, int border_width, Color border_colour, Padding padding)
		{
			Padding      = padding;
			Alignment    = alignment;
			BorderSide   = border_side;
			BorderWidth  = border_width;
			BorderColour = border_colour;
			RowWidth     = row_width*Builder.TWIPSA4/100;
		}
	}

	/// <summary>Definition Of cell In table row</summary>
	public class CellDefinition
	{
		public ECellAlignment Alignment    { get; private set; }
		public ECellBorderSide BorderSide   { get; private set; }
		public int        BorderWidth  { get; private set; }
		public Color      BorderColour { get; private set; }
		public float      CellWidth    { get; private set; }
		public Padding    Padding      { get; private set; }
		public int        X            { get; set; }

		public CellDefinition(int cell_width, ECellAlignment alignment, ECellBorderSide border_side, int border_width, Color border_color, Padding padding)
		{
			Padding      = padding;
			Alignment    = alignment;
			BorderSide   = border_side;
			BorderWidth  = border_width;
			BorderColour = border_color;
			CellWidth    = (float)cell_width/100;
			X            = 0;
		}
	}

	/// <summary> A Work in Progress</summary>
	private class Util
	{
		public void ParagraphBorderSide(StringBuilder sb, ECellBorderSide rTFBorderSide)
		{
			if (rTFBorderSide == ECellBorderSide.None) return;
			if ((rTFBorderSide & ECellBorderSide.Left  ) == ECellBorderSide.Left  ) sb.Append("\\brdrl");
			if ((rTFBorderSide & ECellBorderSide.Right ) == ECellBorderSide.Right ) sb.Append("\\brdrr");
			if ((rTFBorderSide & ECellBorderSide.Top   ) == ECellBorderSide.Top   ) sb.Append("\\brdrt");
			if ((rTFBorderSide & ECellBorderSide.Bottom) == ECellBorderSide.Bottom) sb.Append("\\brdrb");
			sb.Append((rTFBorderSide & ECellBorderSide.DoubleThickness) == ECellBorderSide.DoubleThickness ? "\\brdrth" : "\\brdrs");
			if ((rTFBorderSide & ECellBorderSide.DoubleBorder) == ECellBorderSide.DoubleBorder) sb.Append("\\brdrdb");
			sb.Append("\\brdrw10");
		}

		public void TableRowBorderSide(StringBuilder sb, ECellBorderSide rTFBorderSide)
		{
			if (rTFBorderSide == ECellBorderSide.None) return;
			if ((rTFBorderSide & ECellBorderSide.Left  ) == ECellBorderSide.Left  ) sb.Append("\\trbrdrl");
			if ((rTFBorderSide & ECellBorderSide.Right ) == ECellBorderSide.Right ) sb.Append("\\trbrdrr");
			if ((rTFBorderSide & ECellBorderSide.Top   ) == ECellBorderSide.Top   ) sb.Append("\\trbrdrt");
			if ((rTFBorderSide & ECellBorderSide.Bottom) == ECellBorderSide.Bottom) sb.Append("\\trbrdrb");
			sb.Append((rTFBorderSide & ECellBorderSide.DoubleThickness) == ECellBorderSide.DoubleThickness ? "\\brdrth" : "\\brdrs");
			if ((rTFBorderSide & ECellBorderSide.DoubleBorder) == ECellBorderSide.DoubleBorder) sb.Append("\\brdrdb");
			sb.Append("\\brdrw10");
		}
	}

	/// <summary>A temporary string builder to save on reallocs</summary>
	private static readonly StringBuilder TmpSB = new StringBuilder();
}

	[Test]
	public static void TestRtfBuilder()
	{
		var rtf = new Rtf.Builder();
		rtf.AppendLine("AppendLine Basic Text");
		rtf.Append("append text1").Append("append text2").Append("append text3").Append("append text4").AppendLine();
		rtf.FontStyle = FontStyle.Bold;
		rtf.AppendLine("Bold");
		rtf.FontStyle = FontStyle.Italic;
		rtf.AppendLine("Italic");
		rtf.FontStyle = FontStyle.Strikeout;
		rtf.AppendLine("Strikeout");
		rtf.FontStyle = FontStyle.Underline;
		rtf.AppendLine("Underline");
		rtf.FontStyle = FontStyle.Bold | FontStyle.Italic | FontStyle.Strikeout | FontStyle.Underline;
		rtf.AppendLine("Underline/Bold/Italic/Underline");
		rtf.ForeColour = Color.FromKnownColor(KnownColor.Red);
		rtf.AppendLine("ForeColour Red");
		rtf.BackColour = Color.FromKnownColor(KnownColor.Yellow);
		rtf.AppendLine("BackColor Yellow");
		rtf.ForeColour = Color.FromKnownColor(KnownColor.Red);
		rtf.BackColour = Color.FromKnownColor(KnownColor.Yellow);
		rtf.AppendLine("ForeColor Red , BackColor Yellow");

		rtf.AppendLine("1. append 2 lines - First Line \r\n2. Second Line (correctly appending rtf codes for secondline)");
		rtf.Font = Rtf.EFont.Georgia;
		rtf.AppendLine("Change to Georgia Font!");
		rtf.Font = Rtf.EFont.Consolas;
		rtf.AppendLine("Change to Consolas Font!");
		rtf.Font = Rtf.EFont.Garamond;
		rtf.AppendLine("Change to Garamond Font!");
		rtf.Font = Rtf.EFont.MSSansSerif;
		rtf.AppendLine("Change to MSSansSerif Font!)");
		rtf.Font = Rtf.EFont.Arial;
		rtf.AppendLine("Change to Arial Font!(default)");
		rtf.Font = Rtf.EFont.ArialBlack;
		rtf.AppendLine("Change to ArialBlack Font!");

		rtf.FontSize = 30;
		rtf.Font = Rtf.EFont.ArialBlack;
		rtf.AppendLine("Change to ArialBlack Font Size 30");

		//Commit Format changes
		rtf.FontSize = 20;
		rtf.Font = Rtf.EFont.ArialBlack;

		using (rtf.FormatLock())
		{
		    rtf.AppendLine("FormatLock to ArialBlack Font Size 20");
		    rtf.AppendLine("FormatLock to ArialBlack Font Size 20");
		}
		rtf.AppendLine("Inserting Image");
		//rtf.InsertImage(Resources.Complications);
		rtf.AppendLine("Inserted Image");
			
		rtf.AppendLine("Creating Table");
		rtf.AppendPage();
		rtf.AppendPara();
		rtf.Reset();
			
		AddRow1(rtf, "Row 1 Cell 1", "Row 1 Cell 2", "Row 1 Cell 3");
		AddRow1(rtf, "Row 2 Cell 1", "Row 2 Cell 2", "Row 2 Cell 3");
		AddRow1(rtf, "Row 3 Cell 1", "Row 3 Cell 2", "Row 3 Cell 3");
		AddRow1(rtf, "Row 4 Cell 1", "Row 4 Cell 2", "Row 4 Cell 3");
		AddRow1(rtf, "Row 5 Cell 1", "Row 5 Cell 2", "Row 5 Cell 3");
			
		rtf.AppendLine("Inserting cell images");
		rtf.AppendPara();
		rtf.Reset();
		rtf.AppendPara();
		rtf.Reset();
			
		AddRow2(rtf, "Row 5 Cell 1", "Row 5 Cell 2", "Row 5 Cell 3");
			
		rtf.AppendPara();
		rtf.AppendLine("Inserting MultiLine Cells --> Fails to display correctly in RichTextBox");
		rtf.Reset();
		rtf.AppendPara();
			
		AddRow1(rtf, "Row 5\r\n Cell 1", "Row 5 \r\n Cell 2", "Row 5 \r\n Cell 3");
		rtf.Reset();
		rtf.AppendPara();
			
		var str = rtf.ToString();
		File.WriteAllText("tmp.rtf", str);
	}
		
	private static void AddRow1(Rtf.Builder rtf, params string[] cellContents)
	{
		Padding p = new Padding { All = 50 };
		var rd  = new Rtf.RowDefinition(88, Rtf.ECellAlignment.TopLeft, Rtf.ECellBorderSide.Default, 15, SystemColors.WindowText, p);
		var cds = new Rtf.CellDefinition[cellContents.Length];
		for (int i = 0; i < cellContents.Length; i++)
		{
		    cds[i] = new Rtf.CellDefinition(88 / cellContents.Length, Rtf.ECellAlignment.TopLeft, Rtf.ECellBorderSide.Default, 15, Color.Blue, Padding.Empty);
		}
		int pos = 0;
		foreach (var item in rtf.EnumerateCells(rd, cds))
		{
		    item.ForeColour = Color.Blue;
		    item.BackColour = Color.Yellow;
		    item.FontStyle = FontStyle.Bold | FontStyle.Underline;
		    item.Append(cellContents[pos++]);
		}
	}

	private static void AddRow2(Rtf.Builder rtf, params string[] cellContents)
	{
		Padding p = new Padding { All = 50 };//ignored
		// create RTFRowDefinition
		var rd = new Rtf.RowDefinition(88, Rtf.ECellAlignment.TopLeft, Rtf.ECellBorderSide.Default, 15, SystemColors.WindowText, p);
		// Create RTFCellDefinitions
		var cds = new Rtf.CellDefinition[cellContents.Length];
		for (int i = 0; i < cellContents.Length; i++)
		{
		    cds[i] = new Rtf.CellDefinition(88 / cellContents.Length, Rtf.ECellAlignment.TopLeft, Rtf.ECellBorderSide.Default, 15, Color.Blue, Padding.Empty);
		}
		int pos = 0;
		// enumerate over cells
		// each cell 
		foreach (var item in rtf.EnumerateCells(rd, cds))
		{
		    //item.InsertImage(Resources.Complications);
		    item.ForeColour = Color.Blue;
		    item.BackColour = Color.Yellow;
		    item.FontStyle = FontStyle.Bold | FontStyle.Underline;
		    item.Append(cellContents[pos++]);
		}
	}
#endif