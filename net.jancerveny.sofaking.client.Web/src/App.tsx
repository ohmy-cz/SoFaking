import React, { useState } from 'react'
import '../node_modules/typeface-montserrat'
import styles from './App.module.scss'
import TextField from '@material-ui/core/TextField'
import Autocomplete from '@material-ui/lab/Autocomplete'
// *https://www.registers.service.gov.uk/registers/country/use-the-api*
import CircularProgress from '@material-ui/core/CircularProgress'
import { Container, Grid, Button } from '@material-ui/core'
import logo from './logo.png'

interface CountryType {
  name: string;
}

function sleep (delay = 0) {
  return new Promise((resolve) => {
    setTimeout(resolve, delay)
  })
}

export enum MovieStatusEnum {
  WatchingFor = 0,
  DownloadQueued = 1,
  Downloading = 2,
  DownloadingPaused = 3,
  Downloaded = 4,
  NoVideoFilesError = 5,
  TranscodingQueued = 6,
  AnalysisStarted = 7,
  TranscodingStarted = 8,
  TranscodingFinished = 9,
  TranscodingError = 10,
  TranscodingCancelled = 20,
  Finished = 11,
  FileInUse = 12,
  FileNotFound = 13, // The download directory or downloaded file missing
  CouldNotDeleteDownloadDirectory = 14,
  TranscodingIncomplete = 15, // After restarting the client
  TranscodingRunningTooLong = 16, // Transcoding took more than 24 hours
  AnalysisAudioFailed = 17, // FFMPEG returned an empty string or threw an error
  AnalysisVideoFailed = 18, // FFMPEG returned an empty string or threw an error
  TorrentNotFound = 19 // Torrent has been removed from Transmission
}

export enum GenreFlags {
	Other = 0,
	Action = 1 << 1,
	Adventure = 1 << 2,
	Animation = 1 << 3,
	Biography = 1 << 4,
	Comedy = 1 << 5,
	Crime = 1 << 6,
	Drama = 1 << 7,
	Fantasy = 1 << 8,
	Family = 1 << 9,
	Historical = 1 << 10,
	Horror = 1 << 11,
	Mystery = 1 << 12,
	Romance = 1 << 13,
	SciFi = 1 << 14,
	Thriller = 1 << 15,
	War = 1 << 16,
	Western = 1 << 17
}

interface Movie {
  Id: number,
  Title: string,
  Added: Date,
  Deleted: Date | null,
  TorrentName: string,
  TorrentHash: string,
  TorrentClientTorrentId: number,
  SizeGb: number,
  Status: MovieStatusEnum,
  ImdbId: string,
  MetacriticScore: number,
  ImdbScore: number,
  Genres: GenreFlags,
  ImageUrl: string,
  TranscodingStarted: Date | null,
  Year: number | null,
  Director: string,
  Creators: string,
  Actors: string,
  Description: string,
  Show: string,
  EpisodeId: string
}

export default function SoFakingClient () {
  const [open, setOpen] = useState(false)
  const [options, setOptions] = useState<CountryType[]>([])
  const [isLoaded, setIsLoaded] = useState(false)
  const [movies, setMovies] = useState<Movie[]>([])
  const loading = open && options.length === 0
  // const movies: Movie[] = [{
  //   Actors: 'Russel Crowe',
  //   Added: new Date(),
  //   Creators: 'Cubrick',
  //   Deleted: null,
  //   Description: 'test',
  //   Director: 'Cubrick',
  //   EpisodeId: '',
  //   Genres: GenreFlags.Adventure,
  //   Id: -1,
  //   ImageUrl: 'https://m.media-amazon.com/images/M/MV5BMTQ4NDI3NDg4M15BMl5BanBnXkFtZTcwMjY5OTI1OA@@._V1_.jpg',
  //   ImdbId: 'tt1707386',
  //   ImdbScore: 9.0,
  //   MetacriticScore: 66,
  //   Show: '',
  //   SizeGb: 10,
  //   Status: MovieStatusEnum.Downloaded,
  //   Title: 'Les Miserables',
  //   TorrentClientTorrentId: -1,
  //   TorrentHash: '',
  //   TorrentName: '',
  //   TranscodingStarted: null,
  //   Year: 2020
  // },
  // {
  //   Actors: 'Russel Crowe',
  //   Added: new Date(),
  //   Creators: 'Cubrick',
  //   Deleted: null,
  //   Description: 'test',
  //   Director: 'Cubrick',
  //   EpisodeId: '',
  //   Genres: GenreFlags.Adventure,
  //   Id: -1,
  //   ImageUrl: 'https://m.media-amazon.com/images/M/MV5BMTY3MjM1Mzc4N15BMl5BanBnXkFtZTgwODM0NzAxMDE@._V1_.jpg',
  //   ImdbId: 'tt0066921',
  //   ImdbScore: 5.0,
  //   MetacriticScore: 66,
  //   Show: '',
  //   SizeGb: 10,
  //   Status: MovieStatusEnum.Downloaded,
  //   Title: 'A Clockwork Orange',
  //   TorrentClientTorrentId: -1,
  //   TorrentHash: '',
  //   TorrentName: '',
  //   TranscodingStarted: null,
  //   Year: 2020
  // }]

  React.useEffect(() => {
    console.log('hey1')
    fetch('//movies-api.jancerveny.net/movie')
      .then(res => res.json())
      .then(
        (result) => {
          console.log('hey');
          setIsLoaded(true)
          setMovies(result)
        },
        // Note: it's important to handle errors here
        // instead of a catch() block so that we don't swallow
        // exceptions from actual bugs in components.
        (error) => {
          setIsLoaded(true)
          console.error(error)
          // setError(error);
        }
      )
  }, [])

  React.useEffect(() => {
    let active = true

    if (!loading) {
      return undefined
    }

    (async () => {
      const response = await fetch('https://country.register.gov.uk/records.json?page-size=5000')
      await sleep(1e3) // For demo purposes.
      const countries = await response.json()

      if (active) {
        setOptions(Object.keys(countries).map((key) => countries[key].item[0]) as CountryType[])
      }
    })()

    return () => {
      active = false
    }
  }, [loading])

  React.useEffect(() => {
    if (!open) {
      setOptions([])
    }
  }, [open])

  return (
    <Container maxWidth="lg" className={styles.App}>
      <h1>
        <img src={logo} alt="SoFaking" height={30}/> SofaKing
      </h1>
      <Autocomplete
        id="asynchronous-demo"
        open={open}
        onOpen={() => {
          setOpen(true)
        }}
        onClose={() => {
          setOpen(false)
        }}
        getOptionSelected={(option, value) => option.name === value.name}
        getOptionLabel={(option) => option.name}
        options={options}
        loading={loading}
        renderInput={(params) => (
          <TextField
            {...params}
            label="Search for a movie"
            variant="outlined"
            InputProps={{
              ...params.InputProps,
              endAdornment: (
                <React.Fragment>
                  {loading ? <CircularProgress color="inherit" size={20} /> : null}
                  {params.InputProps.endAdornment}
                </React.Fragment>
              )
            }}
          />
        )}
      />
      {!isLoaded && <p>Loading&hellip;</p>}
      {movies.length > 0 && (
        <ul>
          {movies.map((movie) => {
            return (<li key={movie.Id}>
              <Grid container spacing={3} justify="space-between">
                <Grid item xs={1}>
                  <img src={movie.ImageUrl} alt={movie.Title} className={styles.MovieThumb}/>
                </Grid>
                <Grid item xs>
                  <h4>{movie.Title}</h4>
                  <p>{movie.Description}</p>
                </Grid>
                <Grid item xs>
                  <CircularProgress color="primary" thickness={10} size={30} variant="static" value={movie.ImdbScore * 10} />
                  <CircularProgress color="secondary" thickness={10} size={30} variant="static" value={movie.MetacriticScore} />
                </Grid>
                <Grid item xs>
                  <p>{MovieStatusEnum[movie.Status]}</p>
                </Grid>
                <Grid item xs className={styles.TextRight}>
                  <Button color="primary" size="small" variant="outlined" onClick={() => alert('download!')}>Download</Button>
                </Grid>
              </Grid>
            </li>)
          })}
        </ul>
      )}
    </Container>
  )
}
