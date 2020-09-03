import { MovieStatusEnum } from "../enums/MovieStatusEnum";
import { GenreFlags } from "../enums/GenreFlags";

export default interface IMovie {
  Id: number;
  Title: string;
  Added: Date;
  Deleted: Date | null;
  TorrentName: string;
  TorrentHash: string;
  TorrentClientTorrentId: number;
  SizeGb: number;
  Status: MovieStatusEnum;
  ImdbId: string;
  MetacriticScore: number;
  ImdbScore: number;
  Genres: GenreFlags;
  ImageUrl: string;
  TranscodingStarted: Date | null;
  Year: number | null;
  Director: string;
  Creators: string;
  Actors: string;
  Description: string;
  Show: string;
  EpisodeId: string;
}
