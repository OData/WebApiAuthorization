using Microsoft.AspNet.OData.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.OData.Authorization.Extensions
{
    public static class ODataBuilderExtensions
    {
        public static IODataBuilder AddAuthorization(this IODataBuilder odataBuilder, Action<ODataAuthorizationOptions> configureODataAuth = null)
        {
            odataBuilder.Services.AddODataAuthorization(configureODataAuth);
            return odataBuilder;
        }
    }
}
