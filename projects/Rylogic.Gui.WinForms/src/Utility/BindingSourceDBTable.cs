using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Db;
using Rylogic.Extn;

namespace Rylogic.Gui.WinForms
{
	/// <summary>Provides a BindingSource-like interface to a DB table</summary>
	public class BindingSourceDbTable<Type> :IEnumerable<Type> ,IEnumerable ,IList<Type>
	{
		// Expected use:
		//  - An instance of this class can exist longer than the DB and table it's based on
		//    (just like a binding source exists longer than its DataSource).
		//  - Clients set the DataSource (which is a DB connection and base table)
		//  - This class provides functionality to sort, filter, and access the data.
		//  - The interface is an array-like interface, with 'Count' and 'this[idx]'
		//    methods providing array-like access.
		//  - The 'base_table_name' table is unchanged, sorting filtered etc occurs
		//    in an index table with the name 'base_table_name_BS'

		public BindingSourceDbTable(string base_table_name)
		{
			var meta = Sqlite.TableMetaData.GetMetaData<Type>();
			if (meta.Pks.Length != 1)
				throw new Exception("This class expects 'Type' to have a single integer primary key");

			DB             = null;
			Cache          = new Cache<int, Type>(10000);
			BaseTableName  = base_table_name;
			Count          = 0;
			Filter         = null;
			SortColumn     = null;
			SortAscending  = true;
			UpdateRequired = false;
			Position       = -1;
		}

		/// <summary>The name of the table that the binding source is based on</summary>
		public string BaseTableName { get; private set; }

		/// <summary>The name of the table containing indices of filtered/sorted/etc entries in BaseTableName</summary>
		public string TableName
		{
			get { return BaseTableName + TableNameExtn; }
		}
		private const string TableNameExtn = "_BS";

		/// <summary>The DB connection</summary>
		public Sqlite.Database DB
		{
			get { return m_db; }
			set
			{
				if (m_db == value) return;

				// Notify of the pending reset of the binding source table
				ListChanging?.Invoke(this, new ListChgEventArgs<Type>(this, ListChg.PreReset, -1, default(Type)));

				if (m_db != null)
				{
					Count = 0;
				}
				m_db = value;
				if (m_db != null)
				{
					// Note: Don't use the DB callback function to detect DB changes, they occur
					// too frequently. Instead, use direct updating. At points in the code that
					// cause the DB to be invalidated, call Update().

					// Ensure there is no existing binding source table
					m_db.Execute($"drop table if exists {TableName}");
				}

				// Update the binding source table
				Update(raise_events:false);

				// Notify of the new data
				ListChanging?.Invoke(this, new ListChgEventArgs<Type>(this, ListChg.Reset, -1, default(Type)));
			}
		}
		private Sqlite.Database m_db;

		/// <summary>A cache of items by array index</summary>
		private Cache<int, Type> Cache { get; set; }

		/// <summary>The name (bracketed) of the primary key for type 'T'</summary>
		private string Pk
		{
			get
			{
				var meta = Sqlite.TableMetaData.GetMetaData<Type>();
				return BaseTableName.HasValue() ? $"{BaseTableName}.{meta.Pks[0].NameBracketed}" : meta.Pks[0].NameBracketed;
			}
		}

		/// <summary>A sql join sub-expression</summary>
		public string Join
		{
			get { return m_impl_join; }
			set
			{
				if (m_impl_join == value) return;
				m_impl_join = value;
				if (DB != null) Invalidate();
			}
		}
		private string m_impl_join;

		/// <summary>An sql where clause that will be appended to a select statement</summary>
		public string Filter
		{
			get { return m_impl_filter; }
			set
			{
				if (m_impl_filter == value) return;
				m_impl_filter = value;
				FilterChanged?.Invoke(this, EventArgs.Empty);
				if (DB != null) Invalidate();
			}
		}
		private string m_impl_filter;

		/// <summary>The column to sort by. Set to null or empty string for no sorting</summary>
		public string SortColumn
		{
			get { return m_impl_sort_column; }
			set
			{
				if (m_impl_sort_column == value) return;
				m_impl_sort_column = value;

				if (DB != null && value != null && !Sqlite.TableMetaData.GetMetaData<Type>().Columns.Any(x => x.Name == value))
					throw new Exception($"No column called {value}");

				if (DB != null)
					Invalidate();
			}
		}
		private string m_impl_sort_column;

		/// <summary>The column sort direction</summary>
		public bool SortAscending
		{
			get { return m_impl_sort_ascending; }
			set
			{
				if (m_impl_sort_ascending == value) return;
				m_impl_sort_ascending = value;
				if (DB != null) Invalidate();
			}
		}
		private bool m_impl_sort_ascending;

		/// <summary>The order by expression implied by 'SortColumn' and 'SortAscending'</summary>
		public string OrderBy
		{
			get { return SortColumn.HasValue() ? $"{BaseTableName}.[{SortColumn}] {(SortAscending ? string.Empty : "desc")}" : string.Empty; }
		}

		/// <summary>Raised whenever the collection is marked as invalid</summary>
		public event EventHandler Invalidated;

		/// <summary>Raised whenever the number of items in the collection changes</summary>
		public event EventHandler<ListChgEventArgs<Type>> ListChanging;

		/// <summary>Raised whenever the Filter expression has changed</summary>
		public event EventHandler FilterChanged;

		/// <summary>Notify observers of the entire source changing</summary>
		public void ResetBindings()
		{
			Update(raise_events:true);
		}
		
		/// <summary>Empty the binding source table</summary>
		public void Clear()
		{
			// Notify of the pending reset of the binding source table
			ListChanging?.Invoke(this, new ListChgEventArgs<Type>(this, ListChg.PreReset, -1, default(Type)));

			Count = 0;

			// Notify of binding source data reset
			ListChanging?.Invoke(this, new ListChgEventArgs<Type>(this, ListChg.Reset, -1, default(Type)));
		}

		/// <summary>Flag the table as out of date</summary>
		public void Invalidate(object sender = null, EventArgs args = null)
		{
			if (!UpdateRequired) Invalidated?.Invoke(this, EventArgs.Empty);
			UpdateRequired = true;
		}

		/// <summary>True if the data source is invalid and requires updating</summary>
		public bool UpdateRequired { get; private set; }

		/// <summary>Update the binding source table if flagged as UpdateRequired</summary>
		public void UpdateIfNecessary(bool raise_events = true)
		{
			if (!UpdateRequired) return;
			Update(raise_events);
		}

		/// <summary>Updates the data table based on current filter, sorting, etc.</summary>
		public void Update(bool raise_events = true)
		{
			// Invalidate to cause an Invalidated event before every update
			if (!UpdateRequired) Invalidate();
			UpdateRequired = false;

			// No data source, no update
			if (DB == null)
			{
				Count = 0;
				Cache.Flush();
				return;
			}

			// Check for threading issues
			// TODO: this can take a long time. It needs to be asynchronous somehow
			Debug.Assert(DB.AssertCorrectThread());

			// Generate the new binding source table in a temp table allowing use
			// of the previous table while the 'Filtering' is happening.
			var tmp_table = TableName + "_TMP";

			// Create the temporary table
			using (var t = DB.NewTransaction())
			{
				DB.Execute($"drop table if exists {tmp_table}");
				DB.Execute(
					$"create table {tmp_table} (\n"+
					"  [Idx] integer primary key autoincrement,\n"+
					"  [Key] integer\n" +
					")");

				// Populate from the base table using the filters
				DB.Execute(
					$"insert into {tmp_table} ([Key])\n"+
					$"select {Pk} from {BaseTableName}\n"+
					(Join   .HasValue() ? $" join {Join}" : "") +
					(Filter .HasValue() ? $"{(Join.Contains(" where ") ? " and " : " where ")} {Filter}" : "") +
					(OrderBy.HasValue() ? $" order by {OrderBy}" : ""));

				t.Commit();
			}

			// Notify of the pending reset of the binding source table
			if (raise_events)
				ListChanging?.Invoke(this, new ListChgEventArgs<Type>(this, ListChg.PreReset, -1, default(Type)));

			// Drop the old table and rename the temporary table to the new binding source table
			using (var t = DB.NewTransaction())
			{
				DB.Execute($"drop table if exists {TableName}");
				DB.Execute($"alter table {tmp_table} rename to {TableName}");
				t.Commit();
			}

			// Get the new table count
			Count = DB.ExecuteScalar($"select count(*) from {TableName}");
			Cache.Flush();

			// Notify of binding source data reset
			if (raise_events)
				ListChanging?.Invoke(this, new ListChgEventArgs<Type>(this, ListChg.Reset, -1, default(Type)));
		}

		/// <summary>Returns the number of items available</summary>
		public int Count { get; private set; }

		/// <summary>Get/Set an item by array index</summary>
		public Type this[int idx]
		{
			get
			{
				// Check you are passing the array index into the filtered table, not the primary key of the item you want
				if (idx >= Count)
					throw new IndexOutOfRangeException($"Index {idx} is out of Range [0,{Count})");

				// Don't use the indexer to update the table, Update() can change the value of 'Count'
				if (UpdateRequired)
					throw new Exception("Table is out of date, call Update()");

				// Sanity checks - expensive...
				//#if DEBUG
				//var base_table_count = DB.ExecuteScalar(Sqlite.Sql("select count(*) from ", BaseTableName));
				//var table_count      = DB.ExecuteScalar(Sqlite.Sql("select count(*) from ", TableName));
				//Debug.Assert(base_table_count >= table_count, "The binding source table is older than the base table. A call to Update() is missing");
				//Debug.Assert(table_count == Count, "Count not up to date. A call to Update() is missing");
				//#endif

				// If 'idx' is not cached, preload the cache with items around 'idx'
				// Check 'idx+1' is not cached as well, if it is we probably don't need to preload
				if (!Cache.IsCached(idx))
				{
					int from = Math.Max(idx - 250, 0), count = 1000;
					var sql = $"select {BaseTableName}.* from {TableName} join {BaseTableName} on [Key] = {Pk} limit ?,?";
					Cache.Add(Enumerable.Range(from,count), DB.EnumRows<Type>(sql, 1, new object[]{from,count}));
				}

				return Cache.Get(idx, k =>
				{
					var sql = $"select {BaseTableName}.* from {TableName} join {BaseTableName} on [Key] = {Pk} where [Idx] = ?";
					var item = DB.EnumRows<Type>(sql, 1, new object[]{k+1}).First();
					return item;
				});
			}
			set
			{
				// Update the item in the base table
				// This will result in an ItemReset event when the change happens in the DB
				// It will also invalidate the cache for that item
				DB.Table<Type>().Update(value);
			}
		}

		/// <summary>Return the array-index of the next item in the filtered table that matches the given predicate</summary>
		public int IndexOf(string where_clause, int first_idx = 1, IEnumerable<object> parms = null)
		{
			var sql = Sqlite.Sql("select [Idx] from ",TableName," join ",BaseTableName," on [Key] = ",Pk," where ",where_clause);
			return DB.EnumRows<int>(sql, first_idx, parms).FirstOrDefault() - 1;
		}

		/// <summary>Get the currently selected item</summary>
		public Type Current
		{
			// No set, because we can't search the DB for a ReferenceEquals(value)
			get { return Position >= 0 && Position < Count ? this[Position] : default(Type); }
		}

		/// <summary>The currently active position in the data source</summary>
		public int Position
		{
			get { return m_impl_position; }
			set
			{
				if (m_impl_position == value) return;
				var old = m_impl_position;
				m_impl_position = value;
				OnPositionChanged(new PositionChgEventArgs(old, value));
			}
		}
		private int m_impl_position;

		/// <summary>Raised when the current position is changed</summary>
		public event EventHandler<PositionChgEventArgs> PositionChanged;

		/// <summary>Called when the current position is changed</summary>
		protected virtual void OnPositionChanged(PositionChgEventArgs args)
		{
			PositionChanged?.Invoke(this, args);
		}

		#region IEnumerable

		/// <summary>Returns the enumerator to iterator through all table rows</summary>
		IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
		{
			if (DB == null)
				return Enumerable.Empty<Type>().GetEnumerator();

			Debug.Assert(DB.AssertCorrectThread());
			var sql = Sqlite.Sql("select ",BaseTableName,".* from ",TableName," join ",BaseTableName," on [Key]=[Id]");
			return DB.EnumRows<Type>(sql).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Type>)this).GetEnumerator();
		}

		#endregion

		#region IList<T>

		public bool IsReadOnly
		{
			get { return false; }
		}
		public int IndexOf(Type item)
		{
			return this.IndexOf(x => Equals(x,item));
		}
		public void Insert(int index, Type item)
		{
			throw new NotImplementedException();
		}
		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}
		public bool Remove(Type item)
		{
			throw new NotImplementedException();
		}
		public void Add(Type item)
		{
			throw new NotImplementedException();
		}
		public bool Contains(Type item)
		{
			return this.Any(x => Equals(x,item));
		}
		public void CopyTo(Type[] array, int arrayIndex)
		{
			if (array.Length - arrayIndex < Count) throw new Exception($"array too small in {GetType().Name}.CopyTo");
			foreach (var x in this)
				array[arrayIndex++] = x;
		}

		#endregion
	}
}
