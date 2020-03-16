using System;
using System.Collections.Generic;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
    public interface IVerifiedMovie
    {
        string Id { get; set; }
        string Title { get; set; }
        int? ReleaseYear { get; set; }
        /// <summary>
        /// X out of 10
        /// </summary>
        double Score { get; set; }
        /// <summary>
        /// Percents
        /// </summary>
        int ScoreMetacritic { get; set; }
        string ImageUrl { get; set; }
    }
}
