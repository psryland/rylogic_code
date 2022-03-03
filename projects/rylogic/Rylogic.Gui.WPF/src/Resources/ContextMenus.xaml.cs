namespace Rylogic.Gui.WPF
{
	public partial class ContextMenus
	{
		// Notes:
		//  - To access these CMenu resources from other assemblies use:
		//       // File: App.xaml
		//       xmlns:gui="clr-namespace:Rylogic.Gui.WPF;assembly=Rylogic.Gui.WPF"
		//       ...
		//       <Application.Resources>
		//           <ResourceDictionary>
		//               <ResourceDictionary.MergedDictionaries>
		//                   <x:Static Member="gui:ContextMenus.Instance"/>
		//               </ResourceDictionary.MergedDictionaries>
		//           </ResourceDictionary>
		//       </Application.Resources>
		//  - Using resources in this way allows:
		//       - x:Shared="false" elements,
		//       - path independence in the containing assembly


		public static ContextMenus Instance { get; } = new ContextMenus();
		public ContextMenus() { InitializeComponent(); }
	}
}
