using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.Common.Utils
{
	public class MaybeArrayJsonConverter<T> : JsonConverter<T[]>
	{
        public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.String)
            {
                return new string[] { reader.GetString() } as T[];
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return new T[] { JsonSerializer.Deserialize<T>(ref reader, options) };
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            List<T> list = new List<T>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list.ToArray();
                }

                //// Get the key.
                //if (reader.TokenType != JsonTokenType.PropertyName)
                //{
                //    throw new JsonException();
                //}

                //string propertyName = reader.GetString();

                //// For performance, parse with ignoreCase:false first.
                //if (!Enum.TryParse(propertyName, ignoreCase: false, out TKey key) &&
                //    !Enum.TryParse(propertyName, ignoreCase: true, out key))
                //{
                //    throw new JsonException(
                //        $"Unable to convert \"{propertyName}\" to Enum \"{_keyType}\".");
                //}

                // Get the value.
                T v = JsonSerializer.Deserialize<T>(ref reader, options);

                // Add to dictionary.
                list.Add(v);
            }

            throw new JsonException();
            //var key = reader.TokenType;
            //if (key != JsonTokenType.String)
            //	return null;
            //var value = reader.GetString();
            //if(typeof(T) == typeof(string))
            //{
            //	return new string[] { value } as T[];
            //}
            //try
            //{
            //	return JsonSerializer.Deserialize<T[]>(value);
            //}
            //catch (Exception _) { }

            //try
            //{
            //	return new T[] { JsonSerializer.Deserialize<T>(value) };
            //}
            //catch (Exception _) { }

            //return new T[] { };
        }

		public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
	}
}
