using System;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class Comment
    {
        private readonly PixivClient _client;
        private readonly Illust _illust;

        public Comment(PixivClient client, Illust illust, IllustComment api)
        {
            _client = client;
            _illust = illust;
            Id = api.Id;
            Content = api.Comment;
            Created = api.Date;
            User = new UserInfo(client, api.User);
            ParentCommentId = api.ParentComment?.Id switch
            {
                0 or null => null,
                int other => other
            };
        }

        public int Id { get; }
        public string Content { get; }
        public DateTimeOffset Created { get; }
        public UserInfo User { get; }

        public int? ParentCommentId { get; }

        public bool IsMine => User.Id == _client.CurrentUserId;

        public Task<Comment> ReplyAsync(string content)
            => _illust.PostCommentAsync(content, this);
    }
}
