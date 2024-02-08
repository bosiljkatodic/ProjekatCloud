using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Common; 
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Fabric;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.ServiceFabric.Services.Client;

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
                    HttpContext.Session.SetString("KorisnikEmail", loginViewModel.Email);

                    return RedirectToAction("ShowProducts", "Proizvod");
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
                    HttpContext.Session.SetString("KorisnikEmail", korisnik.Email);

                    // Uspesna registracija - preusmeravanje na Index akciju kontrolera Proizvod
                    return RedirectToAction("ShowProducts", "Proizvod");
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

        [HttpPost]
        public async Task<IActionResult> UpdateUser(Korisnik korisnik)
        {
            try
            {
                // Provera da li je model validan
                IValidator proxy = null;
                var fabricClient = new FabricClient();
                var serviceUri = new Uri("fabric:/ProjekatCloud/Validator");
                proxy = ServiceProxy.Create<IValidator>(serviceUri);

                // Validacija korisnika
                string validationResult = await proxy.ValidateUpdate(korisnik);

                if (validationResult == "Uspjesna izmjena")
                {
                    HttpContext.Session.SetString("KorisnikEmail", korisnik.Email);

                    return RedirectToAction("ShowProducts", "Proizvod");
                }
                else
                {
                    return View("Error");

                }
            }
            catch (Exception ex)
            {
                // Greška prilikom ažuriranja korisnika - prikaži grešku
                ModelState.AddModelError(string.Empty, $"Greška prilikom ažuriranja korisnika: {ex.Message}");
                return View("UpdateUser", korisnik);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateUser()
        {
            // Dohvati email trenutno ulogovanog korisnika iz sesije
            var email = HttpContext.Session.GetString("KorisnikEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "User");
            }

            IUserStatefullService proxy = null;

            var fabricClient = new FabricClient();
            var serviceUri = new Uri("fabric:/ProjekatCloud/UserStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

            proxy = ServiceProxy.Create<IUserStatefullService>(serviceUri);

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;

                var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);

                proxy = ServiceProxy.Create<IUserStatefullService>(serviceUri, servicePartitionKey);
                break;
            }

            try
            {
                var korisnik = await proxy.GetUserByEmail(email);

                return View("UpdateUser", korisnik);

            }
            catch (Exception)
            {
                return View("Error");
            }
        }

    }
}
