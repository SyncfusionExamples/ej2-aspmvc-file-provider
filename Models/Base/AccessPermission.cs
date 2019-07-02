using System;
using System.Collections.Generic;
using System.Linq;
#if EJ2_DNX
using System.Web;
#endif

namespace Syncfusion.EJ2.FileManager.Base
{
    public class AccessPermission
    {
        public bool Copy = true;
        public bool Download = true;
        public bool Edit = true;
        public bool EditContents = true;
        public bool Read = true;
        public bool Upload = true;
    }
}