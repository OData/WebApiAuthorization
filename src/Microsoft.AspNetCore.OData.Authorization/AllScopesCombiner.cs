using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// Combines a set of permission handlers. Returns true
    /// iff all handlers return true.
    /// </summary>
    internal class AllScopesCombiner: BaseScopesCombiner
    {

        public AllScopesCombiner(params IScopesEvaluator[] permissions) : base(permissions)
        { }

        public AllScopesCombiner(IEnumerable<IScopesEvaluator> permissions) : base(permissions)
        { }

        public override bool AllowsScopes(IEnumerable<string> scopes)
        {
            return _permissions.All(permissions => permissions.AllowsScopes(scopes));
        }
    }
}
