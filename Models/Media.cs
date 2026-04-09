using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public enum MediaSortBy { Title, PublishDate, Likes }

    public class Media : Record
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string YoutubeId { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Now;
        public int OwnerId { get; set; } = 1;
        public bool Shared { get; set; } = true;
        public List<int> LikesUserIds { get; set; } = new List<int>();

        [JsonIgnore]
        public User Owner
        {
            get
            {
                User owner = DB.Users.Get(OwnerId);
                return owner != null ? owner.Copy() : null;
            }
        }

        [JsonIgnore]
        public int LikesCount
        {
            get
            {
                if (LikesUserIds == null) LikesUserIds = new List<int>();
                return LikesUserIds.Count;
            }
        }

        [JsonIgnore]
        public bool ConnectedUserLikesIt
        {
            get
            {
                if (LikesUserIds == null) LikesUserIds = new List<int>();
                if (User.ConnectedUser == null) return false;
                return LikesUserIds.Contains(User.ConnectedUser.Id);
            }
        }

        [JsonIgnore]
        public List<User> LikedByUsers
        {
            get
            {
                if (LikesUserIds == null) LikesUserIds = new List<int>();
                return DB.Users.ToList()
                    .Where(u => LikesUserIds.Contains(u.Id))
                    .OrderBy(u => u.Name)
                    .Select(u => u.Copy())
                    .ToList();
            }
        }

        [JsonIgnore]
        public string LikesToolTip
        {
            get
            {
                var users = LikedByUsers;
                if (users.Count == 0) return "Aucun j’aime";
                return string.Join(", ", users.Select(u => u.Name));
            }
        }

        public override bool IsValid()
        {
            if (LikesUserIds == null) LikesUserIds = new List<int>();
            if (!HasRequiredLength(Title, 1)) return false;
            if (!HasRequiredLength(Category, 1)) return false;
            if (!HasRequiredLength(Description, 1)) return false;
            if (DB.Medias.ToList().Where(m => m.YoutubeId == YoutubeId && m.Id != Id).Any()) return false;
            return true;
        }
    }
}