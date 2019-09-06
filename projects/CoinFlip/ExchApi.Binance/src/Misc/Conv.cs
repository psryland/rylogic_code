using System;
using Binance.API.DomainObjects;
using Newtonsoft.Json;

namespace Binance.API
{
	internal static class Conv
	{
		public static TimeSpan ToTimeSpan(this EMarketPeriod period, double count = 1)
		{
			switch (period)
			{
			default: throw new Exception($"Unknown market period: {period}");
			case EMarketPeriod.None:      return TimeSpan.Zero;
			case EMarketPeriod.Minutes1:  return TimeSpan.FromMinutes(1 * count);
			case EMarketPeriod.Minutes3:  return TimeSpan.FromMinutes(3 * count);
			case EMarketPeriod.Minutes5:  return TimeSpan.FromMinutes(5 * count);
			case EMarketPeriod.Minutes15: return TimeSpan.FromMinutes(15 * count);
			case EMarketPeriod.Minutes30: return TimeSpan.FromMinutes(30 * count);
			case EMarketPeriod.Hours1:    return TimeSpan.FromHours(1 * count);
			case EMarketPeriod.Hours2:    return TimeSpan.FromHours(2 * count);
			case EMarketPeriod.Hours4:    return TimeSpan.FromHours(4 * count);
			case EMarketPeriod.Hours6:    return TimeSpan.FromHours(6 * count);
			case EMarketPeriod.Hours8:    return TimeSpan.FromHours(8 * count);
			case EMarketPeriod.Hours12:   return TimeSpan.FromHours(12 * count);
			case EMarketPeriod.Day1:      return TimeSpan.FromDays(1 * count);
			case EMarketPeriod.Day3:      return TimeSpan.FromDays(3 * count);
			case EMarketPeriod.Week1:     return TimeSpan.FromDays(7 * count);
			case EMarketPeriod.Month1:    return TimeSpan.FromDays(30.44 * count);
			}
		}
	}
	public class ToCurrencyPair :JsonConverter<CurrencyPair>
	{
		public override CurrencyPair ReadJson(JsonReader reader, Type objectType, CurrencyPair existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return CurrencyPair.Parse((string)reader.Value);
		}
		public override void WriteJson(JsonWriter writer, CurrencyPair value, JsonSerializer serializer)
		{
			writer.WriteValue(value.Id);
		}
	}
}