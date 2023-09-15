using System.Text.Json;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADUFO
{
	public static class Misc
	{
		/// <summary>Convert the string to a work item query</summary>
		public static Wiql AsQuery(this string query)
		{
			return new Wiql { Query = query };
		}
	}

	/// <summary>System.Text.Json extensions</summary>
	public static class Json_
	{
		/// <summary>True if the current token is 'type'</summary>
		public static bool IsToken(this Utf8JsonReader reader, JsonTokenType type)
		{
			return reader.TokenType == type;
		}

		/// <summary>Expect the current token to be 'type', and advance to the next token</summary>
		public static void Expect(this Utf8JsonReader reader, JsonTokenType type)
		{
			if (reader.TokenType != type)
				throw new JsonException($"Parse error at {reader.Position}. Expected {type} but found {reader.TokenType}");
			if (!reader.Read())
				throw new JsonException($"Parse error at {reader.Position}. Failed to read token: {type}");
		}

		/// <summary>If the current token is a property name, return true and the property, otherwise false</summary>
		public static bool GetPropertyName(this Utf8JsonReader reader, out string prop)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				prop = string.Empty;
				return false;
			}
			prop = reader.GetString() ?? throw new JsonException($"Parse error at {reader.Position}. Property name expected");
			return true;
		}
	}
}
