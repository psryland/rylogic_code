using System;
using System.Reflection;
using Newtonsoft.Json;
using Rylogic.Utility;

namespace ExchApi.Common.JsonConverter
{
	public class NullIsDefault<T> : JsonConverter<T>
	{
		/// <inheritdoc/>
		public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return reader.Value is T val ? val : default(T)!;
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
		{
			writer.WriteValue(value);
		}
	}
	public class ParseMethod<T> : JsonConverter<T>
	{
		private static readonly MethodInfo s_parse = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public) ?? throw new NullReferenceException("Method 'Parse' not found");

		/// <inheritdoc/>
		public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.Value is not string val) throw new Exception("Expected a string value");
			return (T?)s_parse.Invoke(null, new object[] { val }) ?? throw new NullReferenceException($"Failed to parse {val} as type {typeof(T).Name}");
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
		{
			writer.WriteValue(value?.ToString());
		}
	}
	public class UnixMSToDateTimeOffset : JsonConverter<DateTimeOffset>
	{
		/// <inheritdoc/>
		public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.Value is not long val) throw new Exception("Expected a 'long' value");
			return DateTimeOffset.FromUnixTimeMilliseconds(val);
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
		{
			writer.WriteValue(value.ToUnixTimeMilliseconds());
		}
	}
	public class ToEnum<T> : JsonConverter<T>
	{
		private readonly bool m_ignore_case;
		public ToEnum()
			: this(true)
		{ }
		public ToEnum(bool ignore_case)
		{
			m_ignore_case = ignore_case;
		}

		/// <inheritdoc/>
		public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return
				reader.Value is string str ? (T)Enum.Parse(typeof(T), str, m_ignore_case) :
				reader.Value is object val ? (T)Enum.ToObject(typeof(T), val) :
				throw new Exception("Expected a string or enum value name");
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
		{
			writer.WriteValue(value?.ToString());
		}
	}
	public class ToUnixSec : JsonConverter<UnixSec>
	{
		/// <inheritdoc/>
		public override UnixSec ReadJson(JsonReader reader, Type objectType, UnixSec existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.Value is not long val) throw new Exception("Expected a 'long' value");
			return new UnixSec(val);
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, UnixSec value, JsonSerializer serializer)
		{
			writer.WriteValue(value.Value);
		}
	}
	public class ToUnixMSec : JsonConverter<UnixMSec>
	{
		/// <inheritdoc/>
		public override UnixMSec ReadJson(JsonReader reader, Type objectType, UnixMSec existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.Value is not long val) throw new Exception("Expected a 'long' value");
			return new UnixMSec(val);
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, UnixMSec value, JsonSerializer serializer)
		{
			writer.WriteValue(value.Value);
		}
	}
}
