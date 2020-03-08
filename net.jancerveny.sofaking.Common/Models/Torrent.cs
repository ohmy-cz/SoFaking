using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.Common.Models
{
    public class Torrent
    {
        public string Name { get; set; }
        public string MagnetLink { get; set; }
        public double SizeGb { get; set; }
        public int Seeders { get; set; }
        public int Leeches { get; set; }
        public string DetailsLink { get; set; }
    }
}
