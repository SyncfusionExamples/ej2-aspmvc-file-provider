using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Syncfusion.EJ2.FileManager.Base
{
    public class FileManagerParams
    {
        public string Name { get; set; }

        public string[] Names { get; set; }

        public string Path { get; set; }

        public string TargetPath { get; set; }

        public string NewName { get; set; }

        public object Date { get; set; }

        public IEnumerable<System.Web.HttpPostedFileBase> FileUpload { get; set; }

        public string[] RenameFiles { get; set; }
    }
}