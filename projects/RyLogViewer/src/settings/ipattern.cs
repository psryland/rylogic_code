using System;

namespace RyLogViewer
{
	public interface IPattern :ICloneable
	{
		/// <summary>Returns true if the pattern is active</summary>
		bool Active { get; set; }

		/// <summary>True if the pattern is a regular expression, false if it's just a substring</summary>
		EPattern PatnType { get; set; }

		/// <summary>The pattern to use when matching</summary>
		string Expr { get; set; }

		/// <summary>True if the pattern should ignore case</summary>
		bool IgnoreCase { get; set; }

		/// <summary>True if the match result should be inverted</summary>
		bool Invert { get; set; }

		/// <summary>Only match if the whole line matches</summary>
		bool WholeLine { get; set; }

		/// <summary>Returns the names of the capture groups in this pattern</summary>
		string[] CaptureGroupNames { get; }

		/// <summary>Returns true if the pattern is valid</summary>
		bool IsValid { get; }
	}
}
