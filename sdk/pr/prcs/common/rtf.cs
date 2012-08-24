//***************************************************
// Rtf string builder
//  Copyright © Rylogic Ltd 2012
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
			Left    = 0,
			Centre  = 1,
			Right   = 2,
			Justify = 3,
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

			/// <summary>Merges two content objects into one. Return true if the merge is successful, false if 'other' cannot be merged</summary>
			public virtual bool Merge(Content other) { return false; }
		}

		/// <summary>Represents the state of all rtf style control words</summary>
		public class Style
		{
			/// <summary>The default style</summary>
			public static Style Default { get { return m_default; } }
			private static readonly Style m_default = new Style();

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

			/// <summary>The indenting to apply to each line (in 100th's of a character unit)</summary>
			public int LineIndent { get; set; }

			/// <summary>The indenting to apply to the first line of a paragraph (in 100th's of a character unit)</summary>
			public int LineIndentFirst { get; set; }

			/// <summary>Paragraph alignment. Left, centre, right, or justify. Note: justify is not supported by RichTextBox</summary>
			public EAlign Alignment { get; set; }

			public Style() :this(0, 12, EFontStyle.Regular) {}
			public Style(int font_index, int font_size = 12, EFontStyle font_style = EFontStyle.Regular)
			{
				FontIndex       = font_index;
				FontSize        = font_size;
				FontStyle       = font_style;
				BackColourIndex = 0;
				ForeColourIndex = 0;
				LineIndent      = 0;
				LineIndentFirst = 0;
				Alignment       = EAlign.Left;
			}
			public Style(Style rhs)
			{
				FontIndex       = rhs.FontIndex       ;
				FontSize        = rhs.FontSize        ;
				FontStyle       = rhs.FontStyle       ;
				BackColourIndex = rhs.BackColourIndex ;
				ForeColourIndex = rhs.ForeColourIndex ;
				LineIndent      = rhs.LineIndent      ;
				LineIndentFirst = rhs.LineIndentFirst ;
				Alignment       = rhs.Alignment       ;
			}

			/// <summary>Writes control words for the differences in style begin 'prev' and 'next'</summary>
			public static void Write(StrBuild sb, Style next, Style prev)
			{
				if (next.LineIndentFirst != prev.LineIndentFirst) sb.AppendFormat(StrBuild.EType.Control, @"\fi{0}" ,next.LineIndentFirst);
				if (next.LineIndent      != prev.LineIndent     ) sb.AppendFormat(StrBuild.EType.Control, @"\li{0}" ,next.LineIndent);
				
				if (next.Alignment != prev.Alignment)
				{
					if (next.Alignment == EAlign.Left   ) sb.Append(StrBuild.EType.Control, @"\ql");
					if (next.Alignment == EAlign.Centre ) sb.Append(StrBuild.EType.Control, @"\qc");
					if (next.Alignment == EAlign.Right  ) sb.Append(StrBuild.EType.Control, @"\qr");
					if (next.Alignment == EAlign.Justify) sb.Append(StrBuild.EType.Control, @"\qj");
				}
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

		/// <summary>An object for constructing rtf strings</summary>
		public class Builder :Content
		{
			private readonly Root m_root;
			private readonly FontTable m_font_table;
 			private readonly ColourTable m_colour_table;

			/// <summary>The current style used when appending text</summary>
			public Style Style { get; set; }

			public Builder()
			{
				m_root = new Root();
				m_font_table = new FontTable();
				m_colour_table = new ColourTable();
				m_root.AddContent(m_font_table);
				m_root.AddContent(m_colour_table);
				m_font_table.Add(new Font());
				Style = Style.Default;
			}

			/// <summary>Returns the contained content as an rtf string</summary>
			public override string ToString()
			{
 				var sb = new StrBuild();
				ToRtf(sb, this);
				return sb.ToString();
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				m_root.ToRtf(sb, this);
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

			public Builder Append(Style style)                         { Style = style; return this; }
			public Builder Append(Content content)                     { m_root.AddContent(content); return this; }
			public Builder Append(Paragraph para)                      { m_root.AddContent(para); return this; }

			/// <summary>Append a plain text string using the current style</summary>
			public Builder Append(string str)                          { m_root.AddContent(new TextSpan(str, Style)); return this; }
			public Builder Append(string str, int start, int count)    { return Append(str.Substring(start, count)); }
			public Builder Append<T>(T x)                              { return Append(x.ToString()); }
			public Builder AppendLine()                                { return Append("\n"); }
			public Builder AppendLine(string str)                      { return Append(str + "\n"); }
			public Builder AppendLine<T>(T x)                          { return Append(x + "\n"); }
		}

		/// <summary>The rtf header string.</summary>
		private class Root :Content
		{
			/// <summary>Nested content</summary>
			private readonly List<Content> m_groups = new List<Content>();

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

			/// <summary>Add some content to this rtf object</summary>
			public void AddContent(Content group)
			{
				// Try to merge the content to the last one added
				if (m_groups.Count != 0 && m_groups.Last().Merge(group))
					return;
				
				// Otherwise add it
				m_groups.Add(group);
			}

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				// e.g: @"\rtf1\ansi\deff0"; @"\rtf1\ansi\ansicpg1252\deff0\deflang3081";
				sb.AppendFormat(StrBuild.EType.Control, @"{{\rtf{0}\{1}\ansicpg{2}\deff{3}\viewkind{4}{5}{6}" ,Version ,CharSet ,AnsiCodePage ,DefaultFontIndex ,Viewkind ,AdditionalControlWords ,Generator);
				sb.AppendLine(StrBuild.EType.Control, @"\pard");
				foreach (var g in m_groups) g.ToRtf(sb, this);
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
				sb.Append(StrBuild.EType.Control, @"{\fonttbl");
				foreach (var f in m_fonts) f.ToRtf(sb, this);
				sb.AppendLine(StrBuild.EType.Control, @"}");
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
				sb.Append(StrBuild.EType.Control, @"{\colortbl;");
				foreach (var c in m_colours) sb.AppendFormat(StrBuild.EType.Control, @"\red{0}\green{1}\blue{2};" ,c.R ,c.G, c.B);
				sb.AppendLine(StrBuild.EType.Control, @"}");
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
		public class Paragraph :Content
		{
			private readonly List<TextSpan> m_spans = new List<TextSpan>();

			/// <summary>The style that applies to this paragraph</summary>
			public Style Style { get; set; }

			public Paragraph Append(Style style)                         { Style = style; return this; }
			public Paragraph Append(TextSpan content)                    { m_spans.Add(content); return this; }

			/// <summary>Append a plain text string using the current style</summary>
			public Paragraph Append(string str)                          { return Append(new TextSpan(str, Style)); }
			public Paragraph Append(string str, int start, int count)    { return Append(str.Substring(start, count)); }
			public Paragraph Append<T>(T x)                              { return Append(x.ToString()); }
			public Paragraph AppendLine()                                { return Append("\n"); }
			public Paragraph AppendLine(string str)                      { return Append(str + "\n"); }
			public Paragraph AppendLine<T>(T x)                          { return Append(x + "\n"); }

			public Paragraph() :this(Style.Default) {}
			public Paragraph(Style style) { Style = style; }

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				sb.Append(StrBuild.EType.Control, @"\pard");
				Style.Write(sb, Style, Style.Default);
				foreach (var s in m_spans) s.ToRtf(sb, this);
				Style.Write(sb, Style.Default, Style);
				sb.AppendLine(StrBuild.EType.Control, @"\par");
			}
		}

		/// <summary>A block of text that uses a specific style</summary>
		public class TextSpan :Content
		{
			/// <summary>A recycled string builder to reduce allocs</summary>
			private static readonly StringBuilder TmpSB = new StringBuilder();

			/// <summary>The text string</summary>
			public StringBuilder Text { get; set; }

			/// <summary>The style applied to the text</summary>
			public Style Style { get; set; }

			public TextSpan() :this("", Style.Default) {}
			public TextSpan(string text) :this(text, Style.Default) {}
			public TextSpan(string text, Style style) { Text = new StringBuilder(text); Style = style; }

			/// <summary>Writes this object as rtf into the provided string builder</summary>
			public override void ToRtf(StrBuild sb, Content parent)
			{
				var para = parent as Paragraph;
				var parent_style = para != null ? para.Style : Style.Default;
				
				// Sanitise the string
				var sanitry = Sanitise(Text);
				
				Style.Write(sb, Style, parent_style);
				int s,e; for (s = 0; (e = sanitry.IndexOf('\n', s)) != -1; s = e+1) sb.Append(StrBuild.EType.Content, sanitry, s, e-s).AppendLine(StrBuild.EType.Control, @"\line");
				if (s != sanitry.Length) sb.Append(StrBuild.EType.Content, sanitry, s, sanitry.Length - s);
				Style.Write(sb, parent_style, Style);
			}

			/// <summary>Merges two content objects into one. Return true if the merge is successful, false if 'other' cannot be merged</summary>
			public override bool Merge(Content other)
			{
				// We can merge text spans if they share the same style
				var rhs = other as TextSpan;
				if (rhs == null || !ReferenceEquals(Style, rhs.Style)) return false;
				Text.Append(rhs.Text.ToString());
				return true;
			}

			/// <summary>Escapes and sanitises a plain text string</summary>
			private string Sanitise(StringBuilder value)
			{
				TmpSB.Clear();
				TmpSB.EnsureCapacity(value.Length + 100);

				for (int i = 0, iend = value.Length; i != iend; ++i)
				{
					char ch = value[i];
					
					// Standardise newlines to a single '\n' character
					if (ch == '\n' || ch == '\r')
					{
						TmpSB.Append('\n');
						if (ch == '\r' && i+1 != iend && value[i+1] == '\n') ++i;
					}
					
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

		/// <summary>Represents a table in the rtf doc</summary>
		public class Table :Content
		{
			public override void ToRtf(StrBuild sb, Content parent)
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>A string builder class with a couple of extra state variables</summary>
		public class StrBuild
		{
			private readonly StringBuilder m_sb = new StringBuilder();
			private bool m_delim_needed;
			
			/// <summary>Rtf string data is either control words or content</summary>
			public enum EType { Control, Content }

			/// <summary>The length of the contained string</summary>
			public int Length { get { return m_sb.Length; } }

			/// <summary>Append a control words or content</summary>
			private StrBuild Append(EType type, string str, int start, int length, bool newline)
			{
				// If this is content being added, and previously control words were added, add a delimiter
				if (type != EType.Control && m_delim_needed) m_sb.Append(' ');
				
				// Add the string
				m_sb.Append(str, start, length);
				if (newline) m_sb.Append('\n');
				
				// A delimiter will be needed if this was a control word that didn't end with a '}'
				m_delim_needed = type == EType.Control && str[start + length-1] != '}';
				return this;
			}
			public StrBuild Append      (EType type, string str)                                        { return Append(type, str, 0, str.Length, false); }
			public StrBuild Append      (EType type, string str, int start, int length)                 { return Append(type, str, start, length, false); }
			public StrBuild AppendLine  (EType type, string str)                                        { return Append(type, str, 0, str.Length, true); }
			public StrBuild AppendFormat(EType type, string fmt, object arg0)                           { return Append(type, string.Format(fmt, arg0)); }
			public StrBuild AppendFormat(EType type, string fmt, object arg0, object arg1)              { return Append(type, string.Format(fmt, arg0, arg1)); }
			public StrBuild AppendFormat(EType type, string fmt, object arg0, object arg1, object arg2) { return Append(type, string.Format(fmt, arg0, arg1, arg2)); }
			public StrBuild AppendFormat(EType type, string fmt, params object[] args)                  { return Append(type, string.Format(fmt, args)); }
			
			public override string ToString() { return m_sb.ToString(); }
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using common;

	[TestFixture]
	internal static partial class UnitTests
	{
		[Test]
		public static void TestRtf()
		{
			var rtf = new Rtf.Builder();
			
			rtf.Append("A basic string\n");
			
			rtf.Style = new Rtf.Style
			{
				FontIndex = rtf.FontIndex(Rtf.FontDesc.CourierNew),
				FontStyle = Rtf.EFontStyle.Italic|Rtf.EFontStyle.Underline,
				ForeColourIndex = rtf.ColourIndex(Color.Red),
				BackColourIndex = rtf.ColourIndex(Color.Green)
			};
			rtf.Append("Text in CourierNew font in Red/Green plus italic and underlined\n");
			
			rtf.Style = Rtf.Style.Default;
			rtf.AppendLine("A normal string with a new line");
			
			rtf.Style = new Rtf.Style
			{
				FontIndex = rtf.FontIndex(Rtf.FontDesc.MSSansSerif),
				FontSize = 24,
				FontStyle = Rtf.EFontStyle.Underline|Rtf.EFontStyle.Bold,
				ForeColourIndex = rtf.ColourIndex(Color.Blue),
			};
			rtf.Append("Bold big blue values of other types:\n");
			
			rtf.Style = new Rtf.Style
			{
				FontIndex = rtf.FontIndex(Rtf.FontDesc.ComicSansMS),
				LineIndent = 400,
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
			
			rtf.Style = new Rtf.Style{Alignment = Rtf.EAlign.Right, ForeColourIndex = rtf.ColourIndex(Color.DarkMagenta)};
			rtf.AppendLine("I'm right aligned");
			
			rtf .Append(new Rtf.Style{FontStyle = Rtf.EFontStyle.Super})
				.Append("Superscript")
				.Append(new Rtf.Style{FontStyle = Rtf.EFontStyle.Sub})
				.Append("Subscript\n");
				
			var para = new Rtf.Paragraph{Style = new Rtf.Style{LineIndentFirst = 800}};
			para.Append("I'm the start of a paragraph. ")
				.Append("I'm some more words in the middle of the paragraph. These words make the paragraph fairly long just is good for testing. ")
				.Append("I'm the end of the paragraph.");
			rtf.Append(para);

			para = new Rtf.Paragraph(para.Style);
			para.Append("I'm a new short paragraph.");
			rtf.Append(para);

			var str = rtf.ToString();
			File.WriteAllText("tmp.rtf", str);
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