using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.OData.Authorization
{
    internal abstract class BasePermissionsCombinder: IPermissionEvaluator
    {
        protected List<IPermissionEvaluator> _permissions;

        public BasePermissionsCombinder(params IPermissionEvaluator[] permissions) : this(permissions.AsEnumerable())
        { }

        public BasePermissionsCombinder(IEnumerable<IPermissionEvaluator> permissions)
        {
            _permissions = new List<IPermissionEvaluator>(permissions);
        }

        public void Add(IPermissionEvaluator permission)
        {
            _permissions.Add(permission);
        }

        public void AddRange(IEnumerable<IPermissionEvaluator> permissions)
        {
            _permissions.AddRange(permissions);
        }

        public abstract bool VerifyScopes(IEnumerable<string> scopes);
    }
}
