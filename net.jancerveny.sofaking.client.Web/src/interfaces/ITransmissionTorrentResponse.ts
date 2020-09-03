export default interface ITransmissionTorrentResponse {
  arguments: ITransmissionTorrentResponseArguments;
  result: "success" | "error";
}

interface ITransmissionTorrentResponseArguments {
  removed: number[];
  torrents: ITransmissionTorrent[];
}

interface ITransmissionTorrent {
  downloadDir: string;
  error: number;
  errorString: string;
  eta: number;
  id: number;
  isFinished: boolean;
  isStalled: boolean;
  leftUntilDone: Date;
  metadataPercentComplete: number;
  peersConnected: number;
  peersGettingFromUs: number;
  peersSendingToUs: number;
  percentDone: number;
  queuePosition: number;
  rateDownload: number;
  rateUpload: number;
  recheckProgress: number;
  seedRatioLimit: number;
  seedRatioMode: number;
  sizeWhenDone: number;
  status: number;
  trackers: [];
  uploadRatio: number;
  uploadedEver: number;
  webseedsSendingToUs: number;
}
