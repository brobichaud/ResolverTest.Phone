using System;
using HashLib;
using System.Text;

namespace Resolver
{
	/// <summary>
	/// Manages hmac security and authentication for the Resolver service.
	/// A Hash Message Authentication Code (HMAC) is used to digitally sign each request to the resolver
	/// using the SHA512 hash function. This results in a 512bit hash value (signature).
	/// 
	/// Each SDK user is assigned a unique user name and security key (which should be closely guarded).
	/// The security key is used by the SDK user to compute a hash (signature) of the requested uri using SHA512.
	/// The users name and the signature are passed to the resolver using the Http Authorization
	/// header in the format {username}:{signature}.
	///
	/// Here we validate the passed signature using the users registered security key (from our database)
	/// following the same HMAC algortihm used by the caller. The two signature must match.
	/// 
	/// The users name is not case sensitive, but the security key and signature are.
	/// 
	/// The signed string follows this format:
	///	Http-verb + \n + Date-header-value (or 'x-dmrc-date' header value) + \n + URL-Resource
	///		1. Http-verb is always upper case
	///		2. Caller has the option to include either the standard http Date header or a custom http header named 'x-dmrc-date'
	///			If a custom date is included in the request then it MUST be used to sign the string, otherwise Date is used.
	///		3. Date and custom date are always in RFC 822 format (in GMT with 4 digit year).
	///		4. URL-Resource is the path and query portion of the full URL (iow: excludes the scheme and host)
	///			and always includes the initial forward slash.
	///		5. Some examples of the string to sign:
	///				POST\nSun, 20 Sep 2009 20:36:40 GMT\napplication/json\n237\n/api/v1.1/payoffs/?payload=122&type=5
	///				POST\nSun, 20 Sep 2009 20:36:40 GMT\napplication/xml\n0\n/api/v1.1/action/token=3287.6832
	/// </summary>
	public class HmacSecurity
	{
		private const string _sigFormatGet = "{0}\n{1}\n{2}";
		private const string _sigFormatPost = "{0}\n{1}\n{2}\n{3}\n{4}";
		private const string _authDelimiter = ":";
		private const char _authDelimiterChar = ':';

		private RequestDetails Details { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public HmacSecurity(RequestDetails details)
		{
			Details = details;
		}

		/// <summary>
		/// Generates an hmac of the passed data using the passed key and a SHA512 hash
		/// </summary>
		public static string GenerateSha512Hmac(byte[] key, string data)
		{
			IHMAC hmac = HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA512());
			hmac.Key = key;
			byte[] dataBytes = Encoding.UTF8.GetBytes(data);
			var hash = hmac.ComputeBytes(dataBytes);

			return Convert.ToBase64String(hash.GetBytes());
		}

		/// <summary>
		/// Creates the string that will be signed based on request details
		/// </summary>
		/// <param name="det">Request details to use</param>
		public static string CreateStringToSign(RequestDetails det)
		{
			if (det.Verb == "POST")
				return string.Format(_sigFormatPost, det.Verb, det.Date, det.ContentType, det.ContentLength, det.GetUriPath());
			
			return string.Format(_sigFormatGet, det.Verb, det.Date, det.GetUriPath());
		}

		/// <summary>
		/// Creates the correctly formatted hmac authorization header from the passed creds
		/// </summary>
		/// <param name="username">Username to encode</param>
		/// <param name="signature">Signature to encode</param>
		public static string FormatHmacAuthHeader(string username, string signature)
		{
			if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(signature))
				throw new ArgumentException("Credentials required");

			return string.Format("{0}{1}{2}", username, _authDelimiter, signature);
		}

		/// <summary>
		/// Parses the user credentials from the http Authorization header when using hmac auth
		/// </summary>
		/// <param name="authHeader">Authorization header value</param>
		/// <param name="username">Extracted username</param>
		/// <param name="signature">Extracted signature</param>
		public static void ParseHmacAuthHeader(string authHeader, out string username, out string signature)
		{
			username = signature = string.Empty;

			if (!authHeader.Contains(_authDelimiter))
				throw new ArgumentException("Malformed credentials");

			string[] parts = authHeader.Split(_authDelimiterChar);
			username = parts[0];
			signature = parts[1];
		}
	}

	/// <summary>
	/// Details of the incoming request to authorize
	/// </summary>
	public class RequestDetails
	{
		public string Uri { get; set; }
		public string Verb { get; set; }
		public string Authorization { get; set; }
		public string ContentType { get; set; }
		public string ContentLength { get; set; }
		public string Date { get; set; }

		/// <summary>
		/// Logs all details for logging
		/// </summary>
		internal virtual string FormatLog()
		{
			var str = new StringBuilder();
			str.AppendFormat("Verb: {0}, ", Verb);
			str.AppendFormat("Type: {0}, ", ContentType);
			str.AppendFormat("Len: {0}, ", ContentLength);
			str.AppendFormat("Date: {0}, ", Date);
			str.AppendFormat("Uri: '{0}', ", GetUriPath());
			str.AppendFormat("Auth: {0} ", Authorization);

			return str.ToString();
		}

		/// <summary>
		/// Parses and returns the path and query portion of the uri, stripping scheme and host
		/// </summary>
		public string GetUriPath()
		{
			try
			{
				var uri = new Uri(Uri);
				return uri.PathAndQuery;
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
