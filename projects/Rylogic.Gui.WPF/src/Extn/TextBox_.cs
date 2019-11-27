using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Common;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public static class TextBox_
	{
		/// <summary>Returns a disposable object that preserves the current selection</summary>
		public static Scope<Range> SelectionScope(this TextBox edit)
		{
			return Scope.Create(
				() => Range.FromStartLength(edit.SelectionStart, edit.SelectionLength),
				rn => edit.Select(rn.Begi, rn.Sizei));
		}

		/// <summary>Global command for textboxes to UpdateSource when enter is pressed</summary>
		public static Command UpdateSource { get; } = Command.Create<object>(null!, UpdateSourceInternal);
		private static void UpdateSourceInternal(object? parameter)
		{
			// Use:
			//   <TextBox
			//       Text="{Binding Whatever}"
			//       >
			//       <TextBox.InputBindings>
			//           <KeyBinding
			//               Key="Return"
			//               Command="{x:Static Member=gui:TextBox_.UpdateSource}"
			//               CommandParameter="{Binding RelativeSource={RelativeSource AncestorType={x:Type TextBox}}}"
			//               />
			//       </TextBox.InputBindings>
			//   </TextBox>

			if (parameter is TextBox tb && BindingOperations.GetBindingExpression(tb, TextBox.TextProperty) is BindingExpression binding)
				binding.UpdateSource();
		}
	}
}
