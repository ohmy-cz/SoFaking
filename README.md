# SoFaking
Experimental project for torrent search and queuing automation.

## Motivation
SoFaking is a spiritual following to [CouchPotato](https://couchpota.to/), Radarr, Sonarr and the like. These projects have some issues, namely they were hard to setup, obsolete or discontinued.

### Goals
1. Have it run on my Raspberry PI 4 with external HDD attached (with DLNA sharing)
2. Plug and play - you can pull the HDD from your Raspberry Pi, share movies with your firends, and then re-connect and continue streaming movies on your home network.
3. Interoperability with media center software like KODI.

### The flow
1. Enter a movie or TV series you would like to download,
2. A list of IMDB results appears, including release year, IMDB rating and MetaCritic rating so you can choose the best match,
3. After choosing the concrete movie/series, crawl internal list of torrent sources, find the best file quality match (highest resolution and size), and torrent gets added to Transmission
4. When the download finishes, transcode the downloaded movie to your target specification, while keeping all of the original soundtracks as secondary tracks (use case: Play the movies on PS4 or other device without audio decoder, and then choose the originl audio when you play it on a device that supports it). Also, add a Cover image and clean metadata from IMDB, and put the outputs in a clean-named folder (by movie title from IMDB)

### Movie watchdog
When you add a movie found on IMDB which wasn't released yet, SoFaking will look for it. There will be a service running periodically, scrapping given list of sources for a matching minimum quality torrent (ie skipping a HDCAM release). When found at a point in the future, the movie will be automatically downloaded and you will get notified through an SMS.

### Made for you
Just like in Spotify, SoFaking will download a certain amount of "random" movies each month, so you have always something fresh to watch. You have to set a limit to the size of these movies in Gb. These movies will be temporary (unless you make them permanent in SoFaking's interface), downloaded based on your other movies in your library and only passing a certain IMDB score (fx. 9/10).

### Structure
1. Use .NET Core 3.1 and the best coding practices.
2. A simple program that connects the fewest possible external programs and dependencies
3. Make my program user friendly, both to install and use
4. Focus on clean and stable code

### Dependencies
- FFMPEG
- Transmission