using System.Data;
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
	}
}
