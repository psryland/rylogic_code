#define COMPILED_LAMBDAS
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Rylogic.Extn;

namespace Rylogic.Db
{
	public static class DB_
	{
		// Notes:
		//  - This is a work in progress. The plan is to create helpers for reflecting types
		//    so they can be mapped to an Sql DB.
		//  - I'm basically porting the parts of sqlite3.cs that are most useful and don't have
		//    a dependency on the native dll interface.

		/// <summary>Convert this type to is equivalent DbType</summary>
		public static DbType DbType(this Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

			return type.Name switch
			{
				"String" => System.Data.DbType.String,
				"Byte[]" => System.Data.DbType.Binary,
				"Boolean" => System.Data.DbType.Boolean,
				"Byte" => System.Data.DbType.Byte,
				"SByte" => System.Data.DbType.SByte,
				"Int16" => System.Data.DbType.Int16,
				"Int32" => System.Data.DbType.Int32,
				"Int64" => System.Data.DbType.Int64,
				"UInt16" => System.Data.DbType.UInt16,
				"UInt32" => System.Data.DbType.UInt32,
				"UInt64" => System.Data.DbType.UInt64,
				"Single" => System.Data.DbType.Single,
				"Double" => System.Data.DbType.Double,
				"Decimal" => System.Data.DbType.Decimal,
				"Guid" => System.Data.DbType.Guid,
				_ => throw new Exception($"Unknown conversion from {type.Name} to DbType"),
			};
		}

		/// <summary>Returns the sqlite data type to use for a given .NET type.</summary>
		public static SqlDbType SqlType(this Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

			if (type == typeof(bool) ||
				type == typeof(byte) ||
				type == typeof(sbyte) ||
				type == typeof(Char) ||
				type == typeof(UInt16) ||
				type == typeof(Int16) ||
				type == typeof(Int32) ||
				type == typeof(UInt32) ||
				type == typeof(Int64) ||
				type == typeof(UInt64) ||
				type == typeof(DateTimeOffset) ||
				//type == typeof(Color) ||
				type.IsEnum)
				return SqlDbType.BigInt;
			if (type == typeof(Single) ||
				type == typeof(Double))
				return SqlDbType.Real;
			if (type == typeof(String) ||
				type == typeof(Guid) ||
				type == typeof(Decimal))
				return SqlDbType.Text;
			if (type == typeof(byte[]) ||
				type == typeof(int[]) ||
				type == typeof(long[]))
				return SqlDbType.Binary;

			throw new NotSupportedException(
				"No default type mapping for type " + type.Name + "\n" +
				"Custom data types should specify a value for the " +
				"'SqlDataType' property in their ColumnAttribute");
		}

		/// <summary>
		/// Controls the mapping from .NET types to database tables.
		/// By default, all public properties (including inherited properties) are used as columns,
		/// this can be changed using the PropertyBindingFlags/FieldBindingFlags properties.</summary>
		[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
		public sealed class TableAttribute : Attribute
		{
			public TableAttribute()
			{
				AllByDefault = true;
				PropertyBindingFlags = BindingFlags.Public;
				FieldBindingFlags = BindingFlags.Default;
				Constraints = null;
				PrimaryKey = null;
				PKAutoInc = false;
			}

			/// <summary>
			/// If true, all properties/fields (selected by the given binding flags) are used as
			/// columns in the created table unless marked with the Sqlite.IgnoreAttribute.<para/>
			/// If false, only properties/fields marked with the Sqlite.ColumnAttribute will be included.<para/>
			/// Default value is true.</summary>
			public bool AllByDefault { get; set; }

			/// <summary>
			/// Binding flags used to reflect on properties in the type.<para/>
			/// Only used if 'AllByDefault' is true. BindingFlag.Instance is added automatically.
			/// Use BindingFlags.Default for none.<para/>
			/// Default value is BindingFlags.Public</summary>
			public BindingFlags PropertyBindingFlags { get; set; }

			/// <summary>
			/// Binding flags used to reflect on fields in the type.<para/>
			/// Only used if 'AllByDefault' is true. BindingFlag.Instance is added automatically.
			/// Use BindingFlags.Default for none.<para/>
			/// Default value is BindingFlags.Default</summary>
			public BindingFlags FieldBindingFlags { get; set; }

			/// <summary>
			/// Defines any table constraints to use when creating the table.<para/>
			/// This can be used to specify multiple primary keys for the table.<para/>
			/// Primary keys are ordered as given in the constraint, starting with Order = 0.<para/>
			/// e.g.<para/>
			///  Constraints = "unique (C1), primary key (C2, C3)"<para/>
			///  Column 'C1' is unique, columns C2 and C3 are the primary keys (in that order)<para/>
			/// Default value is null.</summary>
			public string? Constraints { get; set; }

			/// <summary>
			/// The name of the property or field to use as the primary key for a table.
			/// This property can be used to specify the primary key at a class level which
			/// is helpful if the class is part of an inheritance hierarchy or is a partial
			/// class. This property is used in addition to primary keys specified by
			/// property/field attributes or table constraints. If given, the column Order
			/// value will be set to 0 for that column.<para/>
			/// Default is value is null.</summary>
			public string? PrimaryKey { get; set; }

			/// <summary>
			/// Set to true if the column given by 'PrimaryKey' is also an auto increment
			/// column. Not used if PrimaryKey is not specified. Default is false.</summary>
			public bool PKAutoInc { get; set; }
		}

		/// <summary>Marks a property or field as a column in a table</summary>
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public sealed class ColumnAttribute : Attribute
		{
			public ColumnAttribute()
			{
				PrimaryKey = false;
				AutoInc = false;
				Name = string.Empty;
				Order = 0;
				SqlDataType = null;
				Constraints = null;
			}

			/// <summary>
			/// True if this column should be used as a primary key. If multiple primary keys are specified, ensure the Order
			/// property is used so that the order of primary keys is defined. Default value is false.</summary>
			public bool PrimaryKey { get; set; }

			/// <summary>True if this column should auto increment. Default is false</summary>
			public bool AutoInc { get; set; }

			/// <summary>The column name to use. If null or empty, the member name is used. Default is null</summary>
			public string? Name { get; set; }

			/// <summary>Defines the relative order of columns in the table. Default is '0'</summary>
			public int Order { get; set; }

			/// <summary>
			/// The sqlite data type used to represent this column.
			/// If set to null, then the default mapping from .NET data type to sqlite type is used. Default is null</summary>
			public SqlDbType? SqlDataType { get; set; }

			/// <summary>Custom constraints to add to this column. Default is null</summary>
			public string? Constraints { get; set; }
		}

		/// <summary>Marks a property or field as not a column in the db table for a type.</summary>
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public sealed class IgnoreAttribute : Attribute
		{ }

		/// <summary>Mapping information from a type to columns in the table</summary>
		public sealed class TableMetaData
		{
			/// <summary>Get the meta data for a table based on 'type'</summary>
			public static TableMetaData Get<T>()
			{
				return Get(typeof(T));
			}
			public static TableMetaData Get(Type type)
			{
				if (type == typeof(object))
					throw new ArgumentException("Type 'object' cannot have TableMetaData", "type");

				return new TableMetaData(type);
			}

			/// <summary>
			/// Constructs the meta data for mapping a type to a database table.
			/// By default, 'Activator.CreateInstance' is used as the factory function. This is slow however so use a static delegate if possible</summary>
			private TableMetaData(Type type)
			{
				var column_name_trim = new[] { ' ', '\t', '\'', '\"', '[', ']' };

				// Get the table attribute
				var attrs = type.GetCustomAttributes(typeof(TableAttribute), true);
				var attr = attrs.Length != 0 ? (TableAttribute)attrs[0] : new TableAttribute();

				Type = type;
				Name = type.Name;
				Constraints = attr.Constraints ?? "";
				Factory = () => Type.New();
				TableKind = Kind.Unknown;

				// Tests if a member should be included as a column in the table
				bool IncludeMember(MemberInfo mi, List<string> marked) =>
					!mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Any() &&  // doesn't have the ignore attribute and,
					(mi.GetCustomAttributes(typeof(ColumnAttribute), false).Any() ||  // has the column attribute or,
					(attr != null && attr.AllByDefault && marked.Contains(mi.Name))); // all in by default and 'mi' is in the collection of found columns

				const BindingFlags binding_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				var pflags = attr.PropertyBindingFlags != BindingFlags.Default ? attr.PropertyBindingFlags | BindingFlags.Instance : BindingFlags.Default;
				var fflags = attr.FieldBindingFlags != BindingFlags.Default ? attr.FieldBindingFlags | BindingFlags.Instance : BindingFlags.Default;

				// Create a collection of the columns of this table
				var cols = new List<ColumnMetaData>();
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? new List<string>() :
						type.GetProperties(pflags).Where(x => x.CanRead && x.CanWrite).Select(x => x.Name).Concat(
						type.GetFields(fflags).Select(x => x.Name)).ToList();

					// Check all public/non-public properties/fields
					cols.AddRange(AllProps(type, binding_flags).Where(pi => IncludeMember(pi, mark)).Select(pi => new ColumnMetaData(pi)));
					cols.AddRange(AllFields(type, binding_flags).Where(fi => IncludeMember(fi, mark)).Select(fi => new ColumnMetaData(fi)));

					// If we found read/write properties or fields then this is a normal db table type
					if (cols.Count != 0)
						TableKind = Kind.Table;
				}

				// If no read/write columns were found, look for readonly columns
				if (TableKind == Kind.Unknown)
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? new List<string>() :
						type.GetProperties(pflags).Where(x => x.CanRead).Select(x => x.Name).Concat(
						type.GetFields(fflags).Select(x => x.Name)).ToList();

					// If we find public readonly properties or fields
					cols.AddRange(AllProps(type, binding_flags).Where(pi => IncludeMember(pi, mark)).Select(x => new ColumnMetaData(x)));
					cols.AddRange(AllProps(type, binding_flags).Where(fi => IncludeMember(fi, mark)).Select(x => new ColumnMetaData(x)));
					if (cols.Count != 0)
						TableKind = Kind.AnonType;
				}

				// If still not columns found, check whether 'type' is a primitive type
				if (TableKind == Kind.Unknown)
				{
					cols.Add(new ColumnMetaData(type));
					TableKind = Kind.PrimitiveType;
				}

				// If a primary key is named in the class level attribute, mark it
				if (attr.PrimaryKey != null)
				{
					var col = cols.FirstOrDefault(x => x.Name == attr.PrimaryKey);
					if (col == null) throw new ArgumentException($"Named primary key column '{attr.PrimaryKey}' (given by Sqlite.TableAttribute) is not a found table column for type '{Name} '");
					col.IsPk = true;
					col.IsAutoInc = attr.PKAutoInc;
					col.Order = 0;
				}

				// Check the table constraints for primary key definitions
				const string primary_key = "primary key";
				var pk_ofs = Constraints.IndexOf(primary_key, StringComparison.OrdinalIgnoreCase);
				if (pk_ofs != -1)
				{
					var s = Constraints.IndexOf('(', pk_ofs + primary_key.Length);
					var e = Constraints.IndexOf(')', s + 1);
					if (s == -1 || e == -1) throw new ArgumentException($"Table constraints '{Constraints}' are invalid");

					// Check that every named primary key is actually a column
					// and also ensure primary keys are ordered as given.
					int order = 0;
					foreach (var pk in Constraints.Substring(s + 1, e - s - 1).Split(',').Select(x => x.Trim(column_name_trim)))
					{
						var col = cols.FirstOrDefault(x => x.Name == pk);
						if (col == null) throw new ArgumentException($"Named primary key column '{pk}' was not found as a table column for type '{Name}'");
						col.IsPk = true;
						col.Order = order++;
					}
				}

				// Sort the columns by the given order
				var cmp = Comparer<int>.Default;
				cols.Sort((lhs, rhs) => cmp.Compare(lhs.Order, rhs.Order));

				// Create the column arrays
				Columns = cols.ToArray();
				Pks = Columns.Where(x => x.IsPk).ToArray();
				NonPks = Columns.Where(x => !x.IsPk).ToArray();
				NonAutoIncs = Columns.Where(x => !x.IsAutoInc).ToArray();
				m_single_pk = Pks.Count() == 1 ? Pks.First() : null;

				// Initialise the generated methods for this type
				#if COMPILED_LAMBDAS
				m_method_equal = typeof(MethodGenerator<>).MakeGenericType(Type).GetMethod("Equal", BindingFlags.Static|BindingFlags.Public) ?? throw new Exception("MethodGenerator<> failed");
				m_method_clone = typeof(MethodGenerator<>).MakeGenericType(Type).GetMethod("Clone", BindingFlags.Static|BindingFlags.Public) ?? throw new Exception("MethodGenerator<> failed");
				#endif
			}

			/// <summary>The .NET type that this meta data is for</summary>
			public Type Type { get; private set; }

			/// <summary>The table name (defaults to the type name)</summary>
			public string Name { get; set; }
			public string NameQuoted { get { return "'" + Name + "'"; } }

			/// <summary>Table constraints for this table (default is none)</summary>
			public string Constraints { get; set; }

			/// <summary>Enumerate the column meta data</summary>
			public ColumnMetaData[] Columns { get; private set; }

			/// <summary>The primary key columns, in order</summary>
			public ColumnMetaData[] Pks { get; private set; }

			/// <summary>The non primary key columns</summary>
			public ColumnMetaData[] NonPks { get; private set; }

			/// <summary>The columns that aren't auto increment columns</summary>
			public ColumnMetaData[] NonAutoIncs { get; private set; }

			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount => Columns.Length;

			// 'm_single_pk' is a pointer to the single primary key column for
			// this table or null if the table has multiple primary keys.
			/// <summary>Returns true of there is only one primary key for this type and it is an integer (i.e. an alias for the row id)</summary>
			public bool SingleIntegralPK => m_single_pk?.SqlDataType == SqlDbType.BigInt;
			private readonly ColumnMetaData? m_single_pk;

			/// <summary>True if the table uses multiple primary keys</summary>
			public bool MultiplePK => Pks.Length > 1;

			/// <summary>The kind of table represented by this meta data</summary>
			public Kind TableKind { get; private set; }
			public enum Kind
			{
				Unknown,  // Unknown
				Table,    // A normal sql table
				AnonType, // An anonymous class
				PrimitiveType // A primitive type
			}

			/// <summary>A factory method for creating instances of the type for this table</summary>
			public Func<object> Factory { get; set; }

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData Column(int column_index)
			{
				return Columns[column_index];
			}

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData? Column(string column_name)
			{
				foreach (var c in Columns)
					if (string.CompareOrdinal(c.Name, column_name) == 0)
						return c;

				return null;
			}

			#if false
			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, params object?[] keys)
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != keys.Length) throw new ArgumentException("Incorrect number of primary keys passed for type " + Name);
				if (Pks.Length == 0) throw new ArgumentException("Attempting to bind primary keys for a type without primary keys");

				int idx = 0;
				foreach (var c in Pks)
				{
					var key = keys[idx];
					if (key == null) throw new ArgumentException($"Primary key value {idx + 1} should be of type {c.ClrType.Name} not null");
					if (key.GetType() != c.ClrType) throw new ArgumentException($"Primary key {idx + 1} should be of type {c.ClrType.Name} not type {key.GetType().Name}");
					c.BindFn(stmt, first_idx + idx, key); // binding parameters are indexed from 1 (hence the +1)
					++idx;
				}
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, object key1, object key2) // overload for performance
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != 2) throw new ArgumentException("Insufficient primary keys provided for this table type");
				if (key1.GetType() != Pks[0].ClrType) throw new ArgumentException("Primary key " + 1 + " should be of type " + Pks[0].ClrType.Name + " not type " + key1.GetType().Name);
				if (key2.GetType() != Pks[1].ClrType) throw new ArgumentException("Primary key " + 2 + " should be of type " + Pks[1].ClrType.Name + " not type " + key2.GetType().Name);
				Pks[0].BindFn(stmt, first_idx + 0, key1);
				Pks[1].BindFn(stmt, first_idx + 1, key2);
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, object key1) // overload for performance
			{
				if (first_idx < 1) throw new ArgumentException("parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != 1) throw new ArgumentException("Insufficient primary keys provided for this table type");
				if (key1.GetType() != Pks[0].ClrType) throw new ArgumentException("Primary key " + 1 + " should be of type " + Pks[0].ClrType.Name + " not type " + key1.GetType().Name);
				Pks[0].BindFn(stmt, first_idx, key1);
			}

			/// <summary>
			/// Bind the values of the properties/fields in 'item' to the parameters
			/// in prepared statement 'stmt'. 'ofs' is the index offset of the first
			/// parameter to start binding from.</summary>
			public void BindObj(sqlite3_stmt stmt, int first_idx, object item, IEnumerable<ColumnMetaData> columns)
			{
				if (first_idx < 1) throw new ArgumentException("parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (item.GetType() != Type) throw new ArgumentException("'item' is not the correct type for this table");

				int idx = 0; // binding parameters are indexed from 1
				foreach (var c in columns)
				{
					c.BindFn(stmt, first_idx + idx, c.Get(item));
					++idx;
				}
			}

			/// <summary>Populate the properties and fields of 'item' from the column values read from 'stmt'</summary>
			public object? ReadObj(sqlite3_stmt stmt)
			{
				var column_count = NativeDll.ColumnCount(stmt);
				if (column_count == 0)
					return null;

				object? obj;
				if (TableKind == Kind.Table)
				{
					obj = Factory();
					for (int i = 0; i != column_count; ++i)
					{
						var cname = NativeDll.ColumnName(stmt, i);
						var col = Column(cname);

						// Since sqlite does not support dropping columns in a table, it's likely,
						// for backwards compatibility, that a table will contain columns that don't
						// correspond to a property or field in 'item'. These columns are silently ignored.
						if (col == null) continue;

						col.Set(obj, col.ReadFn(stmt, i));
					}
				}
				else if (TableKind == Kind.AnonType)
				{
					var args = new List<object>();
					for (int i = 0; i != column_count; ++i)
					{
						var cname = NativeDll.ColumnName(stmt, i);
						var col = Column(cname);
						if (col == null) continue;

						args.Add(col.ReadFn(stmt, i));
					}
					obj = Activator.CreateInstance(Type, args.ToArray(), null);
				}
				else if (TableKind == Kind.PrimitiveType)
				{
					var col = Column(0);
					obj = col.ReadFn(stmt, 0);
				}
				else
				{
					obj = null;
				}
				return obj;
			}
			#endif

			/// <summary>Returns true of 'lhs' and 'rhs' are equal instances of this table type</summary>
			public bool Equal(object lhs, object rhs)
			{
				Debug.Assert(lhs.GetType() == Type, "'lhs' is not the correct type for this table");
				Debug.Assert(rhs.GetType() == Type, "'rhs' is not the correct type for this table");

#if COMPILED_LAMBDAS
				return (bool)m_method_equal.Invoke(null, new[]{lhs, rhs})!;
#else
				return MethodGenerator.Equal(lhs, rhs);
#endif
			}

#if COMPILED_LAMBDAS

			/// <summary>Compiled lambda method for testing two 'Type' instances as equal</summary>
			private readonly MethodInfo m_method_equal;

			/// <summary>Compiled lambda method for returning a shallow copy of an object</summary>
			private readonly MethodInfo m_method_clone;

			/// <summary>Helper class for generating compiled lambda expressions</summary>
			private static class MethodGenerator<T>
			{
				/// <summary>Test two instances of 'T' for having equal fields</summary>
				public static bool Equal(object lhs, object rhs) { return m_func_equal((T)lhs, (T)rhs); }
				private static readonly Func<T,T,bool> m_func_equal = EqualFunc();
				private static Func<T,T,bool> EqualFunc()
				{
					var lhs = Expression.Parameter(typeof(T), "lhs");
					var rhs = Expression.Parameter(typeof(T), "rhs");
					Expression body = Expression.Constant(true);
					foreach (var f in AllFields(typeof(T), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
						body = Expression.AndAlso(Expression.Equal(Expression.Field(lhs, f), Expression.Field(rhs, f)), body);
					return Expression.Lambda<Func<T,T,bool>>(body, lhs, rhs).Compile();
				}

				/// <summary>Returns a shallow copy of 'obj' as a new instance</summary>
				public static object Clone(object obj) { return m_func_clone((T)obj)!; }
				private static readonly Func<T,T> m_func_clone = CloneFunc();
				private static Func<T,T> CloneFunc()
				{
					var p = Expression.Parameter(typeof(T), "obj");
					var bindings = AllFields(typeof(T), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
						.Select(x => (MemberBinding)Expression.Bind(x, Expression.Field(p,x)));
					Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
					return Expression.Lambda<Func<T,T>>(body, p).Compile();
				}
			}

#else

			/// <summary>Helper class for housing the runtime equivalents of the methods in MethodGenerator(T)</summary>
			private static class MethodGenerator
			{
				public static bool Equal(object lhs, object rhs)
				{
					if (lhs.GetType() != rhs.GetType()) return false;
					foreach (var f in AllFields(lhs.GetType(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
						if (!Equals(f.GetValue(lhs), f.GetValue(rhs)))
							return false;
					return true;
				}
				public static object Clone(object obj)
				{
					object clone = Activator.CreateInstance(obj.GetType());
					foreach (var f in AllFields(obj.GetType(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
						f.SetValue(clone, f.GetValue(obj));
					return clone;
				}
			}

#endif

			/// <summary>Returns a table declaration string for this table</summary>
			public string Decl()
			{
				var sb = new StringBuilder();
				sb.Append(string.Join(",\n", Columns.Select(c => c.ColumnDef(m_single_pk != null))));
				if (!string.IsNullOrEmpty(Constraints))
				{
					if (sb.Length != 0) sb.Append(",\n");
					sb.Append(Constraints);
				}
				return sb.ToString();
			}

			/// <summary>
			/// Returns the constraint string for the primary keys of this table.<para/>
			/// i.e. select * from Table where {Index = ? and Key2 = ?}-this bit</summary>
			public string PkConstraints()
			{
				return m_single_pk != null
					? m_single_pk.NameBracketed + " = ?"
					: string.Join(" and ", Pks.Select(x => x.NameBracketed + " = ?"));
			}

#if false
			/// <summary>Updates the value of the auto increment primary key (if there is one)</summary>
			public void SetAutoIncPK(object obj, sqlite3 db)
			{
				if (m_single_pk == null || !m_single_pk.IsAutoInc) return;
				int id = (int)NativeDll.LastInsertRowId(db);
				m_single_pk.Set(obj, id);
			}
#endif

			/// <summary>Returns all inherited properties for a type</summary>
			private static IEnumerable<PropertyInfo> AllProps(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object) || type.BaseType == null) return Enumerable.Empty<PropertyInfo>();
				return AllProps(type.BaseType, flags).Concat(type.GetProperties(flags | BindingFlags.DeclaredOnly));
			}

			/// <summary>Returns all inherited fields for a type</summary>
			private static IEnumerable<FieldInfo> AllFields(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object) || type.BaseType == null) return Enumerable.Empty<FieldInfo>();
				return AllFields(type.BaseType, flags).Concat(type.GetFields(flags | BindingFlags.DeclaredOnly));
			}
		}

		/// <summary>A column within a db table</summary>
		public sealed class ColumnMetaData
		{
			private ColumnMetaData(MemberInfo? mi, Type type, Func<object, object?> get, Action<object, object?> set)
			{
				var attr = (ColumnAttribute?)null;
				attr ??= (ColumnAttribute?)mi?.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
				attr ??= new ColumnAttribute();
				
				var is_nullable = Nullable.GetUnderlyingType(type) != null;

				// Setup accessors
				Get = get;
				Set = set;

				MemberInfo = mi;
				Name = attr.Name ?? mi?.Name ?? string.Empty;
				SqlDataType = attr.SqlDataType ?? type.SqlType();
				Constraints = attr.Constraints ?? "";
				IsPk = attr.PrimaryKey;
				IsAutoInc = attr.AutoInc;
				IsNotNull = type.IsValueType && !is_nullable;
				IsCollate = false;
				Order = OrderBaseValue + attr.Order;
				ClrType = type;

				//// Set up the bind and read methods
				//BindFn = Bind.FuncFor(type);
				//ReadFn = Read.FuncFor(type);
			}
			internal ColumnMetaData(PropertyInfo pi)
				: this(pi, pi.PropertyType, obj => pi.GetValue(obj, null), (obj, val) => pi.SetValue(obj, val, null))
			{}
			internal ColumnMetaData(FieldInfo fi)
				:this(fi, fi.FieldType, fi.GetValue, fi.SetValue)
			{}
			internal ColumnMetaData(Type type)
				:this(null, type, obj => obj, (obj, val) => throw new NotImplementedException())
			{}

			/// <summary>The member info for the member represented by this column</summary>
			public MemberInfo? MemberInfo;

			/// <summary>The name of the column</summary>
			public string Name;
			public string NameBracketed => $"[{Name}]";

			/// <summary>The data type of the column</summary>
			public SqlDbType SqlDataType;

			/// <summary>Column constraints for this column</summary>
			public string Constraints;

			/// <summary>True if this column is a primary key</summary>
			public bool IsPk;

			/// <summary>True if this column is an auto increment column</summary>
			public bool IsAutoInc;

			/// <summary>True if this column cannot be null</summary>
			public bool IsNotNull;

			/// <summary>True for collate columns</summary>
			public bool IsCollate;

			/// <summary>An ordering field used to define the column order in a table</summary>
			public int Order;

			/// <summary>The .NET type of the property or field on the object</summary>
			public Type ClrType;

			/// <summary>Returns the value of this column from an object of type 'ClrType'</summary>
			public Func<object, object?> Get { get; set; }

			/// <summary>Sets the value of this column in an object of type 'ClrType'</summary>
			public Action<object, object?> Set { get; set; }

			///// <summary>Binds the value from this column to parameter 'index' in 'stmt'</summary>
			//public Bind.Func BindFn;

			///// <summary>Reads column 'index' from 'stmt' and sets the corresponding property or field in 'obj'</summary>
			//public Read.Func ReadFn;

			/// <summary>Returns the column definition for this column</summary>
			public string ColumnDef(bool incl_pk)
			{
				var sql_type = SqlDataType.ToString().ToLowerInvariant();
				var pk = incl_pk && IsPk ? "primary key " : "";
				var ainc = IsAutoInc ? "autoincrement " : "";
				return $"[{Name}] {sql_type} {pk} {ainc} {Constraints}";
			}

			/// <summary></summary>
			public override string ToString()
			{
				var sb = new StringBuilder();
				if (IsPk) sb.Append("*");
				sb.Append("[").Append(SqlDataType).Append("]");
				sb.Append(" ").Append(Name);
				if (IsAutoInc) sb.Append("<auto inc>");
				if (IsNotNull) sb.Append("<not null>");
				sb.Append(" ").Append("(").Append(ClrType.Name).Append(")");
				return sb.ToString();
			}

			public const int OrderBaseValue = 0xFFFF;
		}
	}
}
