using EnvDTE80;
using EnvDTE;
using System.Linq;
using pr.util;

namespace Rylogic.VSExtension
{
	public sealed class OutlineCodeCommand :BaseCommand
	{
		public OutlineCodeCommand(Rylogic_VSExtensionPackage package)
			:base(package, PkgCmdIDList.cmdidOutlineCodeOnly)
		{
			IDE = (DTE2)Package.GetService<DTE>();
		}

		private DTE2 IDE { get; set; }

		private Document Doc { get; set; }

		/// <summary></summary>
		protected override void Execute()
		{
			// Recursively find the points in the code to collapse
			Doc = IDE.ActiveDocument;
			Doc.Activate();

			// Preserve the caret position
			var text = (TextSelection)Doc.Selection;
			using (Scope.Create(() => text.ActivePoint, pt => text.MoveToPoint(pt)))
			using (Scope.Create(() => IDE.UndoContext.Open("OutlineCode"), () => IDE.UndoContext.Close()))
			{
				// Expand all to start
				try { IDE.ExecuteCommand("Edit.StopOutlining"); } catch { }
				try { IDE.ExecuteCommand("Edit.StartAutomaticOutlining"); } catch { }

				var code = Doc.ProjectItem.FileCodeModel;
				foreach (var elem in code.CodeElements.OfType<CodeElement>())
					DoCollapse(elem, 0);
			}
		}

		/// <summary>Recursively find the lines in the code to collapse</summary>
		private void DoCollapse(CodeElement elem, int level)
		{
			switch (elem.Kind)
			{
			case vsCMElement.vsCMElementNamespace:
				{
					foreach (var member in elem.Children.OfType<CodeElement>())
						DoCollapse(member, level + 1);
					break;
				}
			case vsCMElement.vsCMElementClass:
				{
					foreach (var member in elem.Children.OfType<CodeElement>())
						DoCollapse(member, level + 1);
					if (level > 1)
						ToggleOutlining(elem.StartPoint);
					break;
				}
			case vsCMElement.vsCMElementFunction:
			case vsCMElement.vsCMElementEnum:
			case vsCMElement.vsCMElementInterface:
			case vsCMElement.vsCMElementEvent:
			case vsCMElement.vsCMElementProperty:
				{
					ToggleOutlining(elem.StartPoint);
					break;
				}
			}
		}

		/// <summary>Toggle outlining at 'pt'</summary>
		private void ToggleOutlining(TextPoint pt)
		{
			var text = (TextSelection)Doc.Selection;
			text.MoveToPoint(pt);
			IDE.ExecuteCommand("Edit.ToggleOutliningExpansion");
		}
	}
}
