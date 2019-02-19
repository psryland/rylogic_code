using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.DockContainerDetail;
using Rylogic.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for DockContainerUI.xaml
	/// </summary>
	public partial class DockContainerUI : Window
	{
		public DockContainerUI()
		{
			InitializeComponent();

			var d0 = new Dockable("Dockable 0") { Background = Brushes.LightCoral };
			var d1 = new Dockable("Dockable 1") { Background = Brushes.LightGreen };
			var d2 = new Dockable("Dockable 2") { Background = Brushes.White };
			var d3 = new Dockable("Dockable 3") { Background = Brushes.White };
			var d4 = new Dockable("Dockable 4") { Background = Brushes.White };
			var d5 = new Dockable("Dockable 5") { Background = Brushes.White };
			var d6 = new Dockable("Dockable 6") { Background = Brushes.White };
			var d7 = new Dockable("Dockable 7") { Background = Brushes.White };
			var d8 = new Dockable("Dockable 8") { Background = Brushes.White };
			var d9 = new Dockable("Dockable 9") { Background = Brushes.White };

			var save_btn = d0.Children.Add2(new Button { Content = "Save Layout" });
			var load_btn = d0.Children.Add2(new Button { Content = "Load Layout" });
			save_btn.Click += (s, a) =>
			{
				var xml = m_dc.SaveLayout();
				xml.Save("\\dump\\layout.xml");
			};
			load_btn.Click += (s, a) =>
			{
				var xml = XElement.Load("\\dump\\layout.xml");
				m_dc.LoadLayout(xml);
			};

			m_dc.Add(d0, EDockSite.Centre);
			m_dc.Add(d1, EDockSite.Centre);
			m_dc.Add(d2, EDockSite.Left);
			m_dc.Add(d3, EDockSite.Left);
			m_dc.Add(d4, EDockSite.Bottom);
			m_dc.Add(d5, EDockSite.Centre);
			m_dc.Add(d6, EDockSite.Centre);
		//	m_dc.Add(d7, EDockSite.Right);
		//	m_dc.Add(d8, EDockSite.Top);
		//	m_dc.Add(d9, EDockSite.Top);
		}
	}

	/// <summary>Example dockable UI element</summary>
	public class Dockable : StackPanel, IDockable
	{
		public Dockable(string text)
		{
			//BorderStyle = BorderStyle.None;
			//Text = text;
			//BackgroundImage = SystemIcons.Error.ToBitmap();
			//BackgroundImageLayout = ImageLayout.Stretch;
			DockControl = new DockControl(this, text)
			{
				TabText = text,
				//TabIcon = SystemIcons.Exclamation.ToBitmap()
			};

			var label = Children.Add2(new Label { Content = "LABEL" });
			DockControl.PaneChanged += (s, a) =>
			{
				var pane = DockControl.DockPane;
				if (pane == null)
					label.Content = "No Pane";
				else
					label.Content = pane.LocationDesc();

			};

			//Children.Add(new TextBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
			//Children.Add(new TextBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
			//Children.Add(new TextBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
		}

		/// <summary>Implements docking functionality</summary>
		public DockControl DockControl
		{
			[DebuggerStepThrough]
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>Human friendly name</summary>
		public override string ToString()
		{
			return DockControl.TabText;
		}
	}
}

/*
 
		<DockPanel Visibility="Collapsed" Width="{Binding ActualWidth, ElementName=Root}" Height="{Binding ActualHeight, ElementName=Root}">
			<StackPanel Name="ts" DockPanel.Dock="Right" Orientation="Horizontal" Background="Beige">
				<StackPanel.LayoutTransform>
					<RotateTransform Angle="90"/>
				</StackPanel.LayoutTransform>

				<Button Content="Words" Margin="5"></Button>
				<Button Content="Words" Margin="5"></Button>
				<Button Content="Words" Margin="5"></Button>
				<Button Content="Words" Margin="5"></Button>
			</StackPanel>
			<Grid></Grid>
		</DockPanel>

		<!--Dock Pane--> 
		<DockPanel Visibility="Collapsed" Name="DockPane" Background="SteelBlue" Width ="200" Height="300">

			<Grid Name="TitleBar" DockPanel.Dock="Top" Background="BurlyWood" >
				<Grid.ColumnDefinitions>
					<ColumnDefinition Width="*"/>
					<ColumnDefinition Width="Auto"/>
					<ColumnDefinition Width="Auto"/>
				</Grid.ColumnDefinitions>
				<TextBlock Name="PaneTitle" Grid.Column="0" VerticalAlignment="Center">This is my DockPane</TextBlock>
				<Button Name="ClosePane" Grid.Column="1" Content="P" Padding="5 0 5 0" Margin="3 3 3 3"/>
				<Button Name="PinPane" Grid.Column="2" Content="X" Padding="5 0 5 0" Margin="3 3 3 3"/>
			</Grid>
			
			<StackPanel Name="TabStrip" Orientation="Horizontal" DockPanel.Dock="Bottom" Background="Beige">
				<Button Content="Tab1" Padding="5 0 5 0" MinHeight="20">
					<Button.LayoutTransform>
						<RotateTransform Angle="0"/>
					</Button.LayoutTransform>
				</Button>
				<Button Content="Tab2" Padding="5 0 5 0" MinHeight="20">
					<Button.LayoutTransform>
						<RotateTransform Angle="0"/>
					</Button.LayoutTransform>
				</Button>
			</StackPanel>

			
			<Canvas Name="PaneCentre"/>
		</DockPanel>

		<!--Dock Container visual tree-->
		<DockPanel Visibility="Collapsed" Background="{x:Static SystemColors.ControlDarkBrush}" Width="{Binding ActualWidth, ElementName=Root}" Height="{Binding ActualHeight, ElementName=Root}">

			<StackPanel Name="AutoHideLeftTabs" Orientation="Vertical" DockPanel.Dock="Left" Background="Beige">
				<Button Content="Tab1" Padding="5 0 5 0" MinWidth="40">
					<Button.LayoutTransform>
						<RotateTransform Angle="-90"/>
					</Button.LayoutTransform>
				</Button>
				<Button Content="Tab2" Padding="5 0 5 0" MinWidth="40">
					<Button.LayoutTransform>
						<RotateTransform Angle="-90"/>
					</Button.LayoutTransform>
				</Button>
			</StackPanel>

			<StackPanel Name="AutoHideRightTabs" Orientation="Vertical" DockPanel.Dock="Right" Background="Beige">
				<Button Width="80" Height="20" Content="Tab1">
					<Button.LayoutTransform>
						<RotateTransform Angle="90"/>
					</Button.LayoutTransform>
				</Button>
				<Button Width="80" Height="20" Content="Tab2">
					<Button.LayoutTransform>
						<RotateTransform Angle="90"/>
					</Button.LayoutTransform>
				</Button>
			</StackPanel>

			<StackPanel Name="AutoHideTopTabs" Orientation="Horizontal" DockPanel.Dock="Top" Background="Beige" Height="20">
				<Button Width="80" Height="20" Content="Tab1">
					<Button.LayoutTransform>
						<RotateTransform Angle="0"/>
					</Button.LayoutTransform>
				</Button>
				<Button Width="80" Height="20" Content="Tab2">
					<Button.LayoutTransform>
						<RotateTransform Angle="0"/>
					</Button.LayoutTransform>
				</Button>
			</StackPanel>

			<StackPanel Name="AutoHideBottomTabs" Orientation="Horizontal" DockPanel.Dock="Bottom" Background="Beige">
				<Button Width="80" Height="20" Content="Tab1">
					<Button.LayoutTransform>
						<RotateTransform Angle="0"/>
					</Button.LayoutTransform>
				</Button>
				<Button Width="80" Height="20" Content="Tab2">
					<Button.LayoutTransform>
						<RotateTransform Angle="0"/>
					</Button.LayoutTransform>
				</Button>
			</StackPanel>

			<Canvas Name="DockArea">
				<!--Branch-->
				<DockPanel Width="{Binding ActualWidth, ElementName=DockArea}" Height="{Binding ActualHeight, ElementName=DockArea}">

					<!--Each dock panel will be a dock pane-->
					<DockPanel Name="DockPaneLeft" DockPanel.Dock="Left" Background="Aqua" Width="80"/>
					<gui2:DockPanelSplitter Name="SplitterLeft" DockPanel.Dock="Left" Thickness="10" ProportionalResize="False" Background="Red"/>

					<DockPanel Name="DockPaneRight" DockPanel.Dock="Right" Background="Aqua" Width="200"/>
					<gui2:DockPanelSplitter  Name="SplitterRight" DockPanel.Dock="Right" Thickness="10" />

					<DockPanel Name="DockPaneTop" DockPanel.Dock="Top" Background="Aqua" Height="60"/>
					<gui2:DockPanelSplitter  Name="SplitterTop" DockPanel.Dock="Top" Thickness="10" />

					<DockPanel Name="DockPaneBottom" DockPanel.Dock="Bottom" Background="Aqua" Height="80"/>
					<gui2:DockPanelSplitter Name="SplitterBottom" DockPanel.Dock="Bottom" Thickness="10" />

					<DockPanel Name="DockPaneCentre" Background="LightSteelBlue"/>
				</DockPanel>

				<!--Auto hide panels-->
				<Grid Visibility="Collapsed" Name="AutoHideLeft"   Canvas.Left="0" Canvas.Top="0" Width="{Binding ActualWidth, ElementName=DockArea}" Height="{Binding ActualHeight, ElementName=DockArea}">
					<Grid.ColumnDefinitions>
						<ColumnDefinition Width="0.25*"/>
						<ColumnDefinition Width="Auto"/>
						<ColumnDefinition Width="*"/>
					</Grid.ColumnDefinitions>
					<DockPanel Name="AutoHideDockPane" Grid.Column="0" Background="Blue"/>
					<GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch" />
				</Grid>
			</Canvas>
		</DockPanel>

		<!-- Dragging indicator -->
		<!--<Image Source="{StaticResource dock_site_cross_lg}" Width="128" Height="128"/>-->

 */
