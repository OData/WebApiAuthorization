// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics.Contracts;

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
        public async Task Invoke(HttpContext context)
        {
            Contract.Assert(context != null);

            var odataFeature = context.ODataFeature();
            if (odataFeature == null || odataFeature.Path == null)
            {
                await _next(context).ConfigureAwait(true);
                return;
            }

            IEdmModel model = context.Request.GetModel();
            if (model == null)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            var permissions = model.ExtractPermissionsForRequest(context.Request.Method, odataFeature.Path);
            ApplyRestrictions(permissions, context);

            await _next(context).ConfigureAwait(false);
        }

        private static void ApplyRestrictions(IScopesEvaluator handler, HttpContext context)
        {
            var requirement = new ODataAuthorizationScopesRequirement(handler);
            var policy = new AuthorizationPolicyBuilder().AddRequirements(requirement).Build();

            // We use the AuthorizeFilter instead of relying on the built-in authorization middleware
            // because we cannot add new metadata to the endpoint in the middle of a request
            // and OData's current implementation of endpoint routing does not allow for
            // adding metadata to individual routes ahead of time
            var authFilter = new AuthorizeFilter(policy);
            context.ODataFeature().ActionDescriptor?.FilterDescriptors?.Add(new FilterDescriptor(authFilter, 0));
        }

    }
}
