﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using FFMpegSharp.FFMPEG.Enums;
using FFMpegSharp.FFMPEG.Exceptions;
using FFMpegSharp.Helpers;
using FFMpegSharp.FFMPEG.Legacy;
using static FFMpegSharp.FFMPEG.Arguments;

namespace FFMpegSharp.FFMPEG
{
    public delegate void ConversionHandler(double percentage);

    public class FFMpeg : FFBase
    {

        public FFMpegLegacy legacy { get; private set; }

        /// <summary>
        ///     Intializes the FFMPEG encoder.
        /// </summary>
        public FFMpeg()
        {
            FfMpegHelper.RootExceptionCheck(ConfiguredRoot);
            FfProbeHelper.RootExceptionCheck(ConfiguredRoot);

            var target = Environment.Is64BitProcess ? "x64" : "x86";
            _ffmpegPath = ConfiguredRoot + $"\\{target}\\ffmpeg.exe";

            legacy = new FFMpegLegacy(_ffmpegPath);
        }

        /// <summary>
        /// Returns the percentage of the current conversion progress.
        /// </summary>
        public event ConversionHandler OnProgress;

        public bool DoFFMpegConversion(FileInfo output,Argument args) {
            return RunProcess(args, output);
        }

        /// <summary>
        ///     Joins a list of video files.
        /// </summary>
        /// <param name="output">Output video file.</param>
        /// <param name="videos">List of vides that need to be joined together.</param>
        /// <returns>Success state.</returns>
        public bool Join(FileInfo output, params VideoInfo[] videos)
        {
            var pathList = new string[videos.Length];

            for (var i = 0; i < pathList.Length; i++)
            {
                pathList[i] = videos[i].FullName.Replace(videos[i].Extension, FfMpegHelper.Extensions.Ts);
                    legacy.ToTs(videos[i],
                    new FileInfo(videos[i].FullName.Replace(FfMpegHelper.Extensions.Mp4, FfMpegHelper.Extensions.Ts)));
            }

            var conversionArgs = Arguments.InputConcat(pathList) +
                                 Arguments.Copy() +
                                 Arguments.BitStreamFilter(Channel.Audio, Filter.Aac_AdtstoAsc) +
                                 Arguments.Output(output);

            var result = RunProcess(conversionArgs, output);

            if (result)
                foreach (var path in pathList)
                    if (File.Exists(path))
                        File.Delete(path);

            return result;
        }

        /// <summary>
        ///     Records M3U8 streams to the specified output.
        /// </summary>
        /// <param name="uri">URI to pointing towards stream.</param>
        /// <param name="output">Output file</param>
        /// <returns>Success state.</returns>
        public bool SaveM3U8Stream(Uri uri, FileInfo output)
        {
            FfMpegHelper.ExtensionExceptionCheck(output, FfMpegHelper.Extensions.Mp4);

            if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                var conversionArgs = Arguments.Input(uri) +
                                     Arguments.Output(output);

                return RunProcess(conversionArgs, output);
            }
            throw new ArgumentException("Uri: {0}, does not point to a valid http(s) stream.", uri.AbsoluteUri);
        }

        /// <summary>
        ///     Strips a video file of audio.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <returns></returns>
        public bool Mute(VideoInfo source, FileInfo output)
        {
            FfMpegHelper.ConversionExceptionCheck(source, output);
            FfMpegHelper.ExtensionExceptionCheck(output, source.Extension);

            var args = Arguments.Input(source) +
                       Arguments.Copy() +
                       Arguments.Disable(Channel.Audio) +
                       Arguments.Output(output);

            return RunProcess(args, output);
        }

        /// <summary>
        ///     Saves audio from a specific video file to disk.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output audio file.</param>
        /// <returns>Success state.</returns>
        public bool ExtractAudio(VideoInfo source, FileInfo output)
        {
            FfMpegHelper.ConversionExceptionCheck(source, output);
            FfMpegHelper.ExtensionExceptionCheck(output, FfMpegHelper.Extensions.Mp3);

            var args = Arguments.Input(source) +
                       Arguments.Disable(Channel.Video) +
                       Arguments.Output(output);

            return RunProcess(args, output);
        }

        /// <summary>
        ///     Adds audio to a video file.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="audio">Source audio file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="stopAtShortest">Indicates if the encoding should stop at the shortest input file.</param>
        /// <returns>Success state</returns>
        public bool ReplaceAudio(VideoInfo source, FileInfo audio, FileInfo output, bool stopAtShortest = false , int aQuality = 160)
        {
            FfMpegHelper.ConversionExceptionCheck(source, output);
            FfMpegHelper.InputFilesExistExceptionCheck(audio);
            FfMpegHelper.ExtensionExceptionCheck(output, source.Extension);

            var args = Arguments.Input(source) +
                       Arguments.Input(audio) +
                       Arguments.Copy(Channel.Video) +
                       Arguments.Audio(AudioCodec.Aac, aQuality) +
                       Arguments.FinalizeAtShortestInput() +
                       Arguments.Output(output);

            return RunProcess(args, output);
        }

        /// <summary>
        ///     Stops any current job that FFMpeg is running.
        /// </summary>
        public void Stop()
        {
            if (IsWorking)
            {
                Process.StandardInput.Write('q');
            }
        }

        #region Private Members & Methods

        private readonly string _ffmpegPath;
        private TimeSpan _totalTime;

        private volatile string _errorData = string.Empty;

        private bool RunProcess(Argument args, FileInfo output)
        {
            var successState = true;

            RunProcess(args.ToString(), _ffmpegPath, true, rStandardError: true);

            try {
                Process.Start();
                Process.ErrorDataReceived += OutputData;
                Process.BeginErrorReadLine();
                Process.WaitForExit();
            }
            catch  {
                successState = false;
            }
            finally
            {
                Process.Close();

                if (File.Exists(output.FullName))
                    using (var file = File.Open(output.FullName, FileMode.Open))
                    {
                        if (file.Length == 0)
                        {
                            throw new FFMpegException(FFMpegExceptionType.Conversion, _errorData);
                        }
                    }
                else
                {
                    throw new FFMpegException(FFMpegExceptionType.Conversion, _errorData);
                }
            }
            return successState;
        }

        private void OutputData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            _errorData = e.Data;
#if DEBUG
            Trace.WriteLine(e.Data);
#endif

            if (OnProgress == null || !IsWorking) return;

            var r = new Regex(@"\w\w:\w\w:\w\w");
            var m = r.Match(e.Data);

            if (!e.Data.Contains("frame")) return;
            if (!m.Success) return;

            var t = TimeSpan.Parse(m.Value);
            var percentage = Math.Round(t.TotalSeconds/_totalTime.TotalSeconds*100, 2);
            OnProgress(percentage);
        }

        #endregion
    }
}