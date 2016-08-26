using System;
using System.Drawing;
using System.IO;
using FFMpegSharp.FFMPEG.Enums;

namespace FFMpegSharp.FFMPEG
{
    public class Arguments
    {
        public class Argument {

            private readonly string arg;

            internal Argument(string arg) {
                this.arg = arg;
            }

            public string ArgumentText { get { return arg; } }

            public override string ToString() {
                return ArgumentText;
            }

            public static Argument operator +(Argument a,Argument b) {
                return new Argument($"{a.ArgumentText} {b.ArgumentText}");
            }
        }


        public static Argument Speed(SpeedProp speed)
        {
            return new Argument($"-preset {speed.ToString().ToLower()} ");
        }

        public static Argument Speed(int cpu)
        {
            return new Argument($"-quality good -cpu-used {cpu} -deadline realtime ");
        }

        public static Argument Audio(AudioCodec codec, int bitrate)
        {
            return new Argument($"-acodec {codec.ToString().ToLower()} -b:a {bitrate}k ");
        }

        public static Argument Video(VideoCodec codec, int bitrate = 0)
        {
            var video = $"-vcodec {codec.ToString().ToLower()}";

            if (bitrate > 0)
                video += $"-b:v {bitrate}k ";

            return new Argument(video);
        }

        public static Argument Threads(bool multiThread)
        {
            var threadCount = multiThread
                ? Environment.ProcessorCount.ToString()
                : "1";

            return new Argument($"-threads {threadCount} ");
        }

        public static Argument Input(Uri uri)
        {
            return new Argument($"-i \"{uri.AbsoluteUri}\" ");
        }

        public static Argument Disable(Channel type)
        {
            switch (type)
            {
                case Channel.Video:
                    return new Argument("-vn ");
                case Channel.Audio:
                    return new Argument("-an ");
                default:
                    return new Argument(string.Empty);
            }
        }

        public static Argument Input(VideoInfo input)
        {
            return new Argument($"-i \"{input.FullName}\" ");
        }

        public static Argument Input(FileInfo input)
        {
            return new Argument($"-i \"{input.FullName}\" ");
        }

        public static Argument Output(FileInfo output)
        {
            return new Argument($"\"{output}\"");
        }

        public static Argument Scale(VideoSize size)
        {
            return new Argument(size == VideoSize.Original ? string.Empty : $"-vf scale={(int) size} ");
        }

        public static Argument Scale(int size) {
            return new Argument($"-vf scale={size} ");
        }

        public static Argument Size(Size? size)
        {
            if (!size.HasValue) return new Argument(string.Empty);

            var formatedSize = $"{size.Value.Width}x{size.Value.Height}";

            return new Argument($"-s {formatedSize} ");
        }

        public static Argument ForceFormat(VideoCodec codec)
        {
            return new Argument($"-f {codec.ToString().ToLower()} ");
        }

        public static Argument BitStreamFilter(Channel type, Filter filter)
        {
            switch (type)
            {
                case Channel.Audio:
                    return new Argument($"-bsf:a {filter.ToString().ToLower()} ");
                case Channel.Video:
                    return new Argument($"-bsf:v {filter.ToString().ToLower()} ");
                default:
                    return new Argument(string.Empty);
            }
        }

        public static Argument Copy(Channel type = Channel.Both)
        {
            switch (type)
            {
                case Channel.Audio:
                    return new Argument("-c:a copy ");
                case Channel.Video:
                    return new Argument("-c:v copy ");
                default:
                    return new Argument("-c copy ");
            }
        }

        public static Argument Seek(TimeSpan? seek)
        {
            return new Argument(!seek.HasValue ? string.Empty : $"-ss {seek} ");
        }

        public static Argument FrameOutputCount(int number)
{
            return new Argument($"-vframes {number} ");
        }

        public static Argument Loop(int count)
        {
            return new Argument($"-loop {count} ");
        }

        public static Argument FinalizeAtShortestInput()
        {
            return new Argument("-shortest ");
        }

        public static Argument InputConcat(string[] paths)
        {
            return new Argument($"-i \"concat:{string.Join(@"|", paths)}\" ");
        }
    }
}