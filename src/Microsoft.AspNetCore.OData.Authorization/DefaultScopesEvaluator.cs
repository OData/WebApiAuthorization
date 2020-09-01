﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.OData.Authorization
{
    internal class DefaultScopesEvaluator : IScopesEvaluator
    {
        public bool AllowsScopes(IEnumerable<string> scopes)
        {
            return true;
        }
    }
}
