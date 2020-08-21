﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// An <see cref="IScopesEvaluator"/> that combines
    /// other evaluators and returns the aggregate
    /// result of the combined evaluators.
    /// </summary>
    internal abstract class BaseScopesCombiner: IScopesEvaluator
    {
        public BaseScopesCombiner(params IScopesEvaluator[] evaluators) : this(evaluators.AsEnumerable())
        { }

        public BaseScopesCombiner(IEnumerable<IScopesEvaluator> permissions)
        {
            Evaluators = new List<IScopesEvaluator>(permissions);
        }

        protected List<IScopesEvaluator> Evaluators { get; private set; }

        public void Add(IScopesEvaluator evaluator)
        {
            Evaluators.Add(evaluator);
        }

        public void AddRange(IEnumerable<IScopesEvaluator> evaluators)
        {
            Evaluators.AddRange(evaluators);
        }

        public abstract bool AllowsScopes(IEnumerable<string> scopes);
    }
}
