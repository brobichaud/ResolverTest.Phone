using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Resolver
{
	/// <summary>
	/// Json serialization related utility class
	/// </summary>
	public static class JsonIO
	{
		#region Formatting support
		private const string _space = " ";
		private const int _defaultIndent = 0;
		private const string _indent = "\t";
		private static readonly string _newLine = Environment.NewLine;
		private static bool _inDoubleString;
		private static bool _inSingleString;
		private static bool _inVariableAssignment;
		private static char _prevChar = '\0';
		private static readonly Stack<JsonContextType> _context = new Stack<JsonContextType>();

		private static void BuildIndents(int indents, StringBuilder output)
		{
			indents += _defaultIndent;
			for (; indents > 0; indents--)
				output.Append(_indent);
		}

		private static bool InString()
		{
			return _inDoubleString || _inSingleString;
		}

		private enum JsonContextType
		{
			Object,
			Array
		}
		#endregion

		/// <summary>
		/// Saves the passed object to a stream
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="stream">Stream to save to</param>
		public static void Serialize(object data, Stream stream)
		{
			var ser = new DataContractJsonSerializer(data.GetType());
			ser.WriteObject(stream, data);
		}

		/// <summary>
		/// Saves the passed object to a StringBuilder buffer
		/// </summary>
		/// <param name="data">Object to save</param>
		/// <param name="buffer">Stringbuilder to save into</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public static void Serialize(object data, StringBuilder buffer)
		{
			var ser = new DataContractJsonSerializer(data.GetType());

			using (var stream = new MemoryStream())
			{
				ser.WriteObject(stream, data);
				stream.Position = 0;
				using (var rdr = new StreamReader(stream))
				{
					buffer.Append(rdr.ReadToEnd());
				}
			}
		}

		/// <summary>
		/// Loads the passed file into an object of the passed type
		/// </summary>
		/// <param name="dataType">Type of object to load</param>
		/// <param name="file">File to load from</param>
		public static object Deserialize(Type dataType, string file)
		{
			var ser = new DataContractJsonSerializer(dataType);
			
			using (FileStream stream = File.OpenRead(file))
			{
				return ser.ReadObject(stream);
			}
		}

		/// <summary>
		/// Loads the passed stream into an object of the passed type
		/// </summary>
		/// <param name="dataType">Type of object to load</param>
		/// <param name="stream">Stream to load from</param>
		public static object Deserialize(Type dataType, Stream stream)
		{
			var ser = new DataContractJsonSerializer(dataType);

			stream.Position = 0;  // assume start at beginning of stream
			return ser.ReadObject(stream);
		}

		/// <summary>
		/// Loads the passed buffer into an object of the passed type
		/// </summary>
		/// <param name="dataType">Type of object to load</param>
		/// <param name="buffer">Buffer to load from</param>
		public static object DeserializeBuffer(Type dataType, string buffer)
		{
			var ser = new DataContractJsonSerializer(dataType);

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(buffer)))
			{
				return ser.ReadObject(stream);
			}
		}

		/// <summary>
		/// Formats a json fragment with indentation
		/// </summary>
		public static string FormatFragment(string input)
		{
			if (input == null)
				throw new ArgumentNullException("input");

			var output = new StringBuilder();
			int inputLength = input.Length;

			for (int i = 0; i < inputLength; i++)
			{
				char c = input[i];

				switch (c)
				{
					case '{':
						if (!InString())
						{
							if (_inVariableAssignment || (_context.Count > 0 && _context.Peek() != JsonContextType.Array))
							{
								output.Append(_newLine);
								BuildIndents(_context.Count, output);
							}

							output.Append(c);
							_context.Push(JsonContextType.Object);
							output.Append(_newLine);
							BuildIndents(_context.Count, output);
						}
						else
							output.Append(c);

						break;

					case '}':
						if (!InString())
						{
							output.Append(_newLine);
							_context.Pop();
							BuildIndents(_context.Count, output);
							output.Append(c);
						}
						else
							output.Append(c);

						break;

					case '[':
						output.Append(c);

						if (!InString())
							_context.Push(JsonContextType.Array);

						break;

					case ']':
						if (!InString())
						{
							output.Append(c);
							_context.Pop();
						}
						else
							output.Append(c);

						break;

					case '=':
						output.Append(c);
						break;

					case ',':
						output.Append(c);

						if (!InString() && _context.Peek() != JsonContextType.Array)
						{
							BuildIndents(_context.Count, output);
							output.Append(_newLine);
							BuildIndents(_context.Count, output);
							_inVariableAssignment = false;
						}

						break;

					case '\'':
						if (!_inDoubleString && _prevChar != '\\')
							_inSingleString = !_inSingleString;

						output.Append(c);
						break;

					case ':':
						if (!InString())
						{
							_inVariableAssignment = true;
							output.Append(_space);
							output.Append(c);
							output.Append(_space);
						}
						else
							output.Append(c);

						break;

					case '"':
						if (!_inSingleString && _prevChar != '\\')
							_inDoubleString = !_inDoubleString;

						output.Append(c);
						break;

					default:
						output.Append(c);
						break;
				}
				_prevChar = c;
			}

			return output.ToString();
		}
	}
}
