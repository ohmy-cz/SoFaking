using System;
using System.Collections.Generic;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
    public interface IVerifiedMovie
    {
        string Id { get; set; }
        string Title { get; set; }
        //IDictionary<CountryEnum, string> TitleInt { get; set; }
        int? ReleaseYear { get; set; }
        /// <summary>
        /// X out of 10
        /// </summary>
        double Score { get; set; }
        /// <summary>
        /// Percents
        /// </summary>
        int ScoreMetacritic { get; set; }
        //string Director { get; set; }
        //string[] Writers { get; set; }
        //string[] Actors { get; set; }
        //TimeSpan Length { get; set; }
        //string[] Genres { get; set; }
        //CountryEnum[] CountriesOfOrigin { get; set; }
    }
}
