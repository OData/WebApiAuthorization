using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.OData.Authorization.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using Xunit;

namespace Microsoft.AspNetCore.OData.Authorization.Tests
{
    public class ODataModelPermissionExtractorTest
    {

        IEdmModel _model = TestModel.GetModelWithPermissions();
        string _serviceRoot = "http://odata/";
        DefaultODataPathHandler _parser = new DefaultODataPathHandler();
        IServiceProvider _serviceProvider;

        public ODataModelPermissionExtractorTest()
        {
            _serviceProvider = CreateServiceProvider(_model);
        }

        private static IServiceProvider CreateServiceProvider(IEdmModel model)
        {
            var container = new ServiceCollection();
            container.AddSingleton(model);
            container.AddSingleton<ODataUriResolver>();
            container.AddSingleton<ODataSimplifiedOptions>();
            container.AddSingleton<ODataUriParserSettings>();
            container.AddSingleton<UriPathParser>();
            container.AddSingleton<ODataOptions>();
            return container.BuildServiceProvider();
        }

        [Theory]
        // Entity set CRUD
        [InlineData("GET", "Products", "Product.ReadAll")]
        [InlineData("GET", "Products(1)", "Product.ReadAll")]
        [InlineData("GET", "Products(1)", "Product.ReadByKey")]
        [InlineData("PUT", "Products(1)", "Product.Update")]
        [InlineData("PATCH", "Products(1)", "Product.Update")]
        [InlineData("MERGE", "Products(1)", "Product.Update")]
        [InlineData("DELETE", "Products(1)", "Product.Delete")]
        [InlineData("POST", "Products", "Product.Insert")]
        // Singleton CRUD
        [InlineData("GET", "MyProduct", "MyProduct.Read")]
        [InlineData("PUT", "MyProduct", "MyProduct.Update")]
        [InlineData("PATCH", "MyProduct", "MyProduct.Update")]
        [InlineData("MERGE", "MyProduct", "MyProduct.Update")]
        [InlineData("DELETE", "MyProduct", "MyProduct.Delete")]
        // Property access
        [InlineData("GET", "Products(10)/Name", "Product.ReadByKey")]
        [InlineData("GET", "Products(10)/Name/$value", "Product.ReadByKey")]
        [InlineData("GET", "Products(10)/Tags/$count", "Product.ReadByKey")]
        [InlineData("DELETE", "Products(10)/Name", "Product.Update")]
        [InlineData("PATCH", "Products(10)/Name", "Product.Update")]
        [InlineData("PUT", "Products(10)/Name", "Product.Update")]
        [InlineData("POST", "Products(10)/Tags", "Product.Update")]
        [InlineData("GET", "MyProduct/Name", "MyProduct.Read")]
        [InlineData("PUT", "MyProduct/Name", "MyProduct.Update")]
        [InlineData("PATCH", "MyProduct/Name", "MyProduct.Update")]
        [InlineData("DELETE", "MyProduct/Name", "MyProduct.Update")]
        // Navigation Properties
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.ReadByKey,Customer.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.Read,ProductCustomers.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers(10)", "Product.Read,ProductCustomers.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers(10)", "Product.Read,ProductCustomers.ReadByKey")]
        [InlineData("GET", "Products(10)/RoutingCustomers(10)", "Product.ReadByKey,Customer.ReadByKey")]
        [InlineData("POST", "Products(10)/RoutingCustomers", "Product.Update,Customer.Insert")]
        [InlineData("POST", "Products(10)/RoutingCustomers", "Product.Update,ProductCustomers.Insert")]
        [InlineData("PUT", "Products(10)/RoutingCustomers(10)", "Product.Update,Customer.Update")]
        [InlineData("PUT", "Products(10)/RoutingCustomers(10)", "Product.Update,ProductCustomers.Update")]
        [InlineData("DELETE", "Products(10)/RoutingCustomers(10)", "Product.Update,Customer.Delete")]
        [InlineData("DELETE", "Products(10)/RoutingCustomers(10)", "Product.Update,ProductCustomers.Delete")]
        public void PermissionEvaluator_ReturnsTrue_IfScopesMatchRequiredPermissions(string method, string endpoint, string userScopes)
        {
            var path = _parser.Parse(_serviceRoot, endpoint, _serviceProvider);
            var scopesList = userScopes.Split(',');

            var permissionHandler = _model.ExtractPermissionsForRequest(method, path);

            Assert.True(permissionHandler.VerifyScopes(scopesList));
        }

        [Theory]
        [InlineData("GET", "Products", "")]
        [InlineData("GET", "Products", "Customers.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.ReadByKey")]
        [InlineData("GET", "Products(10)/RoutingCustomers", "ProductCustomers.Read")]
        public void PermissionEvaluator_ReturnsFalse_IfRequiredScopesNotFound(string method, string endpoint, string userScopes)
        {
            var path = _parser.Parse(_serviceRoot, endpoint, _serviceProvider);
            var scopesList = userScopes.Split(',');

            var permissionHandler = _model.ExtractPermissionsForRequest(method, path);

            Assert.False(permissionHandler.VerifyScopes(scopesList));
        }
    }
}
