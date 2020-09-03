import React, { useState } from "react";
import "../node_modules/typeface-montserrat";
import styles from "./App.module.scss";
import TextField from "@material-ui/core/TextField";
import Autocomplete from "@material-ui/lab/Autocomplete";
// *https://www.registers.service.gov.uk/registers/country/use-the-api*
import CircularProgress from "@material-ui/core/CircularProgress";
import { Container, Grid, Button } from "@material-ui/core";
import ISofaKingEntity from "./interfaces/ISofakingEntity";
import ICountryType from "./interfaces/ICountryType";
import logo from "./logo.png";
import MovieCard from "./components/MovieCard";
import Loading from "./components/Loading";
import ITransmissionTorrentRequest from "./interfaces/ITransmissionTorrentRequest";
import ITransmissionTorrentResponse from "./interfaces/ITransmissionTorrentResponse";

function sleep(delay = 0) {
  return new Promise((resolve) => {
    setTimeout(resolve, delay);
  });
}

export default function SoFakingClient() {
  const [open, setOpen] = useState(false);
  const [options, setOptions] = useState<ICountryType[]>([]);
  const [isLoaded, setIsLoaded] = useState(false);
  const [entities, setEntities] = useState<ISofaKingEntity[]>([]);
  const [torrents, setTorrents] = useState<ITransmissionTorrentResponse[]>([]);
  const [transmissionSessionId, setTransmissionSessionId] = useState<
    string | null
  >(null);
  const loading = open && options.length === 0;

  React.useEffect(() => {
    fetch("//movies-api.jancerveny.net/movie")
      .then((response) => {
        if (response.ok) {
          return response.json();
        } else {
          return Promise.reject(response.statusText);
        }
      })
      .then(
        (result) => {
          setIsLoaded(true);
          setEntities(result);
        },
        (error) => {
          setIsLoaded(true);
          setEntities([]);
          console.warn(error);
        }
      );

    // call the endpoint with method session-get,
    // read the X-Transmission-Session-Id header,
    // attach the X-Transmission-Session-Id to all following requests
    const authorizationRequest: ITransmissionTorrentRequest = {
      method: "session-get",
    };

    fetch("/transmission/rpc", {
      method: "POST",
      // mode: "same-origin", // no-cors, *cors, same-origin
      // cache: "no-cache", // *default, no-cache, reload, force-cache, only-if-cached
      // credentials: "omit", // include, *same-origin, omit
      headers: {
        Accept: "application/json",
      },
      body: JSON.stringify(authorizationRequest),
    }).then((response) => {
      console.log(response);
      if (response.headers) {
        response.headers.forEach((header) => {
          if (header === "X-Transmission-Session-Id") {
            setTransmissionSessionId(header);
            return;
          }
        });
      } else {
        console.error(response.statusText);
      }
    });
  }, []);

  // React.useEffect(() => {
  //   fetch("//localhost:999/transmission/rpc")
  //     .then((response) => {
  //       if (response.ok) {
  //         return response.json();
  //       } else {
  //         return Promise.reject(response.statusText);
  //       }
  //     })
  //     .then(
  //       (result) => {
  //         setTorrents(result);
  //       },
  //       (error) => {
  //         setTorrents([]);
  //         console.warn(error);
  //       }
  //     );
  // }, [transmissionSessionId]);

  React.useEffect(() => {
    console.log(torrents);
  }, [torrents]);

  React.useEffect(() => {
    let active = true;

    if (!loading) {
      return undefined;
    }

    (async () => {
      const response = await fetch(
        "https://country.register.gov.uk/records.json?page-size=5000"
      );
      await sleep(1e3); // For demo purposes.
      const countries = await response.json();

      if (active) {
        setOptions(
          Object.keys(countries).map(
            (key) => countries[key].item[0]
          ) as ICountryType[]
        );
      }
    })();

    return () => {
      active = false;
    };
  }, [loading]);

  React.useEffect(() => {
    if (!open) {
      setOptions([]);
    }
  }, [open]);

  return (
    <Container maxWidth="lg" className={styles.App}>
      <h1>
        <img src={logo} alt="SoFaking" height={30} /> SofaKing
      </h1>
      <Autocomplete
        id="asynchronous-demo"
        open={open}
        onOpen={() => {
          setOpen(true);
        }}
        onClose={() => {
          setOpen(false);
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
                  {loading ? (
                    <CircularProgress color="inherit" size={20} />
                  ) : null}
                  {params.InputProps.endAdornment}
                </React.Fragment>
              ),
            }}
          />
        )}
      />
      <div>Transmission session ID: {transmissionSessionId}</div>
      {!isLoaded && <Loading />}
      {entities.length > 0 && (
        <Grid container spacing={3}>
          {entities.map((movie) => (
            <Grid key={movie.Movie.ImdbId} item xs={6} sm={4} md={3} lg={2}>
              <MovieCard movie={movie.Movie} />
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}
