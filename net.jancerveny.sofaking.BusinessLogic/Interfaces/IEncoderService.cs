using net.jancerveny.sofaking.BusinessLogic.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
	public interface IEncoderService : IDisposable
	{
		Task StartTranscodingAsync(ITranscodingJob transcodingJob, CancellationToken cancellationToken);
		Task<IMediaInfo> GetMediaInfo(string filePath);
		string CurrentFile { get; }
		double PercentDone { get; }
		bool Busy { get; }
		void Kill();
		void CleanTempData();
		public event EventHandler<EventArgs> OnStart;
		/// <summary>
		/// Gets triggered when the transcoding got cancelled externally
		/// </summary>
		public event EventHandler<EventArgs> OnCancelled;
		/// <summary>
		/// Evertime there's an update
		/// </summary>
		public event EventHandler<EncodingProgressEventArgs> OnProgress;
		public event EventHandler<EncodingSuccessEventArgs> OnSuccess;
		public event EventHandler<EncodingErrorEventArgs> OnError;
		public void DisposeAndKeepFiles();
	}
}
