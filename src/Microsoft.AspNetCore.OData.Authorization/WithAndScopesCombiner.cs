using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// Combines <see cref="IScopesEvaluator"/>s with a logical AND: returns true
    /// iff all evaluators return true or if evaluators are empty.
    /// </summary>
    internal class WithAndScopesCombiner: BaseScopesCombiner
    {

        public WithAndScopesCombiner(params IScopesEvaluator[] permissions) : base(permissions)
        { }

        public WithAndScopesCombiner(IEnumerable<IScopesEvaluator> permissions) : base(permissions)
        { }

        public override bool AllowsScopes(IEnumerable<string> scopes)
        {
            return Evaluators.All(permissions => permissions.AllowsScopes(scopes));
        }
    }
}
