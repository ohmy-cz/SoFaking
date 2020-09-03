export default interface ITransmissionTorrentRequest {
  arguments?: ITransmissionTorrentRequestArguments;
  method: "torrent-get" | "session-get" | "session-stats";
}

interface ITransmissionTorrentRequestArguments {
  fields?: string[];
  ids: "recently-active";
}
