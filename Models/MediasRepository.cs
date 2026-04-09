using DAL;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public class MediasRepository : Repository<Media>
    {
        public List<string> MediasCategories()
        {
            List<string> Categories = new List<string>();
            foreach (Media media in ToList().OrderBy(m => m.Category))
            {
                if (Categories.IndexOf(media.Category) == -1)
                {
                    Categories.Add(media.Category);
                }
            }
            return Categories;
        }

        public List<User> Authors(User connectedUser = null)
        {
            IEnumerable<Media> medias = ToList();
            if (connectedUser != null && !connectedUser.IsAdmin)
                medias = medias.Where(m => m.Shared || m.OwnerId == connectedUser.Id);

            List<int> ownerIds = medias
                .Select(m => m.OwnerId)
                .Distinct()
                .ToList();

            return DB.Users.ToList()
                .Where(u => ownerIds.Contains(u.Id))
                .OrderBy(u => u.Name)
                .Select(u => u.Copy())
                .ToList();
        }

        public bool ToggleLike(int mediaId, int userId)
        {
            Media media = Get(mediaId);
            if (media == null) return false;
            if (media.LikesUserIds == null) media.LikesUserIds = new List<int>();

            if (media.LikesUserIds.Contains(userId))
                media.LikesUserIds.Remove(userId);
            else
                media.LikesUserIds.Add(userId);

            return Update(media);
        }

        public void RemoveLikesByUser(int userId)
        {
            BeginTransaction();
            try
            {
                foreach (Media media in ToList().Where(m => m.LikesUserIds != null && m.LikesUserIds.Contains(userId)).ToList())
                {
                    media.LikesUserIds.Remove(userId);
                    base.Update(media);
                }
                EndTransaction();
            }
            catch
            {
                EndTransaction();
                throw;
            }
        }

        public void DeleteOwnedByUser(int userId)
        {
            BeginTransaction();
            try
            {
                foreach (Media media in ToList().Where(m => m.OwnerId == userId).ToList())
                {
                    base.Delete(media.Id);
                }
                EndTransaction();
            }
            catch
            {
                EndTransaction();
                throw;
            }
        }
    }
}