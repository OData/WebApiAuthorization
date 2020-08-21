using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    /// <summary>
    /// Represents permission restrictions extracted from an OData model.
    /// </summary>
    internal class PermissionData: IScopesEvaluator
    {
        public string SchemeName { get; set; }
        public IList<PermissionScopeData> Scopes { get; set; }

        public bool AllowsScopes(IEnumerable<string> scopes)
        {
            var allowedScopes = Scopes.Select(s => s.Scope);
            return allowedScopes.Intersect(scopes).Any();
        }
    }

    internal class PermissionScopeData
    {
        public string Scope { get; set; }
        public string RestrictedProperties { get; set; }
    }
}
