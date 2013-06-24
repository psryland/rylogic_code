using System;

namespace RyLogViewer
{
	public enum EBtnImageIdx
	{
		AddNew = 0,
		Save = 1
	}

	interface IPatternUI
	{
		/// <summary>Return the original before any edits were made</summary>
		IPattern Original { get; }

		/// <summary>Return the pattern currently being edited</summary>
		IPattern Pattern { get; }

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		bool IsNew { get; }

		/// <summary>Set a new pattern for the UI</summary>
		void NewPattern(IPattern pat);

		/// <summary>Select a pattern into the UI for editing</summary>
		void EditPattern(IPattern pat);

		/// <summary>True if the contained pattern is different to the original</summary>
		bool HasUnsavedChanges { get; }

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		bool CommitEnabled { get; }

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		event EventHandler Commit;

		/// <summary>Access to the test text field</summary>
		string TestText { get; set; }

		/// <summary>Set focus to the primary input field</summary>
		void FocusInput();
	}
}
