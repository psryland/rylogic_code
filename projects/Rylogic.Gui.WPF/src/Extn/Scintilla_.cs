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
		/// <summary></summary>
		private class StyleDesc
		{
			public StyleDesc(int id, Colour32 fore, Colour32 back, string font)
			{
				Id = id;
				Fore = fore;
				Back = back;
				Font = font;
			}
			public int Id { get; }
			public Colour32 Fore { get; }
			public Colour32 Back { get; }
			public string Font { get; }
		};

		/// <summary>Initialise with reasonable default style</summary>
		public static void InitDefaultStyle(this ScintillaControl sc)
		{
			sc.CodePage = Sci.SC_CP_UTF8;
			sc.ClearDocumentStyle();
			sc.StyleBits = 7;
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
			sc.ConvertEOLs(Sci.EEndOfLineMode.LF);
			sc.EOLMode = Sci.EEndOfLineMode.LF;
			//SndMsg<void>(SCI_SETVIEWEOL, TRUE, 0);

			// set marker symbol for marker type 0 - bookmark
			sc.MarkerDefine(0, Sci.SC_MARK_CIRCLE);

			//// display all margins
			//DisplayLinenumbers(TRUE);
			//SetDisplayFolding(TRUE);
			//SetDisplaySelection(TRUE);
		}

		/// <summary>Set up this control for Ldr script</summary>
		public static void InitLdrStyle(this ScintillaControl sc, bool dark = false)
		{
			sc.ClearDocumentStyle();
			sc.StyleBits = 7;
			sc.IndentationGuides = true;
			sc.AutoIndent = true;
			sc.TabWidth = 4;
			sc.Indent = 4;
			sc.CaretFore = dark ? 0xFFffffff : 0xFF000000;
			sc.CaretPeriod = 400;
			sc.ConvertEOLs(Sci.EEndOfLineMode.LF);
			sc.EOLMode = Sci.EEndOfLineMode.LF;
			sc.Property("fold", "1");
			sc.MultipleSelection = true;
			sc.AdditionalSelectionTyping = true;
			sc.VirtualSpace = Sci.SCVS_RECTANGULARSELECTION;

			var dark_style = new StyleDesc[]
			{
				new StyleDesc(Sci.STYLE_DEFAULT          , 0xc8c8c8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.STYLE_LINENUMBER       , 0xc8c8c8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.STYLE_INDENTGUIDE      , 0x484439 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.STYLE_BRACELIGHT       , 0x98642b , 0x5e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_DEFAULT        , 0xc8c8c8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COMMENT_BLK    , 0x4aa656 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COMMENT_LINE   , 0x4aa656 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_STRING_LITERAL , 0x859dd6 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_CHAR_LITERAL   , 0x859dd6 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NUMBER         , 0xf7f7f8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_KEYWORD        , 0xd69c56 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_PREPROC        , 0xc563bd , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_OBJECT         , 0x81c93d , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NAME           , 0xffffff , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COLOUR         , 0x7c97c3 , 0x1e1e1e , "courier new"),
			};
			var light_style = new StyleDesc[]
			{
				new StyleDesc(Sci.STYLE_DEFAULT          , 0x120700 , 0xffffff , "courier new"),
				new StyleDesc(Sci.STYLE_LINENUMBER       , 0x120700 , 0xffffff , "courier new"),
				new StyleDesc(Sci.STYLE_INDENTGUIDE      , 0xc0c0c0 , 0xffffff , "courier new"),
				new StyleDesc(Sci.STYLE_BRACELIGHT       , 0x2b6498 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_DEFAULT        , 0x120700 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COMMENT_BLK    , 0x008100 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COMMENT_LINE   , 0x008100 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_STRING_LITERAL , 0x154dc7 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_CHAR_LITERAL   , 0x154dc7 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NUMBER         , 0x1e1e1e , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_KEYWORD        , 0xff0000 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_PREPROC        , 0x8a0097 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_OBJECT         , 0x81962a , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NAME           , 0x000000 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COLOUR         , 0x83573c , 0xffffff , "courier new"),
			};
			Debug.Assert(dark_style.Length == light_style.Length);

			var style = dark ? dark_style : light_style;
			for (int i = 0; i != style.Length; ++i)
			{
				var s = style[i];
				sc.StyleSetFont(s.Id, s.Font);
				sc.StyleSetFore(s.Id, s.Fore);
				sc.StyleSetBack(s.Id, s.Back);
			}

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
		}

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
						sc.InitDefaultStyle();
						break;
					}
				case EScintillaStyles.LdrLight:
					{
						sc.InitLdrStyle(dark: false);
						break;
					}
				case EScintillaStyles.LdrDark:
					{
						sc.InitLdrStyle(dark: true);
						break;
					}
				}
			}
		}
	}
}
