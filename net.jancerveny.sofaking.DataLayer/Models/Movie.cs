using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace net.jancerveny.sofaking.DataLayer.Models
{
    public class Movie
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Added { get; set; }
        public DateTime? Deleted { get; set; }
        public string TorrentName { get; set; }
        public string TorrentHash { get; set; }
        public int TorrentClientTorrentId { get; set; }
        public double SizeGb { get; set; }
        public MovieStatusEnum Status { get; set; }
        public string ImdbId { get; set; }
        public int MetacriticScore { get; set; }
        public double ImdbScore { get; set; }
        public GenreFlags Genres { get; set; }
        public string ImageUrl { get; set; }
        public Movie()
        {
            Added = DateTime.Now;
            Status = MovieStatusEnum.DownloadQueued;
        }
    }
}
