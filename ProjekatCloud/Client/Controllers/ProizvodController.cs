using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Collections.Concurrent;
using System.Fabric;

namespace Client.Controllers
{
    public class ProizvodController : Controller
    {

        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            HttpContext.Session.Remove("KorisnikEmail");
            // Implementacija logike za odjavljivanje
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
                await proxy.IsprazniKorpu();
                return RedirectToAction("Index", "Home");

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home");
            }

        }


        [HttpGet]
        public async Task<IActionResult> ShowProducts()
        {
            // Provera da li je korisnik prijavljen
            if (HttpContext.Session.GetString("KorisnikEmail") == null)
            {
                // Ako korisnik nije prijavljen, preusmerite ga na stranicu za prijavu
                return RedirectToAction("Login", "User");
            }

           
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

                try { 
                    IEnumerable<Proizvod> proizvodi = await proxy.GetAllProducts();
                    // Grupisanje proizvoda po kategorijama
                    var grupisaniProizvodi = proizvodi.GroupBy(p => p.KategorijaProizvoda);
                    return View("ShowProducts", grupisaniProizvodi);
                }
                catch (Exception) 
                {
                    return View("Error"); // Ako nije pronađen proxy, prikaži grešku
                }
        }

    }
}
