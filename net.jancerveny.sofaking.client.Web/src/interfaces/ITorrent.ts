import { MovieStatusEnum } from "../enums/MovieStatusEnum";

export default interface ITorrent {
  id: number;
  hashString: string;
  status: MovieStatusEnum;
  isFinished: boolean;
  eta: number;
  percentDone: number;
  sizeWhenDone: number;
  leftUntilDone: number;
  downloadDir: string;
  name: string;
}
