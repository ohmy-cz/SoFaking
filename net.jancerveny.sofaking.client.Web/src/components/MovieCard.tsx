import React from "react";
import Card from "@material-ui/core/Card";
import CardActions from "@material-ui/core/CardActions";
import CardContent from "@material-ui/core/CardContent";
import Button from "@material-ui/core/Button";
import Typography from "@material-ui/core/Typography";
import style from "./MovieCard.module.scss";
import IMovie from "../interfaces/IMovie";
import ITorrent from "../interfaces/ITorrent";
import { Grid, CircularProgress } from "@material-ui/core";
import Box from "@material-ui/core/Box";
import MovieStatus from "./MovieStatus";
import { MovieStatusEnum } from "../enums/MovieStatusEnum";

type MovieProps = {
  movie: IMovie;
  torrent: ITorrent | null;
};

type CircularProgressWithLabelProps = {
  value: number;
};

function CircularProgressWithLabel(props: CircularProgressWithLabelProps) {
  return (
    <Box position="relative" display="inline-flex">
      <CircularProgress variant="static" {...props} />
      <Box
        top={0}
        left={0}
        bottom={0}
        right={0}
        position="absolute"
        display="flex"
        alignItems="center"
        justifyContent="center"
      >
        <Typography
          variant="caption"
          component="div"
          color="textSecondary"
        >{`${Math.round(props.value)}%`}</Typography>
      </Box>
    </Box>
  );
}

export default function MovieCard(props: MovieProps): JSX.Element {
  return (
    <Card className={style.movieCard}>
      <CardContent>
        <div
          className={style.cover}
          style={{ backgroundImage: `url(${props.movie.ImageUrl})` }}
        >
          <div>
            <Typography variant="h5" component="h2">
              {props.movie.Title}
            </Typography>
          </div>
        </div>
      </CardContent>
      {props.torrent && (
        <CardContent>
          <Grid container spacing={1}>
            {props.torrent.status != MovieStatusEnum.Downloaded && (
              <Grid
                item
                className={props.torrent.percentDone > 0.9 ? style.success : ""}
              >
                <CircularProgressWithLabel
                  value={props.torrent.percentDone * 100}
                />
              </Grid>
            )}
            <Grid item>
              <Typography component="p" variant="caption">
                <MovieStatus status={props.torrent.status} />
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      )}
      <CardActions>
        <Button
          target="_blank"
          size="small"
          href={`https://www.imdb.com/title/${props.movie.ImdbId}`}
        >
          See on IMDb
        </Button>
      </CardActions>
    </Card>
  );
}
