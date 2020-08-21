using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// Combines <see cref="IScopesEvaluator"/>s using a logical OR: returns
    /// true if any of the evaluators return true or if there
    /// are no evualtors added to the combiner.
    /// </summary>
    internal class WithOrScopesCombiner: BaseScopesCombiner
    {
        public WithOrScopesCombiner(params IScopesEvaluator[] evaluators) : base(evaluators)
        { }

        public WithOrScopesCombiner(IEnumerable<IScopesEvaluator> evaluators) : base(evaluators)
        { }

        public override bool AllowsScopes(IEnumerable<string> scopes)
        {
            if (!Evaluators.Any())
            {
                return true;
            }

            return Evaluators.Any(permission => permission.AllowsScopes(scopes));
        }
    }
}
