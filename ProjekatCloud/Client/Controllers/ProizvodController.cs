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
        /* public IActionResult Index()
         {
             return View();
         }
        */

        [HttpGet]
        public IActionResult LogOut()
        {
            // Implementacija logike za odjavljivanje
            HttpContext.Session.Remove("KorisnikEmail");

            return RedirectToAction("Index", "Home"); // Preusmeravanje na početnu stranicu
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IProductStatefullService proxy = null;

            var fabricClient = new FabricClient();
            var serviceUri = new Uri("fabric:/ProjekatCloud/ProductStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);

            proxy = ServiceProxy.Create<IProductStatefullService>(serviceUri);

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;

                var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);

                proxy = ServiceProxy.Create<IProductStatefullService>(serviceUri, servicePartitionKey);
                break;
            }

            try
            {
                IEnumerable<Proizvod> proizvodi = await proxy.GetAllProducts();
                // Grupisanje proizvoda po kategorijama
                var grupisaniProizvodi = proizvodi.GroupBy(p => p.KategorijaProizvoda);

                return View(grupisaniProizvodi);

        
            }
            catch (Exception)
            {
                return View("Error");
            }

        }


        [HttpGet]
        public IActionResult IzmeniProfil()
        {
            return RedirectToAction("Edit", "User");
        }
    }
}
