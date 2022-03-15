using System;
using System.Runtime.Serialization;

namespace Rylogic.Db
{
	/// <summary>An exception type specifically for sqlite exceptions</summary>
	[Serializable]
	public class SqliteException : Exception
	{
		public SqliteException()
			:this(Sqlite.EResult.Error, string.Empty)
		{}
		public SqliteException(Sqlite.EResult res, string message, string? sql_error_msg = null, Exception? inner_exception = null)
			: base(message, inner_exception)
		{
			Result = res;
			SqlErrMsg = sql_error_msg ?? string.Empty;
		}
		protected SqliteException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>The result code associated with this exception</summary>
		public Sqlite.EResult Result { get; }

		/// <summary>The sqlite error message</summary>
		public string SqlErrMsg { get; }

		/// <summary></summary>
		public override string ToString() => $"{Result} - {Message}";
	}
}
