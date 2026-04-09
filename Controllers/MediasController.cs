using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using static Controllers.AccessControl;

[UserAccess(Access.View)]
public class MediasController : Controller
{
    private void InitSessionVariables()
    {
        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["SelectedOwnerId"] == null) Session["SelectedOwnerId"] = "0";
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["MediaSortBy"] == null) Session["MediaSortBy"] = MediaSortBy.PublishDate;
        if (Session["SortAscending"] == null) Session["SortAscending"] = false;
        ValidateSelectedCategory();
        ValidateSelectedOwner();

        if (Session["pageNum"] == null) Session["pageNum"] = 1;
        if (Session["firstPageSize"] == null) Session["firstPageSize"] = 12;
        if (Session["pageSize"] == null) Session["pageSize"] = 3;
        if (Session["EndOfMedias"] == null) Session["EndOfMedias"] = false;
    }

    private void ResetMediasPaging()
    {
        Session["pageNum"] = 1;
        Session["EndOfMedias"] = false;
    }

    private IEnumerable<Media> VisibleMedias()
    {
        if (Models.User.ConnectedUser.IsAdmin)
            return DB.Medias.ToList();

        return DB.Medias.ToList().Where(c => c.Shared || Models.User.ConnectedUser.Id == c.OwnerId);
    }

    private List<Media> _getItems(int index, int nbItems)
    {
        try
        {
            InitSessionVariables();

            IEnumerable<Media> result = VisibleMedias();

            bool search = (bool)Session["Search"];
            string searchString = (string)Session["SearchString"];

            if (search)
            {
                if (!string.IsNullOrWhiteSpace(searchString))
                    result = result.Where(c => (c.Title.ToLower() + c.Description.ToLower()).Contains(searchString));

                string selectedCategory = (string)Session["SelectedCategory"];
                if (!string.IsNullOrWhiteSpace(selectedCategory))
                    result = result.Where(c => c.Category == selectedCategory);

                string selectedOwnerId = Session["SelectedOwnerId"].ToString();
                int ownerId = 0;
                if (int.TryParse(selectedOwnerId, out ownerId) && ownerId > 0)
                    result = result.Where(c => c.OwnerId == ownerId);
            }

            if ((bool)Session["SortAscending"])
            {
                switch ((MediaSortBy)Session["MediaSortBy"])
                {
                    case MediaSortBy.Title:
                        result = result.OrderBy(c => c.Title);
                        break;
                    case MediaSortBy.PublishDate:
                        result = result.OrderBy(c => c.PublishDate);
                        break;
                    case MediaSortBy.Likes:
                        result = result.OrderBy(c => c.LikesCount).ThenBy(c => c.Title);
                        break;
                }
            }
            else
            {
                switch ((MediaSortBy)Session["MediaSortBy"])
                {
                    case MediaSortBy.Title:
                        result = result.OrderByDescending(c => c.Title);
                        break;
                    case MediaSortBy.PublishDate:
                        result = result.OrderByDescending(c => c.PublishDate);
                        break;
                    case MediaSortBy.Likes:
                        result = result.OrderByDescending(c => c.LikesCount).ThenBy(c => c.Title);
                        break;
                }
            }

            int resultCount = result.Count();
            if (resultCount < nbItems + index)
            {
                nbItems = resultCount - index;
                Session["EndOfMedias"] = true;
            }

            if (nbItems < 0) nbItems = 0;

            return result.Skip(index).Take(nbItems).ToList();
        }
        catch
        {
            return null;
        }
    }

    public ActionResult SetFirstPageSize(int pageSize)
    {
        Session["firstPageSize"] = pageSize;
        return null;
    }

    public ActionResult getNextMediasPage()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        if (!EndOfMedias)
        {
            Session["pageNum"] = (int)Session["pageNum"] + 1;
            int pageNum = (int)Session["pageNum"];
            int pageSize = (int)Session["pageSize"];
            int firstPageSize = (int)Session["firstPageSize"];
            Debug.WriteLine("PageNum: " + pageNum);
            IEnumerable<Media> mediasPage = _getItems(
                pageNum == 1 ? 0 : (pageNum - 2) * pageSize + firstPageSize,
                pageNum == 1 ? firstPageSize : pageSize);
            return PartialView("GetMedias", mediasPage);
        }
        return null;
    }

    public ActionResult EndOfMedias()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        return Json(EndOfMedias, JsonRequestBehavior.AllowGet);
    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            string selectedCategory = (string)Session["SelectedCategory"];
            if (!string.IsNullOrWhiteSpace(selectedCategory))
            {
                var medias = VisibleMedias().Where(c => c.Category == selectedCategory);
                if (!medias.Any())
                    Session["SelectedCategory"] = "";
            }
        }
    }

    private void ValidateSelectedOwner()
    {
        if (Session["SelectedOwnerId"] != null)
        {
            int selectedOwnerId = 0;
            if (int.TryParse(Session["SelectedOwnerId"].ToString(), out selectedOwnerId) && selectedOwnerId > 0)
            {
                var medias = VisibleMedias().Where(c => c.OwnerId == selectedOwnerId);
                if (!medias.Any())
                    Session["SelectedOwnerId"] = "0";
            }
        }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            bool search = (bool)Session["Search"];
            if (search)
                return PartialView();

            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult GetMediaDetails(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            int mediaId = (int)Session["CurrentMediaId"];
            Media media = DB.Medias.Get(mediaId);
            if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
                return PartialView(media);

            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult GetMedias(bool forceRefresh = false)
    {
        try
        {
            if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
            {
                InitSessionVariables();
                int pageNum = (int)Session["pageNum"];
                int pageSize = (int)Session["pageSize"];
                int firstPageSize = (int)Session["firstPageSize"];
                return PartialView(_getItems(0, pageNum > 1 ? (pageNum - 1) * pageSize + firstPageSize : firstPageSize));
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }

    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }

    public ActionResult ToggleSearch()
    {
        ResetMediasPaging();
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }

    public ActionResult SetMediaSortBy(MediaSortBy mediaSortBy)
    {
        ResetMediasPaging();
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }

    public ActionResult ToggleMediaSort()
    {
        ResetMediasPaging();
        int mediaSortBy = (int)Session["MediaSortBy"] + 1;
        if (mediaSortBy >= Enum.GetNames(typeof(MediaSortBy)).Length) mediaSortBy = 0;
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }

    public ActionResult ToggleSort()
    {
        ResetMediasPaging();
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }

    public ActionResult SetSearchString(string value)
    {
        ResetMediasPaging();
        Session["SearchString"] = (value ?? "").ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        ResetMediasPaging();
        Session["SelectedCategory"] = value ?? "";
        return RedirectToAction("List");
    }

    public ActionResult SetSearchOwner(string value)
    {
        ResetMediasPaging();
        Session["SelectedOwnerId"] = string.IsNullOrWhiteSpace(value) ? "0" : value;
        return RedirectToAction("List");
    }

    [UserAccess(Access.View)]
    public ActionResult ToggleMediaLike(int id)
    {
        Media media = DB.Medias.Get(id);
        if (media == null) return new HttpStatusCodeResult(404);

        bool visible = media.Shared || Models.User.ConnectedUser.IsAdmin || media.OwnerId == Models.User.ConnectedUser.Id;
        if (!visible) return new HttpStatusCodeResult(401);

        DB.Medias.ToggleLike(id, Models.User.ConnectedUser.Id);
        return null;
    }

    public ActionResult About()
    {
        return View();
    }

    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media media = DB.Medias.Get(id);
        Session["UserCanEditCurrentMedia"] = false;
        if (media != null)
        {
            Session["CurrentMediaTitle"] = media.Title;
            Session["UserCanEditCurrentMedia"] = media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin;
            return View(media);
        }
        return RedirectToAction("List");
    }

    [UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }

    [HttpPost]
    [UserAccess(Access.Write)]
    [ValidateAntiForgeryToken()]
    public ActionResult Create(Media media, string sharedCB = "off")
    {
        if (media.IsValid())
        {
            media.OwnerId = Models.User.ConnectedUser.Id;
            media.Shared = sharedCB == "on";
            if (media.LikesUserIds == null) media.LikesUserIds = new List<int>();
            DB.Medias.Add(media);
            DB.Events.Add("Create", media.Title);
            return RedirectToAction("List");
        }
        DB.Events.Add("Illegal Create Media");
        return Redirect("/Accounts/Login?message=Erreur de creation de Media!&success=false");
    }

    [UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media media = DB.Medias.Get(id);
            if (media != null)
            {
                if (media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                    return View(media);
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    [UserAccess(Access.Write)]
    [HttpPost]
    [ValidateAntiForgeryToken()]
    public ActionResult Edit(Media media, string sharedCB = "off")
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        Media storedMedia = DB.Medias.Get(id);

        if (storedMedia != null)
        {
            bool userCanEdit = storedMedia.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin;
            if (!userCanEdit)
            {
                DB.Events.Add("Illegal Edit Media");
                return Redirect("/Accounts/Login?message=Accès illégal!&success=false");
            }

            media.Shared = sharedCB == "on";
            media.Id = id;
            media.OwnerId = storedMedia.OwnerId;
            media.PublishDate = storedMedia.PublishDate;
            media.LikesUserIds = storedMedia.LikesUserIds ?? new List<int>();

            if (media.IsValid())
            {
                DB.Medias.Update(media);
                return RedirectToAction("Details/" + id);
            }
        }

        DB.Events.Add("Illegal Edit Media");
        return Redirect("/Accounts/Login?message=Erreur de modification de Media!&success=false");
    }

    [UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media media = DB.Medias.Get(id);
            if (media != null)
            {
                if (media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                {
                    DB.Medias.Delete(id);
                    return RedirectToAction("List");
                }
                return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(), JsonRequestBehavior.AllowGet);
    }
}