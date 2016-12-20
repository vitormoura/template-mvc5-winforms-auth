using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LabAutenticacao.Controllers
{
    [Authorize(Roles="Documentador")]
    public class DocumentosController : AppController
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}