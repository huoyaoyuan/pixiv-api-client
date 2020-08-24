using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class MotionPicMetadata
    {
        public class MetadataClass
        {
            public class Urls
            {
                public Uri Medium { get; set; }
            }
            public Urls ZipUrls { get; set; }

            public class Frame
            {
                public string File { get; set; }
                public int Delay { get; set; }
            }
            public ImmutableArray<Frame> Frames { get; set; }
        }

        public MetadataClass UgoiraMetadata { get; set; }
    }
}
