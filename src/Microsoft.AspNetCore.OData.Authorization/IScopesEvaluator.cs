using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.OData.Authorization
{
    public interface IScopesEvaluator
    {
        bool AllowsScopes(IEnumerable<string> scopes);
    }
}
