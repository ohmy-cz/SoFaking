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
                if(db.Movies.Where(x => x.ImdbId == movie.ImdbId).Any())
                {
                    return false;
                }
                db.Movies.Add(movie);
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
                var movie = db.Movies.Where(x => x.Id == id).FirstOrDefault();
                if (movie == null)
                {
                    throw new Exception("Movie does not exist");
                }
                db.Movies.Remove(movie);
                await db.SaveChangesAsync();
            }
        }
    }
}
