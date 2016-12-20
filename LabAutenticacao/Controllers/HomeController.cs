using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LabAutenticacao.Controllers
{
    [Authorize]
    public class HomeController : AppController
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}