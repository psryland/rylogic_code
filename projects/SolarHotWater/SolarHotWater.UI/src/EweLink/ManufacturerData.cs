using Newtonsoft.Json.Linq;

namespace EweLink
{
	/// <summary></summary>
	public class ManufacturerData
	{
		private readonly JObject m_jmanu;
		public ManufacturerData(JObject? jmanu)
		{
			m_jmanu = jmanu ?? new JObject();
		}

		/// <summary></summary>
		public EDeviceType UIID
		{
			get => (EDeviceType)(m_jmanu["uiid"]?.Value<int>() ?? 0);
			set => m_jmanu["uiid"] = (int)value;
		}

		/// <summary></summary>
		public string Description
		{
			get => m_jmanu["description"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["description"] = value;
		}

		/// <summary></summary>
		public string BrandID
		{
			get => m_jmanu["brandId"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["brandId"] = value;
		}

		/// <summary></summary>
		public string Manufacturer
		{
			get => m_jmanu["manufacturer"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["manufacturer"] = value;
		}

		/// <summary></summary>
		public string Model
		{
			get => m_jmanu["model"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["model"] = value;
		}

		/// <summary></summary>
		public string ModelInfo
		{
			get => m_jmanu["modelInfo"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["modelInfo"] = value;
		}

		/// <summary></summary>
		public string MAC
		{
			get => m_jmanu["mac"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["mac"] = value;
		}

		/// <summary></summary>
		public string ApMAC
		{
			get => m_jmanu["apmac"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["apmac"] = value;
		}

		/// <summary></summary>
		public string StaMAC
		{
			get => m_jmanu["staMac"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["staMac"] = value;
		}

		/// <summary></summary>
		public string UI
		{
			get => m_jmanu["ui"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["ui"] = value;
		}

		/// <summary></summary>
		public string ChipID
		{
			get => m_jmanu["chipid"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["chipid"] = value;
		}
	}
}
