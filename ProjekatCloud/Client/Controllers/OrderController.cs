using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Fabric;

namespace Client.Controllers
{
    public class OrderController : Controller
    {

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
                    TempData["ErrorMessage"] = "Količina proizvoda nije uspješno smanjena.";
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
