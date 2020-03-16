using System;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface ITorrentClientTorrent
	{
		/// <summary>
		/// Do not use this ID, it keeps on changing.
		/// </summary>
		int Id { get; set; }
		string Hash { get; set; }
		TorrentStatusEnum Status { get; set; }
		bool IsFinished { get; set; }
		int ETA { get; set; }
		double PercentDone { get; set; }
		long SizeWhenDoneKb { get; set; }
		long LeftUntilDoneKb { get; set; }
		string DownloadDir { get; set; }
		string Name { get; set; }
	}
}
