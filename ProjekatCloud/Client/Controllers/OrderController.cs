using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Fabric;
using System.Security.Permissions;

namespace Client.Controllers
{
    public class OrderController : Controller
    {

        [HttpGet]
        public async Task<IActionResult> Istorija()
        {
            var emailKorisnika = HttpContext.Session.GetString("KorisnikEmail");


            var fabricClient = new FabricClient();
            var serviceUri = new Uri("fabric:/ProjekatCloud/OrderStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

            IOrderStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;
                var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);
                proxy = ServiceProxy.Create<IOrderStatefullService>(serviceUri, servicePartitionKey);
                break; // Ovde prekidamo petlju jer smo dobili jednu particiju
            }

            var porudzbine = await proxy.GetPorudzbineZaKorisnikaAsync(emailKorisnika);


            // Pretvorite niz u listu
            var porudzbineList = porudzbine.ToList();

            // Prosledite listu porudžbina pogledu
            return View(porudzbineList);
        }
    


        [HttpPost]
       /* public async Task<IActionResult> Naruci(List<Common.Proizvod> proizvodi, string nacinPlacanja, double ukupnaCijena)*/

        public async Task<IActionResult> Naruci(string nacinPlacanja, double ukupnaCijena)
        {
            var fabricClient = new FabricClient();
            var serviceUri = new Uri("fabric:/ProjekatCloud/OrderStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

            IOrderStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;
                var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);
                proxy = ServiceProxy.Create<IOrderStatefullService>(serviceUri, servicePartitionKey);
                break; // Ovde prekidamo petlju jer smo dobili jednu particiju
            }
            var emailKorisnika = HttpContext.Session.GetString("KorisnikEmail");
            //var serializedKorpa = HttpContext.Session.GetString("Korpa");
            //var korpa = JsonConvert.DeserializeObject<IEnumerable<Proizvod>>(serializedKorpa);


            try
            {
                var fabricClient2 = new FabricClient();
                var serviceUri2 = new Uri("fabric:/ProjekatCloud/ProductStatefullService");
                var partitionList2 = await fabricClient2.QueryManager.GetPartitionListAsync(serviceUri2);

                IProductStatefullService proxy2 = null;

                foreach (var partition2 in partitionList2)
                {
                    var partitionKey2 = partition2.PartitionInformation as Int64RangePartitionInformation;
                    var servicePartitionKey2 = new ServicePartitionKey(partitionKey2.LowKey);
                    proxy2 = ServiceProxy.Create<IProductStatefullService>(serviceUri2, servicePartitionKey2);
                    break; // Ovde prekidamo petlju jer smo dobili jednu particiju
                }
                var korpa = await proxy2.GetCijeluKorpu();

                var kreirano = await proxy.KreirajPorudzbinu(emailKorisnika, korpa, nacinPlacanja, ukupnaCijena);

                if (kreirano)
                {
                    var korpaObrisana = await proxy2.IsprazniKorpuPriPorudzbini();

                    // Nakon što je porudžbina uspješno kreirana
                    ViewBag.SuccessMessage = "Vaša porudžbina je uspješno kreirana!";
                    TempData["obavjestenje"] = "Uspješno ste kreirali porudžbinu!";
                    return RedirectToAction("ShowProducts", "Proizvod");
                }
                else
                {
                    return View("Error"); 
                }

            }
            catch (Exception)
            {
                return View("Error"); // Ako nije pronađen proxy, prikaži grešku
            }

            // Ovdje možete obrađivati odabrani način plaćanja i izvršiti odgovarajuće akcije, kao što su čuvanje u bazi podataka, slanje potvrde porudžbine korisniku, itd.
            // Na primjer, možete pozvati odgovarajući servis za kreiranje porudžbine i proslijediti odabrani način plaćanja.

            // Redirekcija na odgovarajuću stranicu nakon potvrde porudžbine
            return RedirectToAction("ShowKorpa", "Order");
        }


        [HttpPost]
        public async Task<IActionResult> UbaciUKorpu(int productId)
        {
            var fabricClient = new FabricClient();
            var serviceUri = new Uri("fabric:/ProjekatCloud/ProductStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

            IProductStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;
                var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);
                proxy = ServiceProxy.Create<IProductStatefullService>(serviceUri, servicePartitionKey);
                break; // Ovde prekidamo petlju jer smo dobili jednu particiju
            }

            try
            {
                var dostupan = await proxy.ReduceProductQuantity(productId);

                if (dostupan)
                {
                    var dodat = await proxy.DodajUKorpu(productId);
                    if (dodat) {
                        var korpa = await proxy.GetCijeluKorpu();
                        // Čuvanje korpe u sesiji
                       // var serializedKorpa = JsonConvert.SerializeObject(korpa); // Pretvaranje korpe u JSON format
                       // HttpContext.Session.SetString("Korpa", serializedKorpa);
                        return View("ShowKorpa", korpa);

                    }
                    else
                    {
                        return View("Error");
                    }             
                }
                else
                {
                    // Ako nije uspjelo smanjiti količinu proizvoda, prikaži odgovarajuću poruku
                    TempData["ErrorMessage"] = "Količina proizvoda nije uspješno smanjena, nema dovoljan broj proizvoda na stanju";
                    return View("Error");
                }

            }
            catch (Exception)
            {
                return View("Error"); // Ako nije pronađen proxy, prikaži grešku
            }
            // Implementiraj logiku za ubacivanje proizvoda u korpu
            // Možete pristupiti productId-u ovde i izvršiti potrebnu logiku
            // za proveru dostupnosti proizvoda i smanjenje količine u Reliable Dictionary-ju

            return RedirectToAction("ShowKorpa"); // Redirektujte na odgovarajuću akciju nakon dodavanja u korpu
        }

        [HttpGet]
        public async Task<IActionResult> ShowKorpa()
        {
            try
            {
                var fabricClient = new FabricClient();
                var serviceUri = new Uri("fabric:/ProjekatCloud/ProductStatefullService");
                var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

                IProductStatefullService proxy = null;

                foreach (var partition in partitionList)
                {
                    var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;
                    var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);
                    proxy = ServiceProxy.Create<IProductStatefullService>(serviceUri, servicePartitionKey);
                    break; // Ovde prekidamo petlju jer smo dobili jednu particiju
                }

                var korpa = await proxy.GetCijeluKorpu();
                return View(korpa);
            }
            catch (Exception)
            {
                return View("Error"); // Ako nije pronađen proxy, prikaži grešku
            }
        }

        [HttpPost]
        public async Task<IActionResult> IzbaciIzKorpe(int productId)
        {
            var fabricClient = new FabricClient();
            var serviceUri = new Uri("fabric:/ProjekatCloud/ProductStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

            IProductStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;
                var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);
                proxy = ServiceProxy.Create<IProductStatefullService>(serviceUri, servicePartitionKey);
                break; // Ovde prekidamo petlju jer smo dobili jednu particiju
            }

            try
            {
                var izbacen = await proxy.IzbaciIzKorpe(productId);

                if (izbacen)
                {
                    // Ako je proizvod uspješno izbačen iz korpe, preusmjeri na stranicu "ShowKorpa"
                    var korpa = await proxy.GetCijeluKorpu();

                    // Ažuriraj proizvod u Azure Storage-u
                    var productToUpdate = await proxy.GetProductById(productId);
                    if (productToUpdate != null)
                    {
                        await proxy.UpdateProductInStorage(productToUpdate);
                    }

                    return View("ShowKorpa", korpa);
                }
                else
                {
                    // Ako proizvod nije pronađen u korpi, prikaži odgovarajuću poruku
                    TempData["ErrorMessage"] = "Proizvod nije pronađen u korpi.";
                    return View("Error");
                }
            }
            catch (Exception)
            {
                return View("Error"); // Ako nije pronađen proxy, prikaži grešku
            }
        }


    }
}
