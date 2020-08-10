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
    internal class AllPermissionsCombiner: IPermissionHandler
    {
        private readonly IEnumerable<IPermissionHandler> _items;

        public AllPermissionsCombiner(IEnumerable<IPermissionHandler> permissions)
        {
            _items = permissions;
        }

        public bool VerifyScopes(IEnumerable<string> scopes)
        {
            return _items.All(permissions => permissions.VerifyScopes(scopes));
        }
    }
}
