import React from "react";
import Card from "@material-ui/core/Card";
import CardActions from "@material-ui/core/CardActions";
import CardContent from "@material-ui/core/CardContent";
import Button from "@material-ui/core/Button";
import Typography from "@material-ui/core/Typography";
import style from "./MovieCard.module.scss";
import ISofaKingMovieProperties from "../interfaces/ISofaKingMovieProperties";

type MovieProps = {
  movie: ISofaKingMovieProperties;
};

export default function MovieCard(props: MovieProps): JSX.Element {
  console.log(props.movie);
  return (
    <Card className={style.movieCard}>
      <CardContent>
        <div
          className={style.cover}
          style={{ backgroundImage: `url(${props.movie.ImageUrl})` }}
        >
          <div>{props.movie.Title}</div>
        </div>
      </CardContent>
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
