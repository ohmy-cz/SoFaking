using net.jancerveny.sofaking.Common.Constants;
using net.jancerveny.sofaking.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public static class TorrentRating
    {
        public static Torrent GetBestTorrent(List<Torrent> torrents)
        {
            if(torrents == null || torrents.Count() == 0)
            {
                return null;
            }

            if(torrents.Count() == 1)
            {
                return torrents.FirstOrDefault();
            }

            // First we get rid of the torrents that fall out of the basic requirement specs
            var filteredTorrents = torrents
                .Where(x =>
                    x.Seeders > 0 &&
                    x.SizeGb >= TorrentScoring.FileSizeGbMin &&
                    x.SizeGb <= TorrentScoring.FileSizeGbMax
                )
                .OrderByDescending(x => x.Seeders)
                .ThenByDescending(x => x.SizeGb);

            var calculatedScores = filteredTorrents
                .ToList()
                .OrderByDescending(x => CalculateTorrentScore(x));

            return calculatedScores
                .FirstOrDefault();
        }

        public static int CalculateTorrentScore(Torrent torrent)
        {
            var score = 0;
            var name = torrent.Name.ToLowerInvariant();
            var nameChunks = new List<string>();
            nameChunks.AddRange(name.Split(" - "));
            nameChunks.AddRange(name.Split("-"));
            nameChunks.AddRange(name.Split("."));
            nameChunks.AddRange(name.Split(" "));
            nameChunks = nameChunks.Distinct().ToList();

            foreach (var tag in TorrentScoring.Tags.Where(x => x.Value == TorrentScoring.BannedTag))
            {
                if (nameChunks.Contains(tag.Key.ToLowerInvariant()))
                {
                    return TorrentScoring.BannedTag;
                }
            }
            foreach (var tag in TorrentScoring.Tags)
            {
                if (nameChunks.Contains(tag.Key.ToLowerInvariant()))
                {
                    score += tag.Value;
                }
            }
            return score;
        }
    }
}
