using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
    public class ProizvodController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
