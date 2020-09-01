using System;
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Interfaces;

namespace Microsoft.AspNetCore.OData.Authorization.Extensions
{
    public static class ODataBuilderExtensions
    {
        /// <summary>
        /// Adds OData model-based authorization services
        /// </summary>
        /// <param name="odataBuilder"></param>
        /// <returns></returns>
        public static IODataBuilder AddAuthorization(this IODataBuilder odataBuilder)
        {
            odataBuilder.Services.AddODataAuthorization();
            return odataBuilder;
        }

        /// <summary>
        /// Adds OData model-based authorization services
        /// </summary>
        /// <param name="odataBuilder"></param>
        /// <param name="configureODataAuth">Action to configure the authorization options</param>
        /// <returns></returns>
        public static IODataBuilder AddAuthorization(this IODataBuilder odataBuilder, Action<ODataAuthorizationOptions> configureODataAuth)
        {
            odataBuilder.Services.AddODataAuthorization(configureODataAuth);
            return odataBuilder;
        }
    }
}
