using System;
using System.Windows;

namespace Rylogic.Gui.WPF
{
	public class DataContextRef :Freezable
	{
		// Notes:
		//  - Captures the DataContext for an element as a resource.
		// Use:
		//  <Window>
		//     <Window.Resources>
		//         <gui:DataContextRef x:Key="WindowCtx" Ctx="{Binding .}"/>
		//     </Window.Resources>
		//     <Grid
		//         DataContext="{Binding SomeOtherObject}"
		//         >
		//         <TextBlock
		//             Text="{Binding Source={StaticResource WindowCtx}, Path=Ctx.Title}" // <- The Window.Title even though the parent DataContext is 'SomeOtherObject'
		//             />

		/// <summary>The DataContext of the saved</summary>
		public object Ctx
		{
			get => GetValue(CtxProperty);
			set => SetValue(CtxProperty, value);
		}
		public static readonly DependencyProperty CtxProperty = Gui_.DPRegister<DataContextRef>(nameof(Ctx));
		protected override Freezable CreateInstanceCore() => throw new NotImplementedException();
	}
}
