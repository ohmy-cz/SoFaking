using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.BusinessLogic.Models
{
	public class IMDBStructuredData
	{
		public string Type { get; set; }
		public string Url { get; set; }
		public string Name { get; set; }
		public string Image { get; set; }
		public string[] Genre { get; set; }
		public string ContentRating { get; set; }
		public Person[] Actor { get; set; }
		public Person Director { get; set; }
		public Person[] Creator { get; set; }
		public string Description { get; set; }
		public DateTime? DatePublished { get; set; }
		public string Keywords { get; set; }
		public AggregateRating AggregateRating { get; set; }
	}

	public class Person
	{
		public Uri Url { get; set; }
		public string Name { get; set; }
	}

	public class AggregateRating
	{
		public int? RatingCount { get; set; }
	    public string BestRating { get; set; }
		public string WorstRating { get; set; }
		public string RatingValue { get; set; }
	}
}
