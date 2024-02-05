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

    public interface IValidator : IService
    {
        [OperationContract]
        Task<string> Validate(Korisnik korisnikPodaci);

        [OperationContract]
        Task<string> ValidateLogin(LoginViewModel loginViewModel);
    }
}
