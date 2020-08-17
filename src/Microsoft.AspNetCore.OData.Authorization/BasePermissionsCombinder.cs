using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    internal abstract class BasePermissionsCombinder: IPermissionHandler
    {
        protected List<IPermissionHandler> _permissions;

        public BasePermissionsCombinder(params IPermissionHandler[] permissions) : this(permissions.AsEnumerable())
        { }

        public BasePermissionsCombinder(IEnumerable<IPermissionHandler> permissions)
        {
            _permissions = new List<IPermissionHandler>(permissions);
        }

        public void Add(IPermissionHandler permission)
        {
            _permissions.Add(permission);
        }

        public void AddRange(IEnumerable<IPermissionHandler> permissions)
        {
            _permissions.AddRange(permissions);
        }

        public abstract bool VerifyScopes(IEnumerable<string> scopes);
    }
}
