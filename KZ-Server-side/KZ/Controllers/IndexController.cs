using KZ.ViewModels;
using System.Web.Mvc;

namespace KZ.Controllers
{
    public class IndexController : Controller
    {
        [Route("")]
        [Route("manage-users/{*route?}")]
        [Route("sc/{*route?}")]
        [Route("experimental/{*route?}")]
        public ActionResult Index()
        {
            IndexViewModel viewModel = new IndexViewModel();
            return View("Index", viewModel);
        }
    }
}