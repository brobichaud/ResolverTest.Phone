using System.Runtime.Serialization;
using System.Text;

namespace Resolver
{
	/// <summary>
	/// Client device metrics
	/// </summary>
	[DataContract(Name = "DeviceMetricV2", Namespace = "")]
	public class DeviceMetricV2
	{
		/// <summary>
		/// Unique identifier of the mobile device
		/// </summary>
		[DataMember(Name = "DeviceIdentifier", Order = 1, IsRequired = true)]
		public string DeviceIdentifier { get; set; }

		/// <summary>
		/// The mobile operating system name
		/// </summary>
		[DataMember(Name = "SystemName", Order = 2, IsRequired = true)]
		public string SystemName { get; set; }

		/// <summary>
		/// The version of the mobile operating system
		/// </summary>
		[DataMember(Name = "SystemVersion", Order = 3, IsRequired = true)]
		public string SystemVersion { get; set; }

		/// <summary>
		/// The manufacturer of the mobile device
		/// </summary>
		[DataMember(Name = "Manufacturer", Order = 4, IsRequired = true)]
		public string Manufacturer { get; set; }

		/// <summary>
		/// The model of the mobile device
		/// </summary>
		[DataMember(Name = "Model", Order = 5, IsRequired = true)]
		public string Model { get; set; }

		/// <summary>
		/// The devices current location - latitude
		/// </summary>
		[DataMember(Name = "Latitude", Order = 6, IsRequired = true)]
		public string Latitude { get; set; }

		/// <summary>
		/// The devices current location - longitude
		/// </summary>
		[DataMember(Name = "Longitude", Order = 7, IsRequired = true)]
		public string Longitude { get; set; }

		/// <summary>
		/// Localization language of the mobile device
		/// </summary>
		[DataMember(Name = "Language", Order = 8, IsRequired = true)]
		public string Language { get; set; }

		/// <summary>
		/// Name of the mobile app
		/// </summary>
		[DataMember(Name = "AppName", Order = 9, IsRequired = true)]
		public string AppName { get; set; }

		/// <summary>
		/// Version of the mobile app
		/// </summary>
		[DataMember(Name = "AppVersion", Order = 10, IsRequired = true)]
		public string AppVersion { get; set; }

		/// <summary>
		/// Logs all the metric info
		/// </summary>
		internal virtual string FormatLog()
		{
			var metrics = new StringBuilder();
			metrics.AppendFormat("DeviceIdentifier: '{0}' ", DeviceIdentifier);
			metrics.AppendFormat("SystemName: '{0}' ", SystemName);
			metrics.AppendFormat("SystemVersion: '{0}' ", SystemVersion);
			metrics.AppendFormat("Manufacturer: '{0}' ", Manufacturer);
			metrics.AppendFormat("Model: '{0}' ", Model);
			metrics.AppendFormat("Longitude: '{0}' ", Longitude);
			metrics.AppendFormat("Latitude: '{0}' ", Latitude);
			metrics.AppendFormat("Language: '{0}' ", Language);
			metrics.AppendFormat("AppName: '{0}' ", AppName);
			metrics.AppendFormat("AppVersion: '{0}' ", AppVersion);

			return metrics.ToString();
		}
	}
}
