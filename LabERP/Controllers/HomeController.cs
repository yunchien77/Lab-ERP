using Microsoft.AspNetCore.Mvc;

namespace LabERP.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
