using System;
using System.Device.Location;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Resolver
{
	/// <summary>
	/// Tasks that can be performed by the client
	/// </summary>
	class Resolver
	{
		private const string _availPathTestDb = "http://{0}/api/v2/isavailable?caller=resolver.client&details=1&testdb=1";
		private const string _payoffPathV2 = "http://{0}/api/v2/payoff/{1}/{2}";
		private const string _clientDataFmtV2 = "?clientdata={0}";
		const string _rfc822DateFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";
		const string _contentType = "application/json; charset=utf-8";

		// public properties
		public static string UserName { get; set; }
		public static string SecurityKey { get; set; }
		public static Stopwatch Elapsed { get; set; }
		public static HttpStatusCode StatusCode { get; set; }

		static Resolver()
		{
			Elapsed = new Stopwatch();
			StatusCode = HttpStatusCode.OK;
		}

		/// <summary>
		/// Calls the Payoff service method
		/// </summary>
		public static async Task<string> Payoff(ResolverData data)
		{
			var handler = new HttpClientHandler();
			handler.PreAuthenticate = handler.SupportsPreAuthenticate();

			using (var client = new HttpClient(handler))
			{
				try
				{
					// formulate full url
					string url = string.Format(_payoffPathV2, data.Host, data.Payload, data.Type);
					AppendClientData(ref url, data);

					var now = DateTime.UtcNow;
					client.DefaultRequestHeaders.Date = now;
					string body = SerializeObject(CreateMetricV2()).ToString();
					int lenBody = Encoding.UTF8.GetByteCount(body);
					var content = new StringContent(body);
					content.Headers.ContentType = MediaTypeHeaderValue.Parse(_contentType);

					// set user credentials in Authorization header
					string authInfo = CreateAuthorizationHeader(url, "POST", lenBody, _contentType, now);
					client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authInfo);

					// make request
					HttpResponseMessage response;
					try
					{
						Elapsed.Restart();
						response = await client.PostAsync(url, content);
					}
					finally { Elapsed.Stop(); }

					StatusCode = response.StatusCode;
					if (!response.IsSuccessStatusCode) return "StatusCode: " + StatusCode;

					var result = response.Content.ReadAsStringAsync().Result;
					return JsonIO.FormatFragment(result);
				}
				catch (Exception e)
				{
					var error = string.Format("Exception: " + e.Message);
					if (e.InnerException != null)
						error += string.Format("\nInner Exception: " + e.InnerException.Message);

					return error;
				}
			}
		}

		/// <summary>
		/// Calls the IsAvailable service method
		/// </summary>
		public static async Task<string> IsAvailable(ResolverData data)
		{
			using (var client = new HttpClient())
			{
				try
				{
					// formulate full url
					string url = string.Format(_availPathTestDb, data.Host);
					AppendClientData(ref url, data);

					// make request
					HttpResponseMessage response;
					try
					{
						Elapsed.Restart();
						response = await client.GetAsync(url);
					}
					finally { Elapsed.Stop(); }

					StatusCode = response.StatusCode;
					if (!response.IsSuccessStatusCode) return "StatusCode: " + StatusCode;

					var result = response.Content.ReadAsStringAsync().Result;
					return JsonIO.FormatFragment(result);
				}
				catch (Exception e)
				{
					var error = string.Format("Exception: " + e.Message);
					if (e.InnerException != null)
						error += string.Format("\nInner Exception: " + e.InnerException.Message);

					return error;
				}
				finally
				{
					Elapsed.Stop();
				}
			}
		}

		#region Helpers
		/// <summary>
		/// Formats a date in RFC 822 format
		/// </summary>
		private static string DateAsRfc822(DateTime date)
		{
			DateTime utcDate = date;
			if (date.Kind != DateTimeKind.Utc)
				utcDate = date.ToUniversalTime();

			return utcDate.ToString(_rfc822DateFormat);
		}

		/// <summary>
		/// Generates the hmac in the expected format
		/// </summary>
		private static string CreateAuthorizationHeader(string url, string verb, int contLen, string contentType, DateTime dateHeader)
		{
			var details = new RequestDetails()
			{
				Uri = url,
				Verb = verb.ToUpper(),
				ContentType = contentType,
				ContentLength = contLen.ToString(),
				Date = DateAsRfc822(dateHeader)
			};

			string stringToSign = HmacSecurity.CreateStringToSign(details);
			byte[] securityKey = Encoding.UTF8.GetBytes(SecurityKey);
			string sig = HmacSecurity.GenerateSha512Hmac(securityKey, stringToSign);
			return HmacSecurity.FormatHmacAuthHeader(UserName, sig);
		}

		/// <summary>
		/// Appends the clientdata query param if specified
		/// </summary>
		private static void AppendClientData(ref string url, ResolverData data)
		{
			if (data.GetCell)
				url += string.Format(_clientDataFmtV2, "usecellname");
			else if (!string.IsNullOrWhiteSpace(data.ClientData))
				url += string.Format(_clientDataFmtV2, data.ClientData);
		}

		/// <summary>
		/// Parses the passed string for the a cell name in the description field
		/// </summary>
		public static string ParseCellName(string respBody)
		{
			const string unknown = "<unknown>";
			string token = "\"Description\" : \"";
			int ndxStart = respBody.IndexOf(token);
			if (ndxStart == -1)
			{
				token = "\"Cell\" : \"";
				if (!respBody.Contains(token)) return unknown;
				ndxStart = respBody.IndexOf(token);
				if (ndxStart == -1) return unknown;
			}

			ndxStart += token.Length;
			int ndxEnd = respBody.IndexOf("\"", ndxStart);
			return respBody.Substring(ndxStart, ndxEnd - ndxStart);
		}

		/// <summary>
		/// Serializes an object in either xml or json format
		/// </summary>
		private static StringBuilder SerializeObject(object obj)
		{
			var sb = new StringBuilder();
			JsonIO.Serialize(obj, sb);
			return sb;
		}

		/// <summary>
		/// Creates a sample device metric version 2
		/// </summary>
		static DeviceMetricV2 CreateMetricV2()
		{
			object deviceId;
			Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out deviceId);
			string deviceIdent = Convert.ToBase64String(deviceId as byte[]);

			var geo = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
			
			

			return new DeviceMetricV2()
			{
				AppName = "ResolverTest",
				AppVersion = "1.00",
				DeviceIdentifier = deviceIdent,
				Language = "en_US",
				Latitude = (geo.Position.Location.IsUnknown) ? "" : geo.Position.Location.Latitude.ToString(),
				Longitude = (geo.Position.Location.IsUnknown) ? "" : geo.Position.Location.Longitude.ToString(),
				Manufacturer = Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer,
				Model = Microsoft.Phone.Info.DeviceStatus.DeviceName,
				SystemName = "Microsoft Windows Phone 8",
				SystemVersion = Environment.OSVersion.Version.ToString()
			};
		}
		#endregion
	}

	/// <summary>
	/// Input data for Resolver calls
	/// </summary>
	class ResolverData
	{
		public string Host { get; set; }
		public string Method { get; set; }
		public string Cell { get; set; }
		public string Payload { get; set; }
		public string Type { get; set; }
		public string ClientData { get; set; }
		public bool GetCell { get; set; }
		public int RepeatDelay { get; set; }
	}
}
