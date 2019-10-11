using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Hosting;
using Newtonsoft.Json;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using Syncfusion.EJ2.FileManager.Base;
using System.Web;
using System;

namespace EJ2ASPMVCFileProvider.Controllers
{
    public class FileManagerController : Controller
    {

        public ActionResult FileManager()
        {
            return View();
        }

        PhysicalFileProvider operation = new PhysicalFileProvider();
        public FileManagerController()
        {
            this.operation.RootFolder(HostingEnvironment.MapPath("~/Content/Files"));
        }

        public ActionResult FileOperations(FileManagerDirectoryContent args)
        {

            switch (args.Action)
            {
                case "read":
                    return Json(operation.ToCamelCase(operation.GetFiles(args.Path, args.ShowHiddenItems)));
                case "delete":
                    return Json(operation.ToCamelCase(operation.Delete(args.Path, args.Names)));
                case "details":
                    if(args.Names == null)
                    {
                       args.Names = new string[] { };
                    }
                    return Json(operation.ToCamelCase(operation.Details(args.Path, args.Names, args.Data)));
                case "create":
                    return Json(operation.ToCamelCase(operation.Create(args.Path, args.Name)));
                case "search":
                    return Json(operation.ToCamelCase(operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive)));
                case "copy":
                    return Json(operation.ToCamelCase(operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData)));
                case "move":
                    return Json(operation.ToCamelCase(operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData)));
                case "rename":
                    return Json(operation.ToCamelCase(operation.Rename(args.Path, args.Name, args.NewName)));
            }
            return null;

        }
        public ActionResult Upload(string path, IList<System.Web.HttpPostedFileBase> uploadFiles, string action)
        {
            if (path == null)
            {
                return Content("");
            }
            FileManagerResponse uploadResponse;
            uploadResponse = operation.Upload(path, uploadFiles, action, null);
            if (uploadResponse.Error != null)
            {
                HttpResponse Response = System.Web.HttpContext.Current.Response;
                Response.Clear();
                Response.Status = uploadResponse.Error.Code + " " + uploadResponse.Error.Message;
                Response.StatusCode = Int32.Parse(uploadResponse.Error.Code);
                Response.StatusDescription = uploadResponse.Error.Message;
                Response.End();
            }

            return Content("");
        }

        public ActionResult Download(string downloadInput)
        {
            FileManagerDirectoryContent args = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(downloadInput);
            return operation.Download(args.Path, args.Names);

        }


        public ActionResult GetImage(FileManagerDirectoryContent args)
        {
            return operation.GetImage(args.Path, args.Id, false, null, null);
        }
    
    }
}