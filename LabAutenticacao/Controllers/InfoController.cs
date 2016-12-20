using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace LabAutenticacao.Controllers
{
    [AllowAnonymous]
    public class InfoController : Controller
    {
        
        public ActionResult Headers()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var h in this.Request.Headers.AllKeys)
            {
                sb.AppendFormat("{0} = {1}<br />", h, this.Request.Headers[h]);
            }

            return Content(sb.ToString());
        }
    }
}