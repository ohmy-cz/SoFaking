using Microsoft.EntityFrameworkCore;
using net.jancerveny.sofaking.DataLayer;
using net.jancerveny.sofaking.DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public class MovieService
    {
        private readonly SoFakingContextFactory _dbContextFactory;
        public MovieService(SoFakingContextFactory dbContextFactory)
        {
            if (dbContextFactory == null) throw new ArgumentNullException(nameof(dbContextFactory));
            _dbContextFactory = dbContextFactory;
        }

        public async Task<bool> AddMovie(Movie movie)
        {
            using (var db = _dbContextFactory.CreateDbContext())
            {
                if(!string.IsNullOrWhiteSpace(movie.ImdbId) && await db.Movies.Where(x => x.ImdbId == movie.ImdbId).AnyAsync())
                {
                    var m = await db.Movies.Where(x => x.ImdbId == movie.ImdbId).FirstOrDefaultAsync();
                    m.Status = movie.Status;
                    m.Actors = movie.Actors;
                    m.Added = movie.Added;
                    m.Creators = movie.Creators;
                    m.Deleted = null;
                    m.Description = movie.Description;
                    m.Director = movie.Director;
                    m.EpisodeId = movie.EpisodeId;
                    m.Genres = movie.Genres;
                    m.ImageUrl = movie.ImageUrl;
                    m.ImdbScore = movie.ImdbScore;
                    m.MetacriticScore = movie.MetacriticScore;
                    m.Show = movie.Show;
                    m.SizeGb = movie.SizeGb;
                    m.Title = movie.Title;
                    m.TorrentClientTorrentId = movie.TorrentClientTorrentId;
                    m.TorrentHash = movie.TorrentHash;
                    m.TorrentName = movie.TorrentName;
                    m.TranscodingStarted = null;
                    m.Year = movie.Year;
                } else
                {
                    db.Movies.Add(movie);
                }
                return await db.SaveChangesAsync() > 0;
            }
        }

        /// <summary>
        /// Remove a movie once it's been found and succesfully added to the Torrent client for download
        /// </summary>
        /// <param name="id">Movie id</param>
        public async Task RemoveMovie(int id)
        {
            using (var db = _dbContextFactory.CreateDbContext())
            {
                var movie = await db.Movies.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (movie == null)
                {
                    throw new Exception("Movie does not exist");
                }
                db.Movies.Remove(movie);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<Movie>> GetMoviesAsync()
        {
            using (var db = _dbContextFactory.CreateDbContext())
            {
                return await db.Movies.ToListAsync();
            }
        }

        public async Task SetMovieStatus(int id, MovieStatusEnum status)
        {
            using (var db = _dbContextFactory.CreateDbContext())
            {
                var m = await db.Movies.Where(x => x.Id == id).FirstOrDefaultAsync();
                m.Status = status;
                if(status == MovieStatusEnum.Finished)
                {
                    m.Deleted = DateTime.Now;
                }

                if(status == MovieStatusEnum.TranscodingStarted)
                {
                    m.TranscodingStarted = DateTime.Now;
                }

                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsAnimated(int id)
        {
            using (var db = _dbContextFactory.CreateDbContext())
            {
                return (await db.Movies.Where(x => x.Id == id).FirstOrDefaultAsync())?.Genres.HasFlag(GenreFlags.Animation) ?? false;
            }
        }
    }
}
