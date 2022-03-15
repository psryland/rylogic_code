using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Rylogic.Extn;

namespace Rylogic.Db
{
	public static partial class Sqlite
	{
		/// <summary>Table helper</summary>
		[DebuggerDisplay("{Name,nq}")]
		public sealed class Table<Item> : IEnumerable<Item>, IEnumerable
		{
			// Notes:
			//  - This type is a helper for tables based on a type
			//  - It's not intended to be the main access to a database

			public Table(Connection db)
				:this(db, GenerateColumns())
			{}
			public Table(Connection db, params ColumnDef[] columns)
				: this(db, columns.AsEnumerable())
			{ }
			public Table(Connection db, IEnumerable<ColumnDef> columns)
			{
				Connection = db;
				Name = typeof(Item).Name;
				Columns = new ColumnCollection(columns);
			}

			/// <summary>The database connection to use</summary>
			public Connection Connection { get; set; }

			/// <summary>Table name</summary>
			public string Name { get; }

			/// <summary>Column definitions</summary>
			public ColumnCollection Columns { get; }

			/// <summary>Test if a table exists in the database</summary>
			public bool Exists => Connection.Cmd($"select count(*) from sqlite_master where type='table' and name='{Name}'").Scalar<int>() != 0;

			/// <summary>Get the number of rows in this table</summary>
			public int RowCount => Connection.Cmd($"select count(*) from {Name}").Scalar<int>();

			/// <summary>Drop the table</summary>
			public int Drop(bool if_exists = true)
			{
				var if_exists_s = if_exists ? "if exists " : string.Empty;
				return Connection.Cmd($"drop table {if_exists_s} {Name}").Execute();
			}

			/// <summary>Create the table</summary>
			public int Create(ECreateConstraint constraint = ECreateConstraint.Default)
			{
				return Connection.Cmd(
					$"create table {constraint.ToSql()} {Name} (\n" +
					$"{string.Join("\n", Columns.Select(x => $"  [{x.Name}] {x.Constraints},")).Trim(',')}\n" +
					$")").Execute();
			}

			/// <summary>Insert an object into the table</summary>
			public int Insert(object obj, EInsertConstraint constraint = EInsertConstraint.Default)
			{
				// This is intended for basic use, where you want to insert a whole object as a row.
				// It is slow because of reflection on property accessors
				using var cmd = Connection.Cmd(
					$"insert {constraint.ToSql()} into {Name} (\n" +
					$"{string.Join("\n", Columns.Select(x => $"  [{x.Name}],")).Trim(',')}\n" +
					$") values (\n" +
					$"  {string.Join(", ", Columns.Select(x => $"@{x.Name.ToLowerInvariant()}"))}\n" +
					$")");

				// Bind parameters
				foreach (var col in Columns)
					cmd.AddParam(col.Name.ToLowerInvariant(), col.GetValue(obj));

				// Run the query
				return cmd.Execute();
			}

			/// <summary>Enumerable rows in the table</summary>
			public IEnumerable<Item> Rows()
			{
				return Connection.Cmd($"select * from {Name}").ExecuteQuery<Item>();
			}

			/// <summary>Return a reader interface to the table</summary>
			public DataReader Reader() // Remmeber to Dispose
			{
				return Connection.Cmd($"select * from {Name}").ExecuteQuery();
			}

			/// <summary>Return the columns for 'Item'</summary>
			public static IEnumerable<ColumnDef> GenerateColumns()
			{
				static string sql_type(Type t) => SqlColumnType(t).ToString().ToLowerInvariant();
				static string allow_null(Type t) => !t.IsClass && Nullable.GetUnderlyingType(t) == null ? "not null" : string.Empty;

				foreach (var pi in typeof(Item).GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					if (pi.Name == "ID") // Special case
						yield return new ColumnDef(pi.Name, pi.PropertyType, "integer unique primary key autoincrement");
					else
						yield return new ColumnDef(pi.Name, pi.PropertyType, $"{sql_type(pi.PropertyType)} {allow_null(pi.PropertyType)}");
				}
				foreach (var fi in typeof(Item).GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					if (fi.Name == "ID") // Special case
						yield return new ColumnDef(fi.Name, fi.FieldType, "integer unique primary key autoincrement");
					else
						yield return new ColumnDef(fi.Name, fi.FieldType, $"{sql_type(fi.FieldType)} {allow_null(fi.FieldType)}");
				}
			}

			/// <summary>A collection of column definitions</summary>
			public class ColumnCollection :IEnumerable<ColumnDef>
			{
				private readonly List<ColumnDef> m_columns;

				public ColumnCollection()
				{
					m_columns = new List<ColumnDef>();
				}
				public ColumnCollection(IEnumerable<ColumnDef> columns)
				{
					m_columns = new List<ColumnDef>(columns);
				}
				public int Count
				{
					get => m_columns.Count;
				}
				public void Clear()
				{
					m_columns.Clear();
				}
				public void Add(ColumnDef column)
				{
					m_columns.Add(column);
				}
				public int IndexOf(string name)
				{
					return m_columns.IndexOf(x => x.Name == name);
				}
				public ColumnDef this[int i]
				{
					get => m_columns[i];
					set => m_columns[i] = value;
				}
				public ColumnDef this[string name]
				{
					get => m_columns[IndexOf(name)];
					set => m_columns[IndexOf(name)] = value;
				}

				#region IEnumerable
				public IEnumerator<ColumnDef> GetEnumerator() => m_columns.GetEnumerator();
				IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
				#endregion
			}

			/// <summary>Column description</summary>
			[DebuggerDisplay("[{Name,nq}] {Constraints,nq}")]
			public class ColumnDef
			{
				public ColumnDef(string name, Type ty, string constraints)
				{
					Name = name;
					Type = ty;
					Constraints = constraints.ToLowerInvariant();
					ColumnType = Enum.TryParse(Constraints.FirstWord(), true, out EDataType ct) ? ct : throw new SqliteException(EResult.Error, $"Unknown column data type");
					PrimaryKey = Constraints.Contains("primary key");
					AutoIncrement = Constraints.Contains("autoincrement");
					NotNull = Constraints.Contains("not null");
					DefaultValue = NotNull ? ty.DefaultInstance() ?? ColumnType.DefaultValue() : null;
					m_value = null;
				}

				/// <summary>Column name</summary>
				public string Name { get; }

				/// <summary>The type of the column</summary>
				public Type Type { get; }

				/// <summary>Column constraints</summary>
				public string Constraints { get; }

				/// <summary>Column type</summary>
				public EDataType ColumnType { get; }

				/// <summary>True if this column is a primary key</summary>
				public bool PrimaryKey { get; }

				/// <summary>True if this column has the auto increment constraint</summary>
				public bool AutoIncrement { get; }

				/// <summary>True if this column is has the not-null constraint</summary>
				public bool NotNull { get; }

				/// <summary>The default value to use for the column</summary>
				public object? DefaultValue { get; set; }

				/// <summary>Read the value of 'Name' from 'obj'</summary>
				public object? GetValue(object obj)
				{
					if (m_value == null || m_value.ReflectedType != obj.GetType())
					{
						m_value =
							obj.GetType().GetProperty(Name) ??
							obj.GetType().GetField(Name) ??
							(MemberInfo?)null;
					}
					var value =
						m_value is PropertyInfo pi ? pi.GetValue(obj) :
						m_value is FieldInfo fi ? fi.GetValue(obj) :
						DefaultValue;
					return value;
				}
				private MemberInfo? m_value;
			}

			#region IEnumerable
			IEnumerator<Item> IEnumerable<Item>.GetEnumerator() => Rows().GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => Rows().GetEnumerator();
			#endregion
		}
	}
}
