using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    internal abstract class BaseScopesCombiner: IScopesEvaluator
    {
        protected List<IScopesEvaluator> _permissions;

        public BaseScopesCombiner(params IScopesEvaluator[] permissions) : this(permissions.AsEnumerable())
        { }

        public BaseScopesCombiner(IEnumerable<IScopesEvaluator> permissions)
        {
            _permissions = new List<IScopesEvaluator>(permissions);
        }

        public void Add(IScopesEvaluator permission)
        {
            _permissions.Add(permission);
        }

        public void AddRange(IEnumerable<IScopesEvaluator> permissions)
        {
            _permissions.AddRange(permissions);
        }

        public abstract bool AllowsScopes(IEnumerable<string> scopes);
    }
}
