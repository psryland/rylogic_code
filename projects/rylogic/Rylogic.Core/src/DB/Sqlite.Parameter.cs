using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Rylogic.Extn;

namespace Rylogic.Db
{
	public static partial class Sqlite
	{
		/// <summary>A @parameter value for an SQL expression</summary>
		[DebuggerDisplay("{Desc,nq}")]
		public sealed class Parameter : IDbDataParameter
		{
			public Parameter()
			{
				Name = string.Empty;
				Value = null;
				Bind = null!;
			}
			public Parameter(string name, object? value, BindFunc? bind = null)
			{
				Name = name;
				Value = value;
				Bind = bind!;
			}

			/// <summary>The parameter name</summary>
			public string Name { get; set; }

			/// <summary>The parameter value</summary>
			public object? Value { get; set; }

			/// <summary>The function for binding this parameter to a statement. Setting to null is allowed</summary>
			public BindFunc Bind
			{
				get => m_bind ?? BindMap.Bind(Value?.GetType() ?? typeof(DBNull));
				set => m_bind = value;
			}
			private BindFunc? m_bind;

			/// <summary>Human readable description</summary>
			public string Desc => Value != null ? $"{Value.GetType().Name} {Name} = {Value}" : $"DBNull {Name} = null";

			#region IDbDataParameter
			string IDataParameter.ParameterName
			{
				get => Name;
				set => Name = value;
			}
			object IDataParameter.Value
			{
				get => Value!;
				set => Value = value;
			}
			byte IDbDataParameter.Precision
			{
				get => 0;
				set { if (value != 0) throw new NotSupportedException(); }
			}
			byte IDbDataParameter.Scale
			{
				get => 0;
				set { if (value != 0) throw new NotSupportedException(); }
			}
			int IDbDataParameter.Size
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
			DbType IDataParameter.DbType
			{
				get => throw new NotSupportedException(); //SqlType(Value?.GetType() ?? typeof(DBNull)).ToDbType();
				set => throw new NotSupportedException();
			}
			ParameterDirection IDataParameter.Direction
			{
				get => ParameterDirection.Input;
				set { if (value != ParameterDirection.Input) throw new NotSupportedException(); }
			}
			DataRowVersion IDataParameter.SourceVersion
			{
				get;
				set;
			}
			bool IDataParameter.IsNullable
			{
				get => throw new NotImplementedException();
			}
			string IDataParameter.SourceColumn
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
			#endregion
		}

		/// <summary>Parameter collection</summary>
		public class ParameterCollection : IDataParameterCollection
		{
			// Notes:
			//  - IDataParameterCollection enforces an IList interface, but associative better suited.
			private readonly List<Parameter> m_parameters = new();

			/// <summary>Number of parameters in the collection</summary>
			int ICollection.Count => m_parameters.Count;

			/// <summary>Clear the collection</summary>
			public void Clear()
			{
				m_parameters.Clear();
			}

			/// <summary>Add a parameter to the collection</summary>
			public void Add(string name, object? value, BindFunc? bind = null)
			{
				name = $"@{name.TrimStart('@')}";
				m_parameters.Add(new Parameter(name, value, bind));
			}

			/// <summary>Contains parameter 'name'</summary>
			public bool Contains(string name)
			{
				return m_parameters.Any(x => x.Name == name);
			}

			/// <summary>The index of the first parameter matching 'name'</summary>
			public int IndexOf(string name)
			{
				return m_parameters.IndexOf(x => x.Name == name);
			}

			/// <summary>Remove parameter 'name'</summary>
			public void Remove(string name)
			{
				m_parameters.RemoveAll(x => x.Name == name);
			}

			/// <summary>Return the parameter matching 'name'</summary>
			public Parameter this[string name]
			{
				get
				{
					var idx = IndexOf(name);
					if (idx == -1) throw new SqliteException(EResult.Error, $"No parameter with name {name} found", string.Empty);
					return m_parameters[idx];
				}
				set
				{
					var idx = IndexOf(name);
					if (idx == -1) throw new SqliteException(EResult.Error, $"No parameter with name {name} found", string.Empty);
					m_parameters[idx] = value;
				}
			}

			/// <summary>Enumeration</summary>
			public IEnumerator<Parameter> GetEnumerator()
			{
				return m_parameters.GetEnumerator();
			}

			#region IDataParameterCollection
			bool IList.IsFixedSize => false;
			bool IList.IsReadOnly => false;
			int IList.Add(object value)
			{
				var p = (Parameter)value;
				Add(p.Name, p.Value, p.Bind);
				return m_parameters.Count - 1;
			}
			bool IList.Contains(object value)
			{
				var p = (Parameter)value;
				return Contains(p.Name);
			}
			int IList.IndexOf(object value)
			{
				var p = (Parameter)value;
				return IndexOf(p.Name);
			}
			void IList.Insert(int index, object value)
			{
				m_parameters.Insert(index, (Parameter)value);
			}
			void IList.Remove(object value)
			{
				var p = (Parameter)value;
				Remove(p.Name);
			}
			void IDataParameterCollection.RemoveAt(string name)
			{
				Remove(name);
			}
			void IList.RemoveAt(int index)
			{
				m_parameters.RemoveAt(index);
			}
			object IDataParameterCollection.this[string name]
			{
				get => m_parameters[IndexOf(name)].Value!;
				set => m_parameters[IndexOf(name)].Value = value;
			}
			object IList.this[int index]
			{
				get => m_parameters[index];
				set => m_parameters[index] = (Parameter)value;
			}
			void ICollection.CopyTo(Array array, int index)
			{
				m_parameters.CopyTo((Parameter[])array, index);
			}
			bool ICollection.IsSynchronized => false;
			object ICollection.SyncRoot => null!;
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			#endregion
		}
	}
}
