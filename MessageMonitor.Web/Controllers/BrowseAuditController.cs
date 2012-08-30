using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MessageMonitor.Web.Controllers
{
    public class BrowseAuditController : Controller
    {
        
        public BrowseAuditController() 
        { 
        
        }

        public ActionResult Index()
        {
            return View();
        }

    }
}
