using Common;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Korisnik korisnik)
        {
            // Ovde dodajte logiku za registraciju korisnika
            // Na primer, možete proveriti korisničke podatke, dodati ih u bazu podataka itd.

            // Nakon što završite sa registracijom, možete preusmeriti korisnika na drugu stranicu
            return RedirectToAction("Index");
        }
    }
}
