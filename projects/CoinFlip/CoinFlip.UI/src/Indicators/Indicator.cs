using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Indicators
{
	public abstract class Indicator<TDerived> :SettingsXml<TDerived>, IIndicator
		where TDerived : Indicator<TDerived>, new()
	{
		public Indicator()
		{
			Id = Guid.NewGuid();
			Name = null;
			Colour = Colour32.Blue;
			Visible = true;
			DisplayOrder = 0;
		}
		public Indicator(XElement node)
			: base(node)
		{
		}
		public virtual void Dispose()
		{ }

		/// <summary>Instance id</summary>
		public Guid Id
		{
			get => get<Guid>(nameof(Id));
			set => set(nameof(Id), value);
		}

		/// <summary>String identifier for the indicator</summary>
		public string Name
		{
			get => get<string>(nameof(Name));
			set => set(nameof(Name), value);
		}

		/// <summary>Colour of the indicator line</summary>
		public Colour32 Colour
		{
			get => get<Colour32>(nameof(Colour));
			set => set(nameof(Colour), value);
		}

		/// <summary>Show this indicator</summary>
		public bool Visible
		{
			get => get<bool>(nameof(Visible));
			set => set(nameof(Visible), value);
		}

		/// <summary>Display order for this indicator</summary>
		public int DisplayOrder
		{
			get => get<int>(nameof(DisplayOrder));
			set => set(nameof(DisplayOrder), value);
		}

		/// <summary>The label to use when displaying this indicator</summary>
		public abstract string Label { get; }

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		public abstract IIndicatorView CreateView(IChartView chart);
	}

	/// <summary>Indicator extensions and static methods</summary>
	public static class Indicator_
	{
		/// <summary>All indicator types </summary>
		private static IEnumerable<Type> IndicatorTypes
		{
			get
			{
				// Include indicators from plugins.... maybe one day
				return Assembly.GetExecutingAssembly().GetExportedTypes().Where(x => x.HasAttribute<IndicatorAttribute>()).OrderBy(x => Name(x));
			}
		}

		/// <summary>Enumerate the types that implement IIndicator</summary>
		public static IEnumerable<Type> EnumDrawings()
		{
			return IndicatorTypes.Where(x => x.FindAttribute<IndicatorAttribute>(false).IsDrawing == true);
		}

		/// <summary>Enumerate the types that implement IIndicator</summary>
		public static IEnumerable<Type> EnumIndicators()
		{
			return IndicatorTypes.Where(x => x.FindAttribute<IndicatorAttribute>(false).IsDrawing == false);
		}

		/// <summary>Return the name of the indicator (for use in menus)</summary>
		public static string Name(Type indy_type)
		{
			return indy_type.Name.ToString(word_sep: Str_.ESeparate.Add);
		}

		/// <summary>Call the static 'Create' method on 'indy_type'</summary>
		public static object CreateInstance(Type indy_type, CandleChart chart)
		{
			// Look for a static 'Create' method.
			var create_method = indy_type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
			if (create_method == null)
				return null;

			// Check it returns an expected type
			if (!typeof(IIndicator).IsAssignableFrom(create_method.ReturnType) &&
				!typeof(ChartControl.MouseOp).IsAssignableFrom(create_method.ReturnType))
				throw new Exception($"Unexpected return type for 'Create' on indicator type: {indy_type.Name}");

			// Create should return either an instance of the indicator, or a ChartControl.MouseOp
			// that implements the operation needed to create the indicator instance.
			var obj = create_method.Invoke(null, new object[] { chart });
			return obj;
		}
	}
}
