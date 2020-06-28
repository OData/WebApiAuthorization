using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;

namespace AspNetCore3ODataPermissionsSample.Models
{
    public class AppModel
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Order>("Orders");
            return builder.GetEdmModel();
        }
    }
}
