﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using FFMpegSharp.Enums;
using FFMpegSharp.FFMPEG;
using FFMpegSharp.FFMPEG.Enums;
using FFMpegSharp.FFMPEG.Legacy;

namespace FFMpegSharp
{
    public partial class VideoInfo
    {
        private FFMpeg _ffmpeg;
        private FileInfo _file;

        /// <summary>
        ///     Returns the percentage of the current conversion progress.
        /// </summary>
        public FFMPEG.ConversionHandler OnConversionProgress;

        /// <summary>
        ///     Create a video information object from a file information object.
        /// </summary>
        /// <param name="fileInfo">Video file information.</param>
        public VideoInfo(FileInfo fileInfo)
        {
            fileInfo.Refresh();

            if (!fileInfo.Exists)
                throw new ArgumentException($"Input file {fileInfo.FullName} does not exist!");

            _file = fileInfo;

            new FFProbe().ParseVideoInfo(this);
        }

        /// <summary>
        ///     Create a video information object from a target path.
        /// </summary>
        /// <param name="path">Path to video.</param>
        public VideoInfo(string path) : this(new FileInfo(path))
        {
        }

        private FFMpeg FFmpeg
        {
            get
            {
                if (_ffmpeg != null && _ffmpeg.IsWorking)
                    throw new InvalidOperationException(
                        "Another operation is in progress, please wait for this to finish before launching another operation. To do multiple operations, create another VideoInfo object targeting that file.");

                return _ffmpeg ?? (_ffmpeg = new FFMpeg());
            }
        }

        /// <summary>
        ///     Duration of the video file.
        /// </summary>
        public TimeSpan Duration { get; internal set; }

        /// <summary>
        ///     Audio format of the video file.
        /// </summary>
        public string AudioFormat { get; internal set; }

        /// <summary>
        ///     Video format of the video file.
        /// </summary>
        public string VideoFormat { get; internal set; }

        /// <summary>
        ///     Aspect ratio.
        /// </summary>
        public string Ratio { get; internal set; }

        /// <summary>
        ///     Video frame rate.
        /// </summary>
        public double FrameRate { get; internal set; }

        /// <summary>
        ///     Height of the video file.
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        ///     Width of the video file.
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        ///     Video file size in MegaBytes (MB).
        /// </summary>
        public double Size { get; internal set; }

        /// <summary>
        ///     Gets the name of the file.
        /// </summary>
        public string Name => _file.Name;

        /// <summary>
        ///     Gets the full path of the file.
        /// </summary>
        public string FullName => _file.FullName;

        /// <summary>
        ///     Gets the file extension.
        /// </summary>
        public string Extension => _file.Extension;

        /// <summary>
        ///     Gets a flag indicating if the file is read-only.
        /// </summary>
        public bool IsReadOnly => _file.IsReadOnly;

        /// <summary>
        ///     Gets a flag indicating if the file exists (no cache, per call verification).
        /// </summary>
        public bool Exists => File.Exists(FullName);

        /// <summary>
        ///     Gets the creation date.
        /// </summary>
        public DateTime CreationTime => _file.CreationTime;

        /// <summary>
        ///     Gets the parent directory information.
        /// </summary>
        public DirectoryInfo Directory => _file.Directory;

        /// <summary>
        ///     See if ffmpeg process associated to this video is idle (not alive).
        /// </summary>
        public bool OperationIdle => !FFmpeg.IsWorking;

        /// <summary>
        ///     Create a video information object from a file information object.
        /// </summary>
        /// <param name="fileInfo">Video file information.</param>
        /// <returns></returns>
        public static VideoInfo FromFileInfo(FileInfo fileInfo)
        {
            return FromPath(fileInfo.FullName);
        }

        /// <summary>
        ///     Create a video information object from a target path.
        /// </summary>
        /// <param name="path">Path to video.</param>
        /// <returns></returns>
        public static VideoInfo FromPath(string path)
        {
            return new VideoInfo(path);
        }

        /// <summary>
        ///     Pretty prints the video information.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Video Path : " + FullName + Environment.NewLine +
                   "Video Root : " + Directory.FullName + Environment.NewLine +
                   "Video Name: " + Name + Environment.NewLine +
                   "Video Extension : " + Extension + Environment.NewLine +
                   "Video Duration : " + Duration + Environment.NewLine +
                   "Audio Format : " + AudioFormat + Environment.NewLine +
                   "Video Format : " + VideoFormat + Environment.NewLine +
                   "Aspect Ratio : " + Ratio + Environment.NewLine +
                   "Framerate : " + FrameRate + "fps" + Environment.NewLine +
                   "Resolution : " + Width + "x" + Height + Environment.NewLine +
                   "Size : " + Size + " MB";
        }

        /// <summary>
        ///     Open a file stream.
        /// </summary>
        /// <param name="mode">Opens a file in a specified mode.</param>
        /// <returns>File stream of the video file.</returns>
        public FileStream FileOpen(FileMode mode)
        {
            return _file.Open(mode);
        }

        /// <summary>
        ///     Move file to a specific directory.
        /// </summary>
        /// <param name="destination"></param>
        public void MoveTo(DirectoryInfo destination)
        {
            var newLocation = $"{destination.FullName}\\{Name}{Extension}";
            _file.MoveTo(newLocation);
            _file = new FileInfo(newLocation);
        }

        /// <summary>
        ///     Delete the file.
        /// </summary>
        public void Delete()
        {
            _file.Delete();
        }

        /// <summary>
        ///     Remove audio channel from video file.
        /// </summary>
        /// <param name="output">Location of the output video file.</param>
        /// <returns>Flag indicating if process ended succesfully.</returns>
        public bool Mute(FileInfo output)
        {
            return FFmpeg.Mute(this, output);
        }

        /// <summary>
        ///     Extract audio channel from video file.
        /// </summary>
        /// <param name="output">Location of the output video file.</param>
        /// <returns>Flag indicating if process ended succesfully.</returns>
        public bool ExtractAudio(FileInfo output)
        {
            return FFmpeg.ExtractAudio(this, output);
        }

        /// <summary>
        ///     Replace the audio of the video file.
        /// </summary>
        /// <param name="audio"></param>
        /// <param name="output"></param>
        /// <returns>Flag indicating if process ended succesfully.</returns>
        public bool ReplaceAudio(FileInfo audio, FileInfo output)
        {
            return FFmpeg.ReplaceAudio(this, audio, output);
        }

        /// <summary>
        ///     Join the video file with other video files.
        /// </summary>
        /// <param name="output">Output location of the resulting video file.</param>
        /// <param name="purgeSources">>Flag original file purging after conversion is done.</param>
        /// <param name="videos">Videos that need to be joined to the video.</param>
        /// <returns>Video information object with the new video file.</returns>
        public VideoInfo JoinWith(FileInfo output, bool purgeSources = false, params VideoInfo[] videos)
        {
            var queuedVideos = videos.ToList();

            queuedVideos.Insert(0, this);

            var success = FFmpeg.Join(output, queuedVideos.ToArray());

            if (!success)
                throw new OperationCanceledException("Could not join the videos.");

            if (purgeSources)
            {
                foreach (var video in videos)
                {
                    video.Delete();
                }
            }

            return new VideoInfo(output);
        }

        /// <summary>
        ///     Tell FFMpeg to stop the current process.
        /// </summary>
        public void CancelOperation()
        {
            FFmpeg.Stop();
        }
    }
}