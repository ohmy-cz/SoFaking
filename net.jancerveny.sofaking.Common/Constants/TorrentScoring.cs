﻿using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.Common.Constants
{
    public static class TorrentScoring
    {
        public static int BannedTag = -1000;
        public static Dictionary<string, int> Tags = new Dictionary<string, int>
        {
            // Positive tags will result in higher score
            { "1080p", 110 },
            { "Atmos", 100 },
            { "TrueHD", 90 },
            { "7.1", 90 },
            { "8CH", 90 },
            { "DTS-HD.MA 7.1", 85 },
            { "DTS-MA", 85 },
            { "DTS-HD", 85 },
            { "DTS5.1", 80 },
            { "DDP5.1", 75 },
            { "DD5.1", 70 },
            { "UNRATED", 70 },
            { "EXTENDED", 70 },
            { "DIRECTORS CUT", 70 },
            { "BluRay", 65 },
            { "BRRip", 65 },
            { "BDRip", 65 },
            { "x264", 60 },
            { "H264", 60 },
            { "mp4", 50 },
            { "mkv", 40 },
            { "AC3", 30 },
            { "ACC.5.1", 20 },
            { "ACC5.1", 20 },
            { "WEBRip", 10 },
            // Negative weight tags will lower the score
            { "DDP2.0", -10},
            { "2ch", -10},
            { "XVID", -20},
            { "AVI", -50},
            { "AAC", -90},
            { "AAC2.0", -100},
            { "720p", -150},
            { "Hindi", -999}, 
            { "Korean", -999},
            // Below 1000 are forbidden tags - these will be immediately removed from consideration
            { "TS", BannedTag },
            { "READINFO", BannedTag },
            { "x265", BannedTag },
            { "ESubs", BannedTag },
            { "E.Subs", BannedTag },
            { "DVDRip", BannedTag },
            { "DVD", BannedTag },
            { "CAM", BannedTag },
            { "HDTC", BannedTag },
            { "R6", BannedTag },
            { "R5", BannedTag },
            { "scr", BannedTag },
            { "480p", BannedTag },
            { "2160p", BannedTag }, // My screen doesn't support this format, skipping
            { "UHD", BannedTag }, // My screen doesn't support this format, skipping
            { "HDR", BannedTag }, // My screen doesn't support this format, skipping
            { "10 bit", BannedTag }, // My screen doesn't support this format, skipping
            { "10bit", BannedTag }, // My screen doesn't support this format, skipping
            { "Porn parody", BannedTag } // ¯\_(ツ)_/¯
        };
        public static int FileSizeGbMin = 4;
        public static int FileSizeGbMax = 30;
    }
}
