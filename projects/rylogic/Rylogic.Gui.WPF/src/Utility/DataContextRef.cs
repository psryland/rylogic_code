﻿using System;
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
		//             ** The Window.Title even though the parent DataContext is 'SomeOtherObject' **
		//             ** Note: Use 'Ctx' not the key name 'WindowCtx' for accessing fields **
		//             Text="{Binding Source={StaticResource WindowCtx}, Path=Ctx.Title}"
		//             />

		/// <summary>The DataContext of the saved</summary>
		public object Ctx
		{
			get => GetValue(CtxProperty);
			set => SetValue(CtxProperty, value);
		}
		public static readonly DependencyProperty CtxProperty = Gui_.DPRegister<DataContextRef>(nameof(Ctx), null, Gui_.EDPFlags.None);
		protected override Freezable CreateInstanceCore() => throw new NotImplementedException();
	}
}
