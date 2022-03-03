using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace CoinFlip
{
	public static class DapperExtensions
	{
		/// <summary>Wrapper of Dapper method to include cancellation token</summary>
		public static Task<int> ExecuteAsync(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}

		/// <summary>Wrapper of Dapper method to include cancellation token</summary>
		public static Task<T> ExecuteScalarAsync<T>(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}

		/// <summary>Wrapper of Dapper method to include cancellation token</summary>
		public static Task<IEnumerable<dynamic>> QueryAsync(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.QueryAsync(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}
		public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}

		/// <summary>Wrapper of Dapper method to include cancellation token</summary>
		public static Task<dynamic> QuerySingleAsync(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.QuerySingleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}
		public static Task<T> QuerySingleAsync<T>(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.QuerySingleAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}
		public static Task<T> QuerySingleOrDefaultAsync<T>(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}

		/// <summary>Wrapper of Dapper method to include cancellation token</summary>
		public static Task<SqlMapper.GridReader> QueryMultipleAsync(this IDbConnection conn, string sql, object parameters, CancellationToken cancel, IDbTransaction? transaction = null)
		{
			return conn.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancel, transaction: transaction));
		}
	}
}
