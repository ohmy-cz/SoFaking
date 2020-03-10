using System;

namespace net.jancerveny.sofaking.DataLayer.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Added { get; set; }
        public DateTime? Deleted { get; set; }
        public string TorrentName { get; set; }
        public string TorrentHash { get; set; }
        public int TorrentClientTorrentId { get; set; }
        public double SizeGb { get; set; }
        public MovieStatusEnum Status { get; set; }
        public string ImdbUrl { get; set; }
        public int MetacriticScore { get; set; }
        public double ImdbScore { get; set; }
    }
}
