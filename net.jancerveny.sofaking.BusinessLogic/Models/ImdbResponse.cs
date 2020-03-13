using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class ImdbResponse
	{
		[JsonPropertyName("d")]
		public List<ImdbResponseMovie> Matches { get; set; }
		[JsonPropertyName("q")]
		public string Query { get; set; }
		[JsonPropertyName("v")]
		public int Version { get; set; }
	}

	public class ImdbResponseMovie
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("l")]
		public string Title { get; set; }

		[JsonPropertyName("i")]
		public ImdbImage Image { get; set; }

		/// <summary>
		/// feature / TV series
		/// </summary>
		[JsonPropertyName("q")]
		public string Type { get; set; }

		[JsonPropertyName("s")]
		public string Actors { get; set; }

		[JsonPropertyName("y")]
		public int? Year { get; set; }

		[JsonPropertyName("yr")]
		public string Years { get; set; }

		/// <summary>
		/// Popularity
		/// </summary>
		[JsonPropertyName("rank")]
		public int Rank { get; set; }
	}

	public class ImdbImage
	{
		[JsonPropertyName("imageUrl")]
		public string ImageUrl { get; set; }
		[JsonPropertyName("width")]
		public int Width { get; set; }
		[JsonPropertyName("height")]
		public int Height { get; set; }
	}
}
