using System.Collections.Generic;
using System.Linq;

namespace AADB2C.GraphApi.Models {
    public class Credentials
    {
        public Credentials(IEnumerable<Tenant> tenants)
        {
            Tenants = tenants.ToList();
        }
        
        public ICollection<Tenant> Tenants { get; }
    }
}