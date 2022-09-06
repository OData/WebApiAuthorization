// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OData.Edm;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// The OData authorization middleware
    /// </summary>
    public class ODataAuthorizationMiddleware
    {
        private RequestDelegate _next;

        /// <summary>
        /// Instantiates a new instance of <see cref="ODataAuthorizationMiddleware"/>.
        /// </summary>
        public ODataAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context">The http context.</param>
        /// <returns>A task that can be awaited.</returns>
        public Task Invoke(HttpContext context)
        {
            Contract.Assert(context != null);

            var odataFeature = context.ODataFeature();

            if (odataFeature == null || odataFeature.Path == null)
            {
                return _next(context);
            }

            IEdmModel model = context.Request.GetModel();

            if (model == null)
            {
                return _next(context);
            }

            // At this point in the Middleware the SelectExpandClause hasn't been evaluated (https://github.com/OData/WebApiAuthorization/issues/4)
            if (odataFeature.SelectExpandClause == null)
            {
                var elementType = odataFeature.Path.LastOrDefault(x => x.EdmType != null);

                if (elementType != null)
                {
                    var queryOptions = new ODataQueryOptions(
                        new ODataQueryContext(model, elementType.EdmType.AsElementType(), odataFeature.Path),
                        context.Request);

                    odataFeature.SelectExpandClause = queryOptions.SelectExpand?.SelectExpandClause;
                }
            }

            var permissions = model.ExtractPermissionsForRequest(context.Request.Method, odataFeature.Path, odataFeature.SelectExpandClause);

            ApplyRestrictions(permissions, context);

            return _next(context);
        }

        private static void ApplyRestrictions(IScopesEvaluator handler, HttpContext context)
        {
            var requirement = new ODataAuthorizationScopesRequirement(handler);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(requirement)
                .Build();

            // We use the AuthorizeFilter instead of relying on the built-in authorization middleware
            // because we cannot add new metadata to the endpoint in the middle of a request
            // and OData's current implementation of endpoint routing does not allow for
            // adding metadata to individual routes ahead of time
            var authFilter = new AuthorizeFilter(policy);

            var controllerActionDescriptor = context.GetEndpoint().Metadata.GetMetadata<ControllerActionDescriptor>();

            if(controllerActionDescriptor != null)
            {
                controllerActionDescriptor.FilterDescriptors?.Add(new FilterDescriptor(authFilter, 0));
            }
        }

    }
}
