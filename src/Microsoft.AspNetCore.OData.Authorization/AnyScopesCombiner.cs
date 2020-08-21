using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// Combines a set of permission handlers using OR: returns
    /// true iff any of the handlers returns true.
    /// </summary>
    internal class AnyScopesCombiner: BaseScopesCombiner
    {
        public AnyScopesCombiner(params IScopesEvaluator[] permissions) : base(permissions)
        { }

        public AnyScopesCombiner(IEnumerable<IScopesEvaluator> permissions) : base(permissions)
        { }

        public override bool AllowsScopes(IEnumerable<string> scopes)
        {
            if (!_permissions.Any())
            {
                return true;
            }
            return _permissions.Any(permission => permission.AllowsScopes(scopes));
        }
    }
}
