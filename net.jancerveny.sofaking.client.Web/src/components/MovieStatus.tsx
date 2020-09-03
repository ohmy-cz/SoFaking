import React from "react";
import { MovieStatusEnum } from "./../enums/MovieStatusEnum";
import style from "./MovieStatus.module.scss";

type MovieStatusProps = {
  status: MovieStatusEnum;
};

export default function MovieStatus(props: MovieStatusProps): JSX.Element {
  switch (props.status) {
    case MovieStatusEnum.AnalysisAudioFailed:
      return <span className={style.error}>Audio analysis failed</span>;
    case MovieStatusEnum.AnalysisStarted:
      return <span>Analysis started</span>;
    case MovieStatusEnum.CouldNotDeleteDownloadDirectory:
      return (
        <span className={style.error}>Could not delete download directory</span>
      );
    case MovieStatusEnum.DownloadQueued:
      return <span>Download queued</span>;
    case MovieStatusEnum.Downloaded:
      return <span className={style.success}>Downloaded</span>;
    case MovieStatusEnum.Downloading:
      return <span>Downloading</span>;
    case MovieStatusEnum.DownloadingPaused:
      return <span>Downloading paused</span>;
    case MovieStatusEnum.FileInUse:
      return <span className={style.error}>File in use</span>;
    case MovieStatusEnum.FileNotFound:
      return <span className={style.error}>File not found</span>;
    case MovieStatusEnum.Finished:
      return <span className={style.error}>Finished</span>;
    case MovieStatusEnum.NoVideoFilesError:
      return <span className={style.error}>No video files</span>;
    case MovieStatusEnum.TorrentNotFound:
      return <span className={style.error}>Torrent not found</span>;
    case MovieStatusEnum.TranscodingCancelled:
      return <span className={style.warning}>Transcoding Cancelled</span>;
    case MovieStatusEnum.TranscodingError:
      return <span className={style.error}>Transcoding Error</span>;
    case MovieStatusEnum.TranscodingFinished:
      return <span className={style.error}>Transcoding Finished</span>;
    case MovieStatusEnum.TranscodingIncomplete:
      return <span className={style.error}>Transcoding Incomplete</span>;
    case MovieStatusEnum.TranscodingQueued:
      return <span>Transcoding Queued</span>;
    case MovieStatusEnum.TranscodingRunningTooLong:
      return (
        <span className={style.warning}>Transcoding running for too long</span>
      );
    case MovieStatusEnum.TranscodingStarted:
      return <span>Transcoding started</span>;
    case MovieStatusEnum.WatchingFor:
      return <span>Watching for movie</span>;
    default:
      return <span>Unknown status</span>;
  }
}
