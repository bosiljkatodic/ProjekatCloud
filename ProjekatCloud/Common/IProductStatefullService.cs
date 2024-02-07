using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]

    public interface IProductStatefullService : IService
    {
        [OperationContract]
        public Task<IEnumerable<Proizvod>> GetAllProducts();
        [OperationContract]
        public Task<bool> ReduceProductQuantity(int productId);
        [OperationContract]
        public Task<bool> DodajUKorpu(int productId);

        [OperationContract]
        public Task<IEnumerable<Proizvod>> GetCijeluKorpu();

        [OperationContract]
        public Task<Proizvod> GetProductById(int productId);

    }
}
