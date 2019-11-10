using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Rylogic.Gfx;
using Rylogic.Scintilla;

namespace Rylogic.Gui.WPF
{
	public enum EScintillaStyles
	{
		Default,
		LdrLight,
		LdrDark,
	}

	public static class Scintilla_
	{
		/// <summary>Initialise with reasonable default style</summary>
		public static ScintillaControl ConfigDefault(this ScintillaControl sc)
		{
			sc.CodePage = Sci.SC_CP_UTF8;
			sc.ClearDocumentStyle();
			sc.IndentationGuides = true;
			sc.TabWidth = 4;
			sc.Indent = 4;
			sc.CaretPeriod = 400;

			// source folding section
			// tell the lexer that we want folding information - the lexer supplies "folding levels"
			sc.Property("fold", "1");
			sc.Property("fold.html", "1");
			sc.Property("fold.html.preprocessor", "1");
			sc.Property("fold.comment", "1");
			sc.Property("fold.at.else", "1");
			sc.Property("fold.flags", "1");
			sc.Property("fold.preprocessor", "1");
			sc.Property("styling.within.preprocessor", "1");
			sc.Property("asp.default.language", "1");

			// Tell scintilla to draw folding lines UNDER the folded line
			sc.FoldFlags(16);

			// Set margin 2 = folding margin to display folding symbols
			sc.MarginMaskN(2, Sci.SC_MASK_FOLDERS);

			// allow notifications for folding actions
			sc.ModEventMask = Sci.SC_MOD_INSERTTEXT | Sci.SC_MOD_DELETETEXT;
			//sc.ModEventMask(SC_MOD_CHANGEFOLD|SC_MOD_INSERTTEXT|SC_MOD_DELETETEXT);

			// make the folding margin sensitive to folding events = if you click into the margin you get a notification event
			sc.MarginSensitiveN(2, true);

			// define a set of markers to display folding symbols
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDEROPEN, Sci.SC_MARK_MINUS);
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDER, Sci.SC_MARK_PLUS);
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDERSUB, Sci.SC_MARK_EMPTY);
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDERTAIL, Sci.SC_MARK_EMPTY);
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDEREND, Sci.SC_MARK_EMPTY);
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDEROPENMID, Sci.SC_MARK_EMPTY);
			sc.MarkerDefine(Sci.SC_MARKNUM_FOLDERMIDTAIL, Sci.SC_MARK_EMPTY);

			// Set the foreground color for some styles
			sc.StyleSetFore(0, new Colour32(0xFF, 0, 0, 0));
			sc.StyleSetFore(2, new Colour32(0xFF, 0, 64, 0));
			sc.StyleSetFore(5, new Colour32(0xFF, 0, 0, 255));
			sc.StyleSetFore(6, new Colour32(0xFF, 200, 20, 0));
			sc.StyleSetFore(9, new Colour32(0xFF, 0, 0, 255));
			sc.StyleSetFore(10, new Colour32(0xFF, 255, 0, 64));
			sc.StyleSetFore(11, new Colour32(0xFF, 0, 0, 0));

			// Set the background color of brace highlights
			sc.StyleSetBack(Sci.STYLE_BRACELIGHT, new Colour32(0xFF, 0, 255, 0));

			// Set end of line mode to CRLF
			sc.ConvertEOLs(Sci.EEndOfLine.Lf);
			sc.EOLMode = Sci.EEndOfLine.Lf;
			//SndMsg<void>(SCI_SETVIEWEOL, TRUE, 0);

			// set marker symbol for marker type 0 - bookmark
			sc.MarkerDefine(0, Sci.SC_MARK_CIRCLE);

			//// display all margins
			//DisplayLinenumbers(TRUE);
			//SetDisplayFolding(TRUE);
			//SetDisplaySelection(TRUE);
			return sc;
		}

		/// <summary>Set up this control for Ldr script</summary>
		public static ScintillaControl ConfigLdr(this ScintillaControl sc, bool dark)
		{
			sc.ClearDocumentStyle();
			sc.IndentationGuides = true;
			sc.AutoIndent = true;
			sc.TabWidth = 4;
			sc.Indent = 4;
			sc.CaretFore = dark ? 0xFFffffff : 0xFF000000;
			sc.CaretPeriod = 400;
			sc.ConvertEOLs(Sci.EEndOfLine.Lf);
			sc.EOLMode = Sci.EEndOfLine.Lf;
			sc.Property("fold", "1");
			sc.MultipleSelection = true;
			sc.AdditionalSelectionTyping = true;
			sc.VirtualSpace = Sci.SCVS_RECTANGULARSELECTION;

			Debug.Assert(LdrDark.Length == LdrLight.Length);
			sc.ApplyStyles(dark ? LdrDark : LdrLight);

			sc.MarginTypeN(0, Sci.SC_MARGIN_NUMBER);
			sc.MarginTypeN(1, Sci.SC_MARGIN_SYMBOL);

			sc.MarginMaskN(1, Sci.SC_MASK_FOLDERS);

			sc.MarginWidthN(0, sc.TextWidth(Sci.STYLE_LINENUMBER, "_9999"));
			sc.MarginWidthN(1, 0);

			// set marker symbol for marker type 0 - bookmark
			sc.MarkerDefine(0, Sci.SC_MARK_CIRCLE);

			//// display all margins
			//DisplayLinenumbers(TRUE);
			//SetDisplayFolding(TRUE);
			//SetDisplaySelection(TRUE);

			// Initialise UTF-8 with the ldr lexer
			sc.CodePage = Sci.SC_CP_UTF8;
			sc.Lexer = Sci.SCLEX_LDR;
			sc.LexerLanguage("ldr");

			return sc;
		}
		public static ScintillaControl ConfigLdrLight(this ScintillaControl sc) => sc.ConfigLdr(dark: false);
		public static ScintillaControl ConfigLdrDark(this ScintillaControl sc) => sc.ConfigLdr(dark: true);

		/// <summary>Default font family for LdrScript</summary>
		public const string LdrFontName = "consolas";

		/// <summary>Style settings for dark mode</summary>
		public static Sci.StyleDesc[] LdrLight { get; } = new[]
		{
			new Sci.StyleDesc(Sci.STYLE_DEFAULT         ){ Fore = 0x120700 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.STYLE_LINENUMBER      ){ Fore = 0x120700 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.STYLE_INDENTGUIDE     ){ Fore = 0xc0c0c0 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.STYLE_BRACELIGHT      ){ Fore = 0x2b6498 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_DEFAULT       ){ Fore = 0x120700 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_COMMENT_BLK   ){ Fore = 0x008100 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_COMMENT_LINE  ){ Fore = 0x008100 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_STRING_LITERAL){ Fore = 0x154dc7 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_CHAR_LITERAL  ){ Fore = 0x154dc7 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_NUMBER        ){ Fore = 0x1e1e1e , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_KEYWORD       ){ Fore = 0xff0000 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_PREPROC       ){ Fore = 0x8a0097 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_OBJECT        ){ Fore = 0x81962a , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_NAME          ){ Fore = 0x000000 , Back = 0xffffff , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_COLOUR        ){ Fore = 0x83573c , Back = 0xffffff , Font = LdrFontName},
		};

		/// <summary>Style settings for dark mode</summary>
		public static Sci.StyleDesc[] LdrDark { get; } = new[]
		{
			new Sci.StyleDesc(Sci.STYLE_DEFAULT         ){ Fore = 0xc8c8c8 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.STYLE_LINENUMBER      ){ Fore = 0xc8c8c8 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.STYLE_INDENTGUIDE     ){ Fore = 0x484439 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.STYLE_BRACELIGHT      ){ Fore = 0x98642b , Back = 0x5e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_DEFAULT       ){ Fore = 0xc8c8c8 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_COMMENT_BLK   ){ Fore = 0x4aa656 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_COMMENT_LINE  ){ Fore = 0x4aa656 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_STRING_LITERAL){ Fore = 0x859dd6 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_CHAR_LITERAL  ){ Fore = 0x859dd6 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_NUMBER        ){ Fore = 0xf7f7f8 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_KEYWORD       ){ Fore = 0xd69c56 , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_PREPROC       ){ Fore = 0xc563bd , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_OBJECT        ){ Fore = 0x81c93d , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_NAME          ){ Fore = 0xffffff , Back = 0x1e1e1e , Font = LdrFontName},
			new Sci.StyleDesc(Sci.SCE_LDR_COLOUR        ){ Fore = 0x7c97c3 , Back = 0x1e1e1e , Font = LdrFontName},
		};

		/// <summary>Style Attached property</summary>
		private const int Style = 0;
		public static readonly DependencyProperty StyleProperty = Gui_.DPRegisterAttached(typeof(Scintilla_), nameof(Style));
		public static EScintillaStyles GetStyle(DependencyObject obj) => (EScintillaStyles)obj.GetValue(StyleProperty);
		public static void SetStyle(DependencyObject obj, EScintillaStyles value) => obj.SetValue(StyleProperty, value);
		private static void Style_Changed(DependencyObject obj)
		{
			if (obj is ScintillaControl sc)
			{
				switch (GetStyle(sc))
				{
				case EScintillaStyles.Default:
					{
						sc.InitStyle = x => ConfigDefault(x);
						break;
					}
				case EScintillaStyles.LdrLight:
					{
						sc.InitStyle = x => ConfigLdrLight(x);
						break;
					}
				case EScintillaStyles.LdrDark:
					{
						sc.InitStyle = x => ConfigLdrDark(x);
						break;
					}
				}
			}
		}
	}
}
