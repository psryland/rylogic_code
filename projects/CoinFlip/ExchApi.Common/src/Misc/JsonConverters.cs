using System;
using System.Reflection;
using Newtonsoft.Json;
using Rylogic.Extn;
using Rylogic.Utility;

namespace ExchApi.Common.JsonConverter
{
	public class ParseMethod<T> : JsonConverter<T>
	{
		private static readonly MethodInfo s_parse = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);
		public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return (T)s_parse.Invoke(null, new object[] { (string)reader.Value });
		}
		public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
		{
			writer.WriteValue(value.ToString());
		}
	}
	public class UnixMSToDateTimeOffset : JsonConverter<DateTimeOffset>
	{
		public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return DateTimeOffset.FromUnixTimeMilliseconds((long)reader.Value);
		}
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
		public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return reader.Value is string str
				? (T)Enum.Parse(typeof(T), str, m_ignore_case)
				: (T)Enum.ToObject(typeof(T), reader.Value);
		}
		public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
	public class ToUnixSec : JsonConverter<UnixSec>
	{
		public override UnixSec ReadJson(JsonReader reader, Type objectType, UnixSec existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return new UnixSec((long)reader.Value);
		}
		public override void WriteJson(JsonWriter writer, UnixSec value, JsonSerializer serializer)
		{
			writer.WriteValue(value.Value);
		}
	}
	public class ToUnixMSec : JsonConverter<UnixMSec>
	{
		public override UnixMSec ReadJson(JsonReader reader, Type objectType, UnixMSec existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return new UnixMSec((long)reader.Value);
		}
		public override void WriteJson(JsonWriter writer, UnixMSec value, JsonSerializer serializer)
		{
			writer.WriteValue(value.Value);
		}
	}
}
