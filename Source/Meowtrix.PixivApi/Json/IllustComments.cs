using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class IllustComments
    {
        public int TotalComments { get; set; }
        public ImmutableArray<IllustComment> Comments { get; set; }
        public Uri NextUri { get; set; }
    }

    public class IllustComment
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public string Date { get; set; }
        public IllustUser User { get; set; }
        public bool HasReplies { get; set; }
        public IllustComment ParentComment { get; set; }
    }
}
