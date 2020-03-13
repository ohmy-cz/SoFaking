using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class MovieSearchResult : IVerifiedMovie
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public int? ReleaseYear { get; set; }
		public double Score { get; set; }
		public int ScoreMetacritic { get; set; }
		public GenreFlags Genres { get; set; }
		public string ImageUrl { get; set; }
	}
}
