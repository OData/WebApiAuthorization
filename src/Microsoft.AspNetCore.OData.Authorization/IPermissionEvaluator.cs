using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.OData.Authorization
{
    public interface IPermissionEvaluator
    {
        bool VerifyScopes(IEnumerable<string> scopes);
    }
}
