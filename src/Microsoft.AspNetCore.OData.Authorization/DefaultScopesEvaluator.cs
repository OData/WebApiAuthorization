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
