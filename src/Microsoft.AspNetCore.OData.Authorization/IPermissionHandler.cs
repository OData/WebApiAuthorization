using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.OData.Authorization
{
    public interface IPermissionHandler
    {
        bool VerifyScopes(IEnumerable<string> scopes);
    }
}
