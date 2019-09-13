using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gfx;

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
}
