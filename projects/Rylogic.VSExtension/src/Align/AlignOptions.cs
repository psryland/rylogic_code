using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using pr.common;

namespace Rylogic.VSExtension
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("C0392BF9-56C7-4D4E-9669-5C4B2B38366C")]
	internal sealed class AlignOptions :DialogPage
	{
		/// <summary>The groups of alignment patterns</summary>
		public BindingList<AlignGroup> Groups { get; private set; }

		public AlignOptions()
		{
			ResetSettings();
		}

		/// <summary>Should be overridden to reset settings to their default values.</summary>
		public override void ResetSettings()
		{
			Groups = Groups ?? new BindingList<AlignGroup>{AllowNew = true, AllowRemove = true};
			Groups.Clear();

			Groups.Add(new AlignGroup("Assignments",
				new AlignPattern(EPattern.RegularExpression, @"(?<![+\-*/%^~&|!=<>])=(?![=<>])",  0, "Assignment not preceded by: +,-,*,/,%,^,~,&,|,!,=,<,> and not followed by: =,<,>"),
				new AlignPattern(EPattern.RegularExpression, @"(?<![&|])[+\-*/%^~&|]={1}"      , -1, "Assignment preceded by: +,-,*,/,%,^,~,&,| but not preceded by &&,||"),
				new AlignPattern(EPattern.RegularExpression, @"&&=|\|\|="                      , -2, "Assignments &&=,||=")));

			Groups.Add(new AlignGroup("Comma delimiter",
				new AlignPattern(EPattern.Substring, @",")));

			Groups.Add(new AlignGroup("Scope start",
				new AlignPattern(EPattern.Substring, @"{")));

			Groups.Add(new AlignGroup("Scope end",
				new AlignPattern(EPattern.Substring, @"}")));

			Groups.Add(new AlignGroup("Line comments",
				new AlignPattern(EPattern.RegularExpression, @"/{2,}", 0, "Two or more '/' characters")));

			Groups.Add(new AlignGroup("Member variables",
				new AlignPattern(EPattern.RegularExpression, @"\bm_\w*", 0, "Variable names prefixed with 'm_'"),
				new AlignPattern(EPattern.RegularExpression, @"\b_\w*", 0, "Variable names prefixed with '_'")));

			Groups.Add(new AlignGroup("Open brackets",
				new AlignPattern(EPattern.Substring, @"(")));

			Groups.Add(new AlignGroup("Close brackets",
				new AlignPattern(EPattern.Substring, @")")));
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override IWin32Window Window
		{
			get { return new AlignOptionsControl(this); }
		}
	}
}
