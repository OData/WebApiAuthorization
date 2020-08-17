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
    internal class AnyPermissionsCombiner: BasePermissionsCombinder
    {
        public AnyPermissionsCombiner(params IPermissionHandler[] permissions) : base(permissions)
        { }

        public AnyPermissionsCombiner(IEnumerable<IPermissionHandler> permissions) : base(permissions)
        { }

        public override bool VerifyScopes(IEnumerable<string> scopes)
        {
            return _permissions.Any(permission => permission.VerifyScopes(scopes));
        }
    }
}
