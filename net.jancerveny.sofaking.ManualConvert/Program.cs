using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Models;
using net.jancerveny.sofaking.DataLayer;
using net.jancerveny.sofaking.ManualConvert.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.ManualConvert
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length == 0)
            {
                throw new Exception("No file to convert provided");
            }

            Console.WriteLine("Encoding: " + args[0]);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // TODO: Change the Production with Enviroment
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var encoderConfiguration = new EncoderConfiguration();
            configuration.GetSection("Encoder").Bind(encoderConfiguration);
            var sofakingConfiguration = new SoFakingConfiguration();
            configuration.GetSection("Sofaking").Bind(sofakingConfiguration);
            var dc = new DConf();
            configuration.GetSection("DownloadFinishedWorker").Bind(dc);

            var builder1 = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddSingleton(sofakingConfiguration)
                        .AddSingleton(encoderConfiguration)
                        .AddSingleton(new SoFakingContextFactory());
                }).UseConsoleLifetime();

            var host = builder1.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var serviceProvider = serviceScope.ServiceProvider;
                var logger = new NullLogger<FFMPEGEncoderService>();

                var encoderTranscodingInstance = new FFMPEGEncoderService(logger, encoderConfiguration, sofakingConfiguration);
                var videoFile = args[0];

                while (true)
                {
                    var flags = EncodingTargetFlags.None;
                    IMediaInfo mediaInfo = null;
                    try
                    {
                        mediaInfo = await encoderTranscodingInstance.GetMediaInfo(videoFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Can not read media info: " + ex.Message);
                        break;
                    }

                    try
                    {
                        if (!HasAcceptableVideo(dc, sofakingConfiguration, mediaInfo))
                        {
                            flags |= EncodingTargetFlags.NeedsNewVideo;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Incompatible video: " + ex.Message);
                        break;
                    }

                    try
                    {
                        if (!HasAcceptableAudio(dc, mediaInfo))
                        {
                            flags |= EncodingTargetFlags.NeedsNewAudio;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Incompatible audio: " + ex.Message);
                        break;
                    }

                    if (flags == EncodingTargetFlags.None)
                    {
                        Console.WriteLine($"Video file {videoFile} doesn't need transcoding, adding to files to move.");
                        break;
                    }

                    await encoderTranscodingInstance.StartTranscodingAsync(new TranscodingJob
                    {
                        SourceFile = videoFile,
                        Action = flags,
                        Duration = mediaInfo.Duration,
                    });
                }

                Console.ReadKey();
            }
        }

        private static bool HasAcceptableVideo(DConf dc, SoFakingConfiguration sc, IMediaInfo mediaInfo)
        {
            if (dc.AcceptedVideoCodecs == null) throw new ArgumentNullException(nameof(dc.AcceptedVideoCodecs));
            if (mediaInfo == null) throw new ArgumentNullException(nameof(mediaInfo));
            if (mediaInfo.VideoCodec == null) throw new ArgumentNullException(nameof(mediaInfo.VideoCodec));
            if (mediaInfo.FileInfo == null) throw new ArgumentNullException(nameof(mediaInfo.FileInfo));
            if (mediaInfo.HorizontalVideoResolution == -1) throw new ArgumentException($"Horizontal resolution invalid: {nameof(mediaInfo.HorizontalVideoResolution)}");

            var acceptableCodec = false;
            foreach (var vc in dc.AcceptedVideoCodecs)
            {
                if (mediaInfo.VideoCodec.IndexOf(vc) >= 0)
                {
                    acceptableCodec = true;
                    break;
                }
            }

            var acceptableResolution = mediaInfo.HorizontalVideoResolution <= sc.MaxHorizontalVideoResolution;
            var acceptableSize = mediaInfo.FileInfo.Length > (sc.MaxSizeGb * 1024 * 1024);
            // TODO: Fixing getting the video bitrate right would speed up the program significantly.
            // Unfortunately, FFMPEG can't return bitrate of only the video stream. So we will ONLY stream copy if video and all the audio streams combined have a lower bitrate than level 4.2 h264 video bitrate compatible with PS4 (6,25Mbit/s)
            var acceptableBitrate = (mediaInfo.AVBitrateKbs == null || mediaInfo.AVBitrateKbs <= TargetVideoBitrateKbs);

            return acceptableCodec && acceptableResolution && acceptableSize && acceptableBitrate;
        }

        private static bool HasAcceptableAudio(DConf dc, IMediaInfo mediaInfo)
        {
            if (dc.AcceptedAudioCodecs == null) throw new ArgumentNullException(nameof(dc.AcceptedAudioCodecs));
            if (mediaInfo == null) throw new ArgumentNullException(nameof(mediaInfo));
            if (mediaInfo.AudioCodec == null) throw new ArgumentNullException(nameof(mediaInfo.AudioCodec));

            return dc.AcceptedAudioCodecs.Contains(mediaInfo.AudioCodec);
        }

        private static int TargetVideoBitrateKbs => (int)(8) * 1024; // TODO: Read from config
    }
}
