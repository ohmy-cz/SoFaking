using net.jancerveny.sofaking.Common.Constants;
using net.jancerveny.sofaking.Common.Models;
using System.Collections.Generic;
using System.Linq;
using net.jancerveny.sofaking.Common.Utils;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public static class TorrentRating
    {
        public static TorrentSearchResult GetBestTorrent(List<TorrentSearchResult> torrents, string[] threeLetterLanguageCodes)
        {
            if(torrents == null || torrents.Count() == 0)
            {
                return null;
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
                .OrderByDescending(x => CalculateTorrentScore(x, threeLetterLanguageCodes));

            return calculatedScores
                .FirstOrDefault();
        }

        public static int CalculateTorrentScore(TorrentSearchResult torrent, string[] threeLetterLanguageCodes)
        {
            var score = 0;
            var name = torrent.Name.ToLowerInvariant();
            // TODO: Convert to dictionary?
            var nameChunks = new List<string>();
            nameChunks.AddRange(name.Split(" - "));
            nameChunks.AddRange(name.Split("-"));
            nameChunks.AddRange(name.Split("."));
            nameChunks.AddRange(name.Split(" "));
            nameChunks = nameChunks.Distinct().ToList();

            var fulltextTags = TorrentScoring.Tags.Where(x => x.Key.Contains(" - ") || x.Key.Contains("-") || x.Key.Contains(".") || x.Key.Contains(" "));
            var exactTags = TorrentScoring.Tags.Except(fulltextTags);

            // Rate movies with language tags higher
            foreach(var lang in threeLetterLanguageCodes)
            {
                if(name.IndexOf(lang) >= 0)
                {
                    score += 1000;
                }

                // Probably less precise, hence the lower score
                if (name.IndexOf(CountryCodes.ConvertThreeLetterNameToTwoLetterName(lang)) >= 0)
                {
                    score += 500;
                }
            }

            // Look for fulltext tag  mmatch, for instance where the tag itself has any of the otherwise splitted characters and the matching would lose meaning
            foreach (var tag in fulltextTags.Where(x => x.Value == TorrentScoring.BannedTag))
            {
                if (name.IndexOf(tag.Key.ToLowerInvariant()) >= 0)
                {
                    return TorrentScoring.BannedTag;
                }
            }
            foreach (var tag in fulltextTags)
            {
                if (name.IndexOf(tag.Key.ToLowerInvariant()) >= 0)
                {
                    score += tag.Value;
                }
            }

            // split  the long torrent name by the dots and  look for exact  tag matches
            foreach (var tag in exactTags.Where(x => x.Value == TorrentScoring.BannedTag))
            {
                if (nameChunks.Contains(tag.Key.ToLowerInvariant()))
                {
                    return TorrentScoring.BannedTag;
                }
            }
            foreach (var tag in exactTags)
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
