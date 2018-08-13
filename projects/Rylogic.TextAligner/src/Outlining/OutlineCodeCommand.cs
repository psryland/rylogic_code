using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Rylogic.Utility;

namespace Rylogic.VSExtension
{
	// Interesting, but not directly related article on creating a custom
	// outlining region provider for VS: https://msdn.microsoft.com/en-us/library/ee197665.aspx

	public sealed class OutlineCodeCommand :BaseCommand
	{
		public OutlineCodeCommand(Rylogic_VSExtensionPackage package)
			: base(package, PkgCmdIDList.cmdidOutlineCodeOnly)
		{ }

		/// <summary>Return the DTE interface</summary>
		private DTE2 IDE
		{
			get { return m_impl_ide ?? (m_impl_ide = (DTE2)Package.GetService<DTE>()); }
		}
		private DTE2 m_impl_ide;

		/// <summary>A view of the text</summary>
		private ITextView TextView
		{
			get { return CurrentViewHost?.TextView; }
		}

		/// <summary>The interface for collapsing outlining sections</summary>
		private IOutliningManager OutliningManager
		{
			get
			{
				// Get the component model service and use it to get the outlining manager
				var comp_model = Package.GetService<SComponentModel>() as IComponentModel;
				var service = comp_model?.GetService<IOutliningManagerService>();
				var manager = service?.GetOutliningManager(TextView);
				return manager;
			}
		}

		/// <summary>Do the command</summary>
		protected override void Execute()
		{
			var ss = TextView.TextSnapshot;
			var mgr = OutliningManager;

			// Collapse anything that contains X tabs and isn't a comment
			var doc_span = new SnapshotSpan(ss, 0, ss.Length);
			mgr.CollapseAll(doc_span, x =>
			{
				var span = x.Extent.GetText(ss);
				return
					!span.StartsWith("//") &&
					 span.TrimStart('\r','\n').StartsWith("\t\t");
			});


			// Recursively find the points in the code to collapse
			//var doc = IDE.ActiveDocument; doc.Activate();
			//var code = doc.ProjectItem.FileCodeModel;


			//// Find the code regions to collapse
			//var regions = new List<SnapshotSpan>();
			//foreach (var elem in code.CodeElements.OfType<CodeElement>())
			//	GetCollapseRegions(elem, 0, regions);

			//// Collapse each region



			//foreach (var region in regions)
			//{
			//	OutliningManager.CollapseAll(region, x => true);
			//}

			//// Use the outlining manager to enumerate the regions intersecting the given set of spans
			//if(outliningManager != null)
			//	foreach(var region in outliningManager.GetCollapsedRegions(span, false))
			//		if(region.IsCollapsed)
			//			outliningManager.Expand(region);

			//var doc_span = new SnapshotSpan(TextView.TextSnapshot, 0, TextView.TextSnapshot.Length);
			//OutliningManager.CollapseAll(doc_span, collapsible =>
			//{
			//	//collapsible

			//	return true;
			//});
		}

		/// <summary>Get the spans to collapse</summary>
		private void GetCollapseRegions(CodeElement elem, int level, List<SnapshotSpan> regions)
		{
			switch (elem.Kind)
			{
			case vsCMElement.vsCMElementNamespace:
				{
					foreach (var member in elem.Children.OfType<CodeElement>())
						GetCollapseRegions(member, level + 1, regions);
					break;
				}
			case vsCMElement.vsCMElementClass:
				{
					foreach (var member in elem.Children.OfType<CodeElement>())
						GetCollapseRegions(member, level + 1, regions);

					if (level > 1)
						AddCodeElement(regions, elem);

					break;
				}
			case vsCMElement.vsCMElementFunction:
			case vsCMElement.vsCMElementEnum:
			case vsCMElement.vsCMElementInterface:
			case vsCMElement.vsCMElementEvent:
			case vsCMElement.vsCMElementProperty:
				{
					AddCodeElement(regions, elem);
					break;
				}
			}
		}

		/// <summary>Add the span for a code element to 'regions'</summary>
		private void AddCodeElement(List<SnapshotSpan> regions, CodeElement elem)
		{
			var ss = TextView.TextSnapshot;
			var len = elem.EndPoint.AbsoluteCharOffset - elem.StartPoint.AbsoluteCharOffset;
			var beg = new SnapshotPoint(ss, elem.StartPoint.AbsoluteCharOffset);
			var end = new SnapshotPoint(ss, elem.EndPoint.AbsoluteCharOffset);
			regions.Add(new SnapshotSpan(beg, end));
		}
	}



	#region
	public sealed class OutlineCodeCommand2 :BaseCommand
	{
		public OutlineCodeCommand2(Rylogic_VSExtensionPackage package)
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
	#endregion
}
