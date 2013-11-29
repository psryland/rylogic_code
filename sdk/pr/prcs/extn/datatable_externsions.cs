using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using pr.common;
using pr.util;

namespace pr.extn
{
	public static class DataTableExternsions
	{
		/// <summary>An RAII scope for BeginInit/EndInit</summary>
		public static Scope BeginInitScope<TDataTable>(this TDataTable dt) where TDataTable :DataTable
		{
			return Scope.Create(dt.BeginInit, dt.EndInit);
		}

		/// <summary>An RAII scope for BeginLoadData/EndLoadData</summary>
		public static Scope BeginLoadDataScope<TDataTable>(this TDataTable dt) where TDataTable :DataTable
		{
			return Scope.Create(dt.BeginLoadData, dt.EndLoadData);
		}

		/// <summary>Helper for reading fields in this table</summary>
		public static TReturn get<TRowType, TReturn>(this TRowType row, Expression<Func<TRowType,TReturn>> expression) where TRowType :DataRow
		{
			var obj = row[Reflect<TRowType>.MemberName(expression)];
			return obj is TReturn ? (TReturn)obj : default(TReturn);
		}

		/// <summary>Helper for writing fields in this table</summary>
		public static void set<TRowType, TReturn>(this TRowType row, Expression<Func<TRowType,TReturn>> expression, TReturn value) where TRowType :DataRow
		{
			row[Reflect<TRowType>.MemberName(expression)] = value;
		}
	}

	/// <summary>Helper base class for creating typed DataTables</summary>
	public abstract class TypedDataTable<TRowType> : DataTable where TRowType: DataRow
	{
		protected TypedDataTable(string name, bool generate_columns)
		{
			TableName = name;
			if (generate_columns)
				AutoGenerateColumns();
		}
		protected override Type GetRowType()
		{
			return typeof(TRowType);
		}
		protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
		{
			 return base.NewRow();
		}
		public new TRowType NewRow()
		{
			return (TRowType)base.NewRow();
		}

		/// <summary>Constructs a custom DataTable using attributed properties on the row type.</summary>
		protected void AutoGenerateColumns(BindingFlags flags = BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly)
		{
			// Find all the columns marked as DataColumns
			var props = GetRowType().GetProperties(flags)
				.Select(pi => new {PropInfo = pi, Attr = pi.GetAttribute<DataColumnAttribute>()})
				.Where(pi => pi.Attr != null)
				.OrderBy(pi => pi.Attr.Order);

			var pks = new List<DataColumn>();

			// Create the columns
			foreach (var prop in props)
			{
				var col = new DataColumn(prop.PropInfo.Name, prop.PropInfo.PropertyType);
				if (prop.Attr.PrimaryKey)
				{
					pks.Add(col);
				}
				if (prop.Attr.AutoInc)
				{
					col.AutoIncrement = true;
					col.AutoIncrementSeed = prop.Attr.AutoIncSeed;
					col.AutoIncrementStep = prop.Attr.AutoIncStep;
				}
				Columns.Add(col);
			}
			PrimaryKey = pks.ToArray();
		}
	}

	/// <summary>Marks a property as a column in a DataTable</summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class DataColumnAttribute :Attribute
	{
		/// <summary>
		/// True if this column should be used as a primary key.
		/// If multiple primary keys are specified, ensure the Order
		/// property is used so that the order of primary keys is defined.
		/// Default value is false.</summary>
		public bool PrimaryKey { get; set; }

		/// <summary>True if this column should auto increment. Default is false</summary>
		public bool AutoInc { get; set; }

		/// <summary>The first auto inc value (only applies when AutoInc is true)</summary>
		public int AutoIncSeed { get; set; }

		/// <summary>The increment for auto inc values (only applies when AutoInc is true)</summary>
		public int AutoIncStep { get; set; }

		/// <summary>Defines the relative order of columns in the table. Default is '0'</summary>
		public int Order { get; set; }

		public DataColumnAttribute()
		{
			PrimaryKey  = false;
			AutoInc     = false;
			AutoIncSeed = 1;
			AutoIncStep = 1;
			Order       = 0;
		}
	}
}
