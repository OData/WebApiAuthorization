using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNetCore.OData.Authorization
{
    internal static class ODataModelPermissionsExtractor
    {
        /// <summary>
        /// Extract permissions from the <paramref name="model"/> that should apply to the current request.
        /// </summary>
        /// <param name="model">The OData model.</param>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        internal static IEnumerable<IPermissionHandler> ExtractPermissionsForRequest(this IEdmModel model, HttpContext context)
        {
            IODataFeature odataFeature = context.ODataFeature();

            var odataPath = odataFeature.Path;
            var template = odataPath.PathTemplate;
            var method = context.Request.Method;

            // $ref segment does not appear in odataPath.Segments, that's why we treat this case separately
            if (template.EndsWith("$ref"))
            {
                // for ref segments, we apply the permission of the entity that contains the navigation property
                // e.g. for GET Customers(10)/Products/$ref, we apply the read key permissions of Customers
                // for GET TopCustomer/Products/$ref, we apply the read permissions of TopCustomer
                // for DELETE Customers(10)/Products(10)/$ref we apply the update permissions of Customers
                var index = odataPath.Segments.Count - 2;
                while (!(odataPath.Segments[index] is KeySegment || odataPath.Segments[index] is SingletonSegment) && index > 0)
                {
                    index--;
                }

                if (odataPath.Segments[index] is SingletonSegment singletonSegment)
                {
                    return GetSingletonPropertyOperationPermissions(singletonSegment.Singleton, model, method);
                }
                else if (odataPath.Segments[index] is KeySegment keySegment)
                {
                    var entitySet = keySegment.NavigationSource as IEdmEntitySet;
                    return GetEntityPropertyOperationPermissions(entitySet, model, method);
                }
            }
            else
            {
                ODataPathSegment mainSegment = null;
                int index = odataPath.Segments.Count - 1;
                for (; index >= 0; index--)
                {
                    var segment = odataPath.Segments[index];
                    if (segment is EntitySetSegment ||
                        segment is SingletonSegment ||
                        segment is NavigationPropertySegment ||
                        segment is OperationSegment ||
                        segment is OperationImportSegment ||
                        segment is KeySegment ||
                        segment is PropertySegment)
                    {
                        mainSegment = segment;
                        break;
                    }
                }

                if (mainSegment is EntitySetSegment entitySetSegment)
                {
                    return GetNavigationSourceCrudPermissions(entitySetSegment.EntitySet, model, method);
                }
                else if (mainSegment is SingletonSegment singletonSegment)
                {
                    return GetNavigationSourceCrudPermissions(singletonSegment.Singleton, model, method);
                }
                else if (mainSegment is NavigationPropertySegment navigationSegment)
                {
                    var target = navigationSegment.NavigationSource as IEdmVocabularyAnnotatable;
                    return GetNavigationSourceCrudPermissions(target, model, method);
                }
                else if (mainSegment is KeySegment keySegment)
                {
                    var entitySet = keySegment.NavigationSource as IEdmEntitySet;
                    return GetEntityCrudPermissions(entitySet, model, method);
                }
                else if (mainSegment is OperationSegment operationSegment)
                {
                    var annotations = operationSegment.Operations.First().VocabularyAnnotations(model);
                    return GetOperationPermissions(annotations);
                }
                else if (mainSegment is OperationImportSegment operationImportSegment)
                {
                    var annotations = operationImportSegment.OperationImports.First().Operation.VocabularyAnnotations(model);
                    return GetOperationPermissions(annotations);
                }
                else if (mainSegment is PropertySegment)
                {
                    // for operations on a property, we apply the permissions of the
                    // entity or singleton that contain the property
                    // e.g. for GET Products(10)/Name, we apply the read key permissions of Products
                    // for GET TopProduct/Name, we apply the read permissions of TopProduct
                    int entityIndex = index;
                    while (!(odataPath.Segments[entityIndex] is SingletonSegment || odataPath.Segments[entityIndex] is KeySegment) && entityIndex > 0)
                    {
                        entityIndex--;
                    }

                    if (odataPath.Segments[entityIndex] is SingletonSegment containingSingleton)
                    {
                        return GetSingletonPropertyOperationPermissions(containingSingleton.Singleton, model, method);
                    }
                    else if (odataPath.Segments[entityIndex] is KeySegment containingEntity)
                    {
                        var entitySet = containingEntity.NavigationSource as IEdmEntitySet;
                        return GetEntityPropertyOperationPermissions(entitySet, model, method);
                    }
                }
            }

            return Enumerable.Empty<PermissionData>();
        }

        internal static IPermissionHandler ExtractPermissionsForRequestWithNavSupport(this IEdmModel model, HttpContext context)
        {
            IODataFeature odataFeature = context.ODataFeature();
            

            var odataPath = odataFeature.Path;
            var template = odataPath.PathTemplate;
            var method = context.Request.Method;
            ODataPathSegment prevSegment = null;

            var segments = new List<ODataPathSegment>();

            var permissionsChain = new AllPermissionsCombiner();

            var lastSegmentIndex = odataPath.Segments.Count - 1;

            if (template.EndsWith("$ref"))
            {
                // for ref segments, we apply the permission of the entity that contains the navigation property
                // e.g. for GET Customers(10)/Products/$ref, we apply the read key permissions of Customers
                // for GET TopCustomer/Products/$ref, we apply the read permissions of TopCustomer
                // for DELETE Customers(10)/Products(10)/$ref we apply the update permissions of Customers
                lastSegmentIndex = odataPath.Segments.Count - 2;
                while (!(odataPath.Segments[lastSegmentIndex] is KeySegment || odataPath.Segments[lastSegmentIndex] is SingletonSegment || odataPath.Segments[lastSegmentIndex] is NavigationPropertySegment)
                    && lastSegmentIndex > 0)
                {
                    lastSegmentIndex--;
                }
            }

            for (int i = 0; i <= lastSegmentIndex; i++)
            {
                var segment = odataPath.Segments[i];
                
                if (segment is EntitySetSegment ||
                        segment is SingletonSegment ||
                        segment is NavigationPropertySegment ||
                        segment is OperationSegment ||
                        segment is OperationImportSegment ||
                        segment is KeySegment ||
                        segment is PropertySegment)
                {
                    var parent = prevSegment;
                    var isPropertyAccess = IsNextSegmentOfType<PropertySegment>(odataPath, i) ||
                        IsNextSegmentOfType<NavigationPropertyLinkSegment>(odataPath, i) ||
                        IsNextSegmentOfType<NavigationPropertySegment>(odataPath, i);
                    prevSegment = segment;
                    segments.Add(segment);

                    // if nested segment, extract navigation restrictions of root

                    // else extract entity/set  restrictions
                    if (segment is EntitySetSegment entitySetSegment)
                    {

                        // if Customers(key), then we'll handle it when we reach the key segment
                        // so that we can properly handle ReadByKeyRestrictions
                        if (IsNextSegmentKey(odataPath, i))
                        {
                            continue;
                        }

                        // if Customers/UnboundFunction, then we'll handle it when we reach there
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        IEnumerable<IPermissionHandler> permissions;
                        
                        permissions = GetNavigationPropertyCrudPermisions(
                            segments,
                            false,
                            model,
                            method);

                        if (!permissions.Any())
                        {
                            permissions = GetNavigationSourceCrudPermissions(entitySetSegment.EntitySet, model, method);
                        }

                        var handler = new AnyPermissionsCombiner(permissions);
                        permissionsChain.Add(handler);
                    }
                    else if (segment is SingletonSegment singletonSegment)
                    {
                        // if Customers/UnboundFunction, then we'll handle it when we reach there
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        if (isPropertyAccess)
                        {
                            var propertyPermissions = GetSingletonPropertyOperationPermissions(singletonSegment.Singleton, model, method);
                            permissionsChain.Add(new AnyPermissionsCombiner(propertyPermissions));
                        }
                        else
                        {
                            var permissions = GetNavigationSourceCrudPermissions(singletonSegment.Singleton, model, method);
                            permissionsChain.Add(new AnyPermissionsCombiner(permissions));
                        }
                    }
                    else if (segment is KeySegment keySegment)
                    {
                        // if Customers/UnboundFunction, then we'll handle it when we reach there
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        var entitySet = keySegment.NavigationSource as IEdmEntitySet;
                        var permissions =  isPropertyAccess ?
                            GetEntityPropertyOperationPermissions(entitySet, model, method) :
                            GetEntityCrudPermissions(entitySet, model, method);

                        var handler = new AnyPermissionsCombiner(permissions);


                        if (parent is NavigationPropertySegment)
                        {
                            var nestedPermissions = isPropertyAccess ?
                                GetNavigationPropertyPropertyOperationPermisions(segments, isTargetByKey: true, model, method) :
                                GetNavigationPropertyCrudPermisions(segments, isTargetByKey: true, model, method);

                            handler.AddRange(nestedPermissions);
                        }
                        

                        permissionsChain.Add(handler);
                    }
                    else if (segment is NavigationPropertySegment navSegment)
                    {
                        // if Customers/UnboundFunction, then we'll handle it when we reach there
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }


                        // if Customers(key), then we'll handle it when we reach the key segment
                        // so that we can properly handle ReadByKeyRestrictions
                        if (IsNextSegmentKey(odataPath, i))
                        {
                            continue;
                        }

                        var topLevelPermissions = GetNavigationSourceCrudPermissions(navSegment.NavigationSource as IEdmVocabularyAnnotatable, model, method);
                        var topLevelHandler = new AnyPermissionsCombiner(topLevelPermissions);

                        var nestedPermissions = GetNavigationPropertyCrudPermisions(
                            segments,
                            isTargetByKey: false,
                            model,
                            method);

                        var nestedHandler = new AnyPermissionsCombiner(nestedPermissions);

                        permissionsChain.Add(new AnyPermissionsCombiner(topLevelHandler, nestedHandler));
                    }
                    else if (segment is OperationImportSegment operationImportSegment)
                    {
                        var annotations = operationImportSegment.OperationImports.First().Operation.VocabularyAnnotations(model);
                        var permissions = GetOperationPermissions(annotations);
                        permissionsChain.Add(new AnyPermissionsCombiner(permissions));
                    }
                    else if (segment is OperationSegment operationSegment)
                    {
                        var annotations = operationSegment.Operations.First().VocabularyAnnotations(model);
                        var operationPermissions = GetOperationPermissions(annotations);
                        permissionsChain.Add(new AnyPermissionsCombiner(operationPermissions));
                    }
                }
            }

            return permissionsChain;
        }

        private static IEnumerable<IPermissionHandler> GetNavigationPropertyCrudPermisions(IList<ODataPathSegment> pathSegments, bool isTargetByKey, IEdmModel model, string method)
        {
            if (pathSegments.Count <= 1) yield break;

            var expectedPath = GetPathFromSegments(pathSegments);
            IEdmVocabularyAnnotatable root = (pathSegments[0] as EntitySetSegment)?.EntitySet as IEdmVocabularyAnnotatable ??
                (pathSegments[0] as SingletonSegment)?.Singleton;

            var navRestrictions = root.VocabularyAnnotations(model).Where(a => a.Term.FullName() == ODataCapabilityRestrictionsConstants.NavigationRestrictions);
            foreach (var restriction in navRestrictions)
            {
                if (restriction.Value is IEdmRecordExpression record)
                {
                    var temp = record.FindProperty("RestrictedProperties");
                    if (temp?.Value is IEdmCollectionExpression restrictedProperties)
                    {
                        foreach (var item in restrictedProperties.Elements)
                        {
                            if (item is IEdmRecordExpression restrictedProperty)
                            {
                                var navigationProperty = restrictedProperty.FindProperty("NavigationProperty").Value as IEdmPathExpression;
                                if (navigationProperty?.Path == expectedPath)
                                
                                {
                                    if (method == "GET")
                                    {
                                        var readRestrictions = restrictedProperty.FindProperty("ReadRestrictions")?.Value as IEdmRecordExpression;
                                      
                                        var readPermissions = ExtractPermissionsFromRecord(readRestrictions);
                                        yield return new AnyPermissionsCombiner(readPermissions);
                                        
                                        if (isTargetByKey)
                                        {
                                            var readByKeyRestrictions = readRestrictions.FindProperty("ReadByKeyRestrictions")?.Value as IEdmRecordExpression;
                                            var readByKeyPermissions =  ExtractPermissionsFromRecord(readByKeyRestrictions);
                                            yield return new AnyPermissionsCombiner(readByKeyPermissions);
                                        }
                                    }
                                    else if (method == "POST")
                                    {
                                        var insertRestrictions = restrictedProperty.FindProperty("InsertRestrictions")?.Value as IEdmRecordExpression;
                                        var insertPermissions = ExtractPermissionsFromRecord(insertRestrictions);
                                        yield return new AnyPermissionsCombiner(insertPermissions);
                                    }
                                    else if (method == "PATCH" || method == "PUT" || method == "PATCH")
                                    {
                                        var updateRestrictions = restrictedProperty.FindProperty("UpdateRestrictions")?.Value as IEdmRecordExpression;
                                        var updatePermissions = ExtractPermissionsFromRecord(updateRestrictions);
                                        yield return new AnyPermissionsCombiner(updatePermissions);
                                    }
                                    else if (method == "DELETE")
                                    {
                                        var deleteRestrictions = restrictedProperty.FindProperty("DeleteRestrictions")?.Value as IEdmRecordExpression;
                                        var deletePermissions = ExtractPermissionsFromRecord(deleteRestrictions);
                                        yield return new AnyPermissionsCombiner(deletePermissions);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<IPermissionHandler> GetNavigationPropertyPropertyOperationPermisions(IList<ODataPathSegment> pathSegments, bool isTargetByKey, IEdmModel model, string method)
        {
            if (pathSegments.Count <= 1) yield break;

            var expectedPath = GetPathFromSegments(pathSegments);
            IEdmVocabularyAnnotatable root = (pathSegments[0] as EntitySetSegment).EntitySet as IEdmVocabularyAnnotatable ?? (pathSegments[0] as SingletonSegment).Singleton;

            var navRestrictions = root.VocabularyAnnotations(model).Where(a => a.Term.FullName() == ODataCapabilityRestrictionsConstants.NavigationRestrictions);
            foreach (var restriction in navRestrictions)
            {
                if (restriction.Value is IEdmRecordExpression record)
                {
                    var temp = record.FindProperty("RestrictedProperties");
                    if (temp?.Value is IEdmCollectionExpression restrictedProperties)
                    {
                        foreach (var item in restrictedProperties.Elements)
                        {
                            if (item is IEdmRecordExpression restrictedProperty)
                            {
                                var navigationProperty = restrictedProperty.FindProperty("NavigationProperty").Value as IEdmPathExpression;
                                if (navigationProperty?.Path == expectedPath)

                                {
                                    if (method == "GET")
                                    {
                                        var readRestrictions = restrictedProperty.FindProperty("ReadRestrictions")?.Value as IEdmRecordExpression;

                                        var readPermissions = ExtractPermissionsFromRecord(readRestrictions);
                                        yield return new AnyPermissionsCombiner(readPermissions);

                                        if (isTargetByKey)
                                        {
                                            var readByKeyRestrictions = readRestrictions.FindProperty("ReadByKeyRestrictions")?.Value as IEdmRecordExpression;
                                            var readByKeyPermissions = ExtractPermissionsFromRecord(readByKeyRestrictions);
                                            yield return new AnyPermissionsCombiner(readByKeyPermissions);
                                        }
                                    }
                                    else if (method == "POST" || method == "PATCH" || method == "PUT" || method == "MERGE" || method == "DELETE")
                                    {
                                        var updateRestrictions = restrictedProperty.FindProperty("UpdateRestrictions")?.Value as IEdmRecordExpression;
                                        var updatePermissions = ExtractPermissionsFromRecord(updateRestrictions);
                                        yield return new AnyPermissionsCombiner(updatePermissions);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        static bool IsNextSegmentKey(Microsoft.AspNet.OData.Routing.ODataPath path, int currentPos)
        {
            return IsNextSegmentOfType<KeySegment>(path, currentPos);
        }

        static bool IsNextSegmentOfType<T>(AspNet.OData.Routing.ODataPath path, int currentPos)
        {
            var maxPos = path.Segments.Count - 1;
            if (maxPos <= currentPos)
            {
                return false;
            }

            var nextSegment = path.Segments[currentPos + 1];

            if (nextSegment is T)
            {
                return true;
            }

            if (nextSegment is TypeSegment && maxPos >= currentPos + 2 && path.Segments[currentPos + 2] is T)
            {
                return true;
            }

            return false;
        }

        static string GetPathFromSegments(IList<ODataPathSegment> segments)
        {
            var pathParts = new List<string>(segments.Count);
            var i = 0;
            foreach (var path in segments)
            {
                i++;

                if (path is EntitySetSegment entitySetSegment)
                {
                    pathParts.Add(entitySetSegment.EntitySet.FullNavigationSourceName());
                }
                else if(path is SingletonSegment singletonSegment)
                {
                    pathParts.Add(singletonSegment.Singleton.FullNavigationSourceName());
                }
                else if(path is KeySegment && i < segments.Count) // don't add {key} to the end of the path
                {
                    pathParts.Add("{key}");
                }
                else if(path is NavigationPropertySegment navSegment)
                {
                    pathParts.Add(navSegment.NavigationProperty.Name);
                }
            }

            return string.Join('/', pathParts);
        }

        private static IEnumerable<PermissionData> GetSingletonPropertyOperationPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);
            if (method == "GET")
            {
                return GetReadPermissions(annotations);
            }
            else if (method == "PATCH" || method == "PUT" || method == "MERGE" || method == "POST" || method == "DELETE")
            {
                return GetUpdatePermissions(annotations);
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static IEnumerable<IPermissionHandler> GetEntityPropertyOperationPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);
            if (method == "GET")
            {
                return GetReadByKeyPermissions(annotations);
            }
            else if (method == "PATCH" || method == "PUT" || method == "MERGE" || method == "POST" || method == "DELETE")
            {
                return GetUpdatePermissions(annotations);
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static IEnumerable<PermissionData> GetNavigationSourceCrudPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);
            if (method == "GET")
            {
                return GetReadPermissions(annotations);
            }
            else if (method == "POST")
            {
                return GetInsertPermissions(annotations);
            }
            else if (method == "PATCH" || method == "PUT" || method == "MERGE")
            {
                return GetUpdatePermissions(annotations);
            }
            else if (method == "DELETE")
            {
                return GetDeletePermissions(annotations);
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static IEnumerable<IPermissionHandler> GetEntityCrudPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);

            if (method == "GET")
            {
                return GetReadByKeyPermissions(annotations);
            }
            else if (method == "PUT" || method == "POST" || method == "MERGE" || method == "PATCH")
            {
                return GetUpdatePermissions(annotations);
            }
            else if (method == "DELETE")
            {
                return GetDeletePermissions(annotations);
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static IEnumerable<PermissionData> GetReadPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            return GetPermissions(ODataCapabilityRestrictionsConstants.ReadRestrictions, annotations);
        }

        private static IEnumerable<IPermissionHandler> GetReadByKeyPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                if (annotation.Term.FullName() == ODataCapabilityRestrictionsConstants.ReadRestrictions && annotation.Value is IEdmRecordExpression record)
                {
                    var readPermissions = ExtractPermissionsFromAnnotation(annotation);
                    yield return new AnyPermissionsCombiner(readPermissions);

                    var readByKeyProperty = record.FindProperty("ReadByKeyRestrictions");
                    var readByKeyValue = readByKeyProperty?.Value as IEdmRecordExpression;
                    var permissionsProperty = readByKeyValue?.FindProperty("Permissions");
                    var  readByKeyPermissions = ExtractPermissionsFromProperty(permissionsProperty);
                    yield return new AnyPermissionsCombiner(readByKeyPermissions);
                }
            }
        }

        private static IEnumerable<PermissionData> GetInsertPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            return GetPermissions(ODataCapabilityRestrictionsConstants.InsertRestrictions, annotations);
        }

        private static IEnumerable<PermissionData> GetDeletePermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            return GetPermissions(ODataCapabilityRestrictionsConstants.DeleteRestrictions, annotations);
        }

        private static IEnumerable<PermissionData> GetUpdatePermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            return GetPermissions(ODataCapabilityRestrictionsConstants.UpdateRestrictions, annotations);
        }

        private static IEnumerable<PermissionData> GetOperationPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            return GetPermissions(ODataCapabilityRestrictionsConstants.OperationRestrictions, annotations);
        }

        private static IEnumerable<PermissionData> GetPermissions(string restrictionType, IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                if (annotation.Term.FullName() == restrictionType)
                {
                    return ExtractPermissionsFromAnnotation(annotation);
                }
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static IEnumerable<PermissionData> ExtractPermissionsFromAnnotation(IEdmVocabularyAnnotation annotation)
        {
            return ExtractPermissionsFromRecord(annotation.Value as IEdmRecordExpression);
        }

        private static IEnumerable<PermissionData> ExtractPermissionsFromRecord(IEdmRecordExpression record)
        {
            var permissionsProperty = record?.FindProperty("Permissions");
            return ExtractPermissionsFromProperty(permissionsProperty);
        }

        private static IEnumerable<PermissionData> ExtractPermissionsFromProperty(IEdmPropertyConstructor permissionsProperty)
        {
            if (permissionsProperty?.Value is IEdmCollectionExpression permissionsValue)
            {
                return permissionsValue.Elements.OfType<IEdmRecordExpression>().Select(p => GetPermissionData(p));
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static PermissionData GetPermissionData(IEdmRecordExpression permissionRecord)
        {
            var schemeProperty = permissionRecord.FindProperty("SchemeName")?.Value as IEdmStringConstantExpression;
            var scopesProperty = permissionRecord.FindProperty("Scopes")?.Value as IEdmCollectionExpression;

            var scopes = scopesProperty.Elements.Select(s => GetScopeData(s as IEdmRecordExpression));

            return new PermissionData() { SchemeName = schemeProperty.Value, Scopes = scopes.ToList() };
        }

        private static PermissionScopeData GetScopeData(IEdmRecordExpression scopeRecord)
        {
            var scopeProperty = scopeRecord.FindProperty("Scope")?.Value as IEdmStringConstantExpression;
            var restrictedPropertiesProperty = scopeRecord.FindProperty("RestrictedProperties")?.Value as IEdmStringConstantExpression;

            return new PermissionScopeData() { Scope = scopeProperty.Value, RestrictedProperties = restrictedPropertiesProperty.Value };
        }
    }
}
