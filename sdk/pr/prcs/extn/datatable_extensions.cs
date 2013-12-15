using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using pr.util;

namespace pr.extn
{
	public static class DataTableExtensions
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

		/// <summary>An RAII scope for BeginInit/EndInit</summary>
		public static Scope BeginInitScope(this DataView dv)
		{
			return Scope.Create(dv.BeginInit, dv.EndInit);
		}

		/// <summary>An RAII scope for temporarily suspending constraint checking</summary>
		public static Scope SuspendConstraints(this DataSet ds)
		{
			var constraints = ds.EnforceConstraints;
			return Scope.Create(
				() => ds.EnforceConstraints = false,
				() => ds.EnforceConstraints = constraints);
		}

		/// <summary>
		/// An RAII scope for temporarily removing constraints from this table.
		/// It seems this isn't generally a performance improvement because adding the constraints
		/// back causes all rows to be tested against the constraints.</summary>
		public static Scope SuspendConstraints<TDataTable>(this TDataTable dt) where TDataTable :DataTable
		{
			var cons = dt.Constraints.Cast<Constraint>().Select(x => x).ToArray();
			return Scope.Create(
				() => dt.Constraints.Clear(),
				() => dt.Constraints.AddRange(cons));
		}

		// While these are awesome, they're really slow :(
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

	/// <summary>Helper base class for creating typed DataTables using an enum to define the column names</summary>
	[Serializable]
	public abstract class TypedDataTable<TRowType, TColumnEnum> :DataTable
		where TRowType :DataRow
		where TColumnEnum :struct, IConvertible
	{
		protected TypedDataTable(string name, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly)
		{
			// Get the table description attribute (throws if not found => ensures IsEnum)
			var pks = new List<DataColumn>();

			// Create the columns
			var props = typeof(TRowType).GetProperties(flags);
			foreach (var col_desc in Enum.GetValues(typeof(TColumnEnum)).Cast<TColumnEnum>())
			{
				// Find the property with this name
				var col_name = col_desc.ToStringFast();
				var pi = props.FirstOrDefault(x => x.Name == col_name);
				if (pi == null)
					throw new Exception("Row type {0} does not have a property named {1}".Fmt(typeof(TRowType).Name, col_name));

				var attr = typeof(TColumnEnum).GetField(col_name).FindAttribute<DataTableColumnAttribute>() ?? new DataTableColumnAttribute();
				var col = new DataColumn(pi.Name, pi.PropertyType)
					{
						AutoIncrement = attr.AutoInc,
						AutoIncrementSeed = attr.AutoIncSeed,
						AutoIncrementStep = attr.AutoIncStep
					};

				if (attr.PrimaryKey)
					pks.Add(col);

				Columns.Add(col);
			}

			TableName  = name;
			PrimaryKey = pks.ToArray();
		}
		protected TypedDataTable(SerializationInfo info, StreamingContext ctx) : base(info, ctx) {}

		/// <summary>Access a column by enum id</summary>
		public DataColumn Column(TColumnEnum col)
		{
			return Columns[(int)((object)col)];
		}

		/// <summary>Gets the row type.</summary>
		protected override Type GetRowType()
		{
			return typeof(TRowType);
		}

		/// <summary>Creates a new row from an existing row.</summary>
		protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
		{
			return (TRowType)Constructor.Invoke(new object[]{builder});
		}

		/// <summary>Creates a new <see cref="T:System.Data.DataRow"/> with the same schema as the table.</summary>
		public new TRowType NewRow()
		{
			return (TRowType)base.NewRow();
		}

		/// <summary>Add a row to the table. To use this, call 'NewRow' first, fill in the fields, then call AddRow.</summary>
		public void AddRow(TRowType row)
		{
			Rows.Add(row);
		}

		/// <summary>The row constructor for 'TRowType'</summary>
		private ConstructorInfo Constructor
		{
			get
			{
				if (m_constructor == null)
				{
					m_constructor = typeof(TRowType).GetConstructor(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(DataRowBuilder)}, null);
					if (m_constructor == null)
						throw new MissingMethodException("No constructor that takes 'DataRowBuilder'");
				}
				return m_constructor;
			}
		}
		private ConstructorInfo m_constructor;
	}

	/// <summary>Marks a property as a column in a DataTable</summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DataTableColumnAttribute :Attribute
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

		public DataTableColumnAttribute()
		{
			PrimaryKey  = false;
			AutoInc     = false;
			AutoIncSeed = 1;
			AutoIncStep = 1;
			Order       = 0;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using extn;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestExtensions
		{
			public class Record :DataRow
			{
				public enum Columns
				{
					[DataTableColumn(PrimaryKey = true, AutoInc = true)] Id,
					Name,
					Size
				}

				protected internal Record([NotNull] DataRowBuilder builder) :base(builder) {}
				public int Id { get; set; }
				public string Name { get; set; }
				public long Size { get; set; }
			}
			public class Table :TypedDataTable<Record, Record.Columns>
			{
				public Table(string name) :base(name) {}
			}

			[Test] public static void DataTable()
			{
				var tbl = new Table("Record");
				var row = tbl.NewRow();
				row.Name = "First";
				row.Size = 10;
				tbl.AddRow(row);

				Assert.AreEqual("Id"   , tbl.Column(Record.Columns.Id  ).ColumnName);
				Assert.AreEqual("Name" , tbl.Column(Record.Columns.Name).ColumnName);
				Assert.AreEqual("Size" , tbl.Column(Record.Columns.Size).ColumnName);
				Assert.AreEqual(1      , tbl.DefaultView.Count);
			}
		}
	}
}

#endif
