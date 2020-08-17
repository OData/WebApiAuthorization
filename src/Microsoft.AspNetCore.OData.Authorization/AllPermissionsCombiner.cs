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
    internal class AllPermissionsCombiner: BasePermissionsCombinder
    {

        public AllPermissionsCombiner(params IPermissionHandler[] permissions) : base(permissions)
        { }

        public AllPermissionsCombiner(IEnumerable<IPermissionHandler> permissions) : base(permissions)
        { }

        public override bool VerifyScopes(IEnumerable<string> scopes)
        {
            return _permissions.All(permissions => permissions.VerifyScopes(scopes));
        }
    }
}
