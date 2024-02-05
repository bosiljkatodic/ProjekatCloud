using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Common; // Uverite se da imate pravilan using za vašu Korisnik klasu
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Fabric;
using System.ComponentModel.DataAnnotations;

namespace Client.Controllers
{
    public class UserController : Controller
    {


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            try
            {
                // Provera da li je model validan
                    IValidator proxy = null;

                    var fabricClient = new FabricClient();
                    var serviceUri = new Uri("fabric:/ProjekatCloud/Validator");


                    proxy = ServiceProxy.Create<IValidator>(serviceUri);
                    string result = await proxy.ValidateLogin(loginViewModel);

                    if (result == "Uspjesno logovanje")
                    {
                        // Uspela prijava - možete preusmeriti korisnika, postaviti sesiju, ili bilo šta drugo

                        return RedirectToAction("RegistrationSuccess"); //IZMIJENI!!!!!!

                    }
                    else
                    {
                        // Neuspela prijava - prikazivanje poruke o neuspeloj prijavi
                        ModelState.AddModelError(string.Empty, "Neispravni korisnički podaci");
                    }
            }
            catch (Exception ex)
            {
                // Greška prilikom prijave - možete prikazati opštu poruku o grešci ili logovati detalje
                ModelState.AddModelError(string.Empty, $"Greška prilikom prijave: {ex.Message}");
            }

            // Ako je došlo do ovde, postoji problem sa unosom podataka ili prijavom
            return View("Login", loginViewModel); // Vratite korisnika na istu stranicu sa modelom
        }

        [HttpPost]
        public async Task<IActionResult> Register(Korisnik korisnik)
        {
            
                try
                {

                    IValidator proxy = null;

                    var fabricClient = new FabricClient();
                    var serviceUri = new Uri("fabric:/ProjekatCloud/Validator");


                    proxy = ServiceProxy.Create<IValidator>(serviceUri);
                    string result = await proxy.Validate(korisnik);

                    if (result == "Uspjesna registracija")
                    {
                        // Uspesna registracija - možete preusmeriti korisnika, prikazati poruku, ili bilo šta drugo
                        return RedirectToAction("RegistrationSuccess");
                    }
                    else
                    {
                        // Neuspela registracija - možete prikazati poruku o grešci
                        ModelState.AddModelError(string.Empty, result);
                    }
                }
                catch (Exception ex)
                {
                    // Greška prilikom registracije - možete prikazati opštu poruku o grešci ili logovati detalje
                    ModelState.AddModelError(string.Empty, $"Error during registration: {ex.Message}");
                }
            

            // Ako je došlo do ovde, postoji problem sa unosom podataka ili registracijom
            return View("Register", korisnik); // Vratite korisnika na istu stranicu sa modelom
        }

        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }
    }
}
