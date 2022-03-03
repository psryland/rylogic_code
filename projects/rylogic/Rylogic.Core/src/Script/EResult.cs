using System;

namespace Rylogic.Script
{
	/// <summary>Script exception values/return codes</summary>
	public enum EResult :uint
	{
		Success = 0,
		Failed = 0x80000000,
		InvalidIdentifier,
		InvalidString,
		InvalidValue,
		ParameterCountMismatch,
		UnexpectedEndOfFile,
		UnknownPreprocessorCommand,
		InvalidMacroDefinition,
		MacroNotDefined,
		MacroAlreadyDefined,
		IncludesNotSupported,
		MacrosNotSupported,
		InvalidInclude,
		MissingInclude,
		InvalidPreprocessorDirective,
		UnmatchedPreprocessorDirective,
		PreprocessError,
		SyntaxError,
		ExpressionSyntaxError,
		EmbeddedCodeNotSupported,
		EmbeddedCodeError,
		KeywordNotFound,
		TokenNotFound,
		ValueNotFound,
		UnknownKeyword,
		UnknownToken,
		UnknownValue,
		FileNotFound,
	}

	public class ScriptException :Exception
	{
		public ScriptException(EResult result, Loc location, string message)
			: base(message)
		{
			Result = result;
			Location = location;
		}
		public ScriptException(EResult result, Loc location, string message, Exception innerException)
			: base(message, innerException)
		{
			Result = result;
			Location = location;
		}

		/// <summary></summary>
		public EResult Result { get; }

		/// <summary></summary>
		public Loc Location { get; }
	}
}
