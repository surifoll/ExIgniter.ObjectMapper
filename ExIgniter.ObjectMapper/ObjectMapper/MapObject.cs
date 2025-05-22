using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security;
using ExIgniter.ObjectMapper.Objects;

namespace ExIgniter.ObjectMapper.ObjectMapper
{
   public static class MapObject
    {
        // Security Constants
        private const int MaxRecursionDepth = 32;
        private static readonly HashSet<string> BannedNamespaces = new HashSet<string>
        {
            "System.IO", "System.Diagnostics", "System.Reflection.Emit"
        };

        // Cache with security constraints
        private static readonly Dictionary<Type, List<PropertyInfo>> PropertyCache = 
            new Dictionary<Type, List<PropertyInfo>>();

        [AttributeUsage(AttributeTargets.Property)]
        public class NoMapAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Class)]
        public class UnmappableAttribute : Attribute { }

        private static List<PropertyInfo> GetCachedProperties(Type type)
        {
            if (type.IsDefined(typeof(UnmappableAttribute), inherit: true))
                throw new SecurityException($"Type {type.FullName} is marked as unmappable");

            if (!PropertyCache.TryGetValue(type, out var props))
            {
                props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetSetMethod() != null) // Only settable properties
                    .Where(p => !p.IsDefined(typeof(NoMapAttribute))) // Filter NoMap properties
                    .ToList();
                
                PropertyCache[type] = props;
            }
            return props;
        }

        private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
        {
            typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(char),
            typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(short), typeof(ushort), typeof(DateTime),
            typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
        };

        private static bool IsSimpleType(Type type)
        {
            if (type == null) return false;
            if (BannedNamespaces.Any(ns => type.FullName?.StartsWith(ns) ?? false))
                return false;

            return type.IsPrimitive || 
                   PrimitiveTypes.Contains(type) || 
                   type.IsEnum ||
                   Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type));
        }

        private static bool IsAllowedType(Type type)
        {
            if (type == null) return false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                return true;

            // Block dangerous types
            var dangerousTypes = new[] { typeof(Delegate), typeof(MulticastDelegate) };
            if (dangerousTypes.Any(t => t.IsAssignableFrom(type)))
                return false;

            return !BannedNamespaces.Any(ns => type.FullName?.StartsWith(ns) ?? false);
        }

        private static Dictionary<string, PropertyInfo> GetSimilarityMap(
            List<PropertyInfo> source, 
            List<PropertyInfo> destination)
        {
            var map = new Dictionary<string, PropertyInfo>();
            foreach (var src in source)
            {
                if (!IsAllowedType(src.PropertyType)) continue;

                var match = Util.CalculateSimilarity(src.Name, destination).FirstOrDefault();
                if (match != null)
                {
                    var matchedProp = destination.FirstOrDefault(d => d.Name == match.DestPropertName);
                    if (matchedProp != null && IsAllowedType(matchedProp.PropertyType))
                        map[src.Name] = matchedProp;
                }
            }
            return map;
        }

        private static T MapTo<T>(this object source, T destination, Func<T, string[]> exclude = null) where T : new()
        {
            return source.MapTo(destination, exclude, new Dictionary<object, object>(), currentDepth: 0);
        }

        private static T MapTo<T>(
            this object source, 
            T destination, 
            Func<T, string[]> exclude,
            Dictionary<object, object> visited,
            int currentDepth) where T : new()
        {
            if (currentDepth > MaxRecursionDepth)
                throw new InvalidOperationException($"Maximum recursion depth {MaxRecursionDepth} exceeded");

            if (source == null)
                return default;

            // Security: Type must be allowed
            if (!IsAllowedType(source.GetType()) || !IsAllowedType(typeof(T)))
                throw new SecurityException("Type mapping not allowed");

            // Circular reference check
            if (visited.TryGetValue(source, out var existing))
                return (T)existing;

            visited.Add(source, destination);

            var excludeProps = exclude?.Invoke(destination) ?? Array.Empty<string>();

            if (source is IEnumerable sourceEnumerable && destination is IList destList)
            {
                var elementType = destination.GetType().GetGenericArguments().FirstOrDefault();
                if (!IsAllowedType(elementType))
                    throw new SecurityException("Collection element type not allowed");

                var destProps = GetCachedProperties(elementType);

                foreach (var item in sourceEnumerable)
                {
                    if (!IsAllowedType(item?.GetType())) continue;

                    if (item != null)
                    {
                        var srcProps = GetCachedProperties(item.GetType());
                        var similarityMap = GetSimilarityMap(srcProps, destProps);
                        var instance = Activator.CreateInstance(elementType);

                        MapProperties(item, instance, srcProps, destProps, similarityMap, 
                            excludeProps, visited, currentDepth + 1);
                        destList.Add(instance);
                    }
                }
                return destination;
            }
            else
            {
                var srcProps = GetCachedProperties(source.GetType());
                var destProps = GetCachedProperties(destination.GetType());
                var similarityMap = GetSimilarityMap(srcProps, destProps);

                MapProperties(source, destination, srcProps, destProps, similarityMap, 
                            excludeProps, visited, currentDepth + 1);
                return destination;
            }
        }

        private static void MapProperties(
            object source,
            object destination,
            List<PropertyInfo> srcProps,
            List<PropertyInfo> destProps,
            Dictionary<string, PropertyInfo> map,
            string[] exclude,
            Dictionary<object, object> visited,
            int currentDepth)
        {
            var destDescriptor = TypeDescriptor.GetProperties(destination);

            foreach (var srcProp in srcProps)
            {
                if (exclude.Contains(srcProp.Name) || 
                    !map.TryGetValue(srcProp.Name, out var destProp) ||
                    !IsAllowedType(srcProp.PropertyType) ||
                    !IsAllowedType(destProp.PropertyType))
                    continue;

                var value = srcProp.GetValue(source);
                if (value == null)
                    continue;

                if (IsSimpleType(srcProp.PropertyType))
                {
                    // Validate type compatibility
                    if (IsSafeAssignment(srcProp.PropertyType, destProp.PropertyType))
                    {
                        destProp.SetValue(destination, value);
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(srcProp.PropertyType) &&
                         srcProp.PropertyType != typeof(string))
                {
                    HandleCollectionMapping(source, destination, srcProp, destProp, 
                                          destDescriptor, visited, currentDepth);
                }
                else
                {
                    var nestedInstance = Activator.CreateInstance(destProp.PropertyType);
                    var mapped = value.MapTo(nestedInstance, null, visited, currentDepth + 1);
                    destDescriptor[destProp.Name].SetValue(destination, mapped);
                }
            }
        }

        private static bool IsSafeAssignment(Type sourceType, Type destType)
        {
            // Handle nullable types
            sourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            destType = Nullable.GetUnderlyingType(destType) ?? destType;

            // Primitive type conversions
            if (IsSimpleType(sourceType) && IsSimpleType(destType))
                return true;

            // Custom objects must be exact type or inheritance
            return destType.IsAssignableFrom(sourceType);
        }

        private static void HandleCollectionMapping(
            object source,
            object destination,
            PropertyInfo srcProp,
            PropertyInfo destProp,
            PropertyDescriptorCollection destDescriptor,
            Dictionary<object, object> visited,
            int currentDepth)
        {
            var sourceCollection = (IEnumerable)srcProp.GetValue(source);
            var targetType = destProp.PropertyType;

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                HandleDictionaryMapping(sourceCollection, destProp, destination, 
                                      destDescriptor, visited, currentDepth);
                return;
            }

            var elementType = GetCollectionElementType(targetType);
            if (elementType == null || !IsAllowedType(elementType)) return;

            var mappedItems = new List<object>();
            foreach (var item in sourceCollection)
            {
                if (item == null || !IsAllowedType(item.GetType())) continue;

                var mappedItem = IsSimpleType(elementType)
                    ? item
                    : item.MapTo(Activator.CreateInstance(elementType), null, 
                               visited, currentDepth + 1);
                mappedItems.Add(mappedItem);
            }

            object targetCollection = CreateTargetCollection(targetType, elementType, mappedItems);
            if (targetCollection != null)
            {
                destDescriptor[destProp.Name].SetValue(destination, targetCollection);
            }
        }
        
       private static void HandleDictionaryMapping(
    IEnumerable sourceCollection,
    PropertyInfo destProp,
    object destination,
    PropertyDescriptorCollection destDescriptor,
    Dictionary<object, object> visited,
    int currentDepth)
{
    var dictType = destProp.PropertyType;
    
    // Security: Verify dictionary type is allowed
    if (!dictType.IsGenericType || dictType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
        throw new SecurityException("Invalid dictionary type");

    var keyType = dictType.GetGenericArguments()[0];
    var valueType = dictType.GetGenericArguments()[1];
    
    // Security: Validate key and value types
    if (!IsAllowedType(keyType) || !IsAllowedType(valueType))
        throw new SecurityException("Dictionary contains prohibited types");

    var dictionary = (IDictionary)Activator.CreateInstance(dictType);

    foreach (var item in sourceCollection)
    {
        if (item == null) continue;
        
        // Security: Verify item type
        if (!IsAllowedType(item.GetType())) continue;

        var keyProperty = item.GetType().GetProperty("Key");
        var valueProperty = item.GetType().GetProperty("Value");

        if (keyProperty == null || valueProperty == null) continue;

        var key = keyProperty.GetValue(item);
        var value = valueProperty.GetValue(item);

        // Security: Validate key and value
        if (key == null || !IsAllowedType(key.GetType())) continue;
        if (value != null && !IsAllowedType(value.GetType())) continue;

        // Map the value if it's a complex type
        if (!IsSimpleType(valueType) && value != null)
        {
            value = value.MapTo(Activator.CreateInstance(valueType), null, visited, currentDepth + 1);
        }

        try
        {
            dictionary.Add(key, value);
        }
        catch (Exception ex)
        {
            // Log dictionary addition error if needed
            continue;
        }
    }

    destDescriptor[destProp.Name].SetValue(destination, dictionary);
}
        private static Type GetCollectionElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
            {
                var genericType = collectionType.GetGenericTypeDefinition();
                if (genericType == typeof(Dictionary<,>))
                    return typeof(object); // Dictionaries are handled separately
                else
                    return collectionType.GetGenericArguments().FirstOrDefault();
            }

            return typeof(object);
        }

        private static object CreateTargetCollection(Type targetType, Type elementType, List<object> items)
        {
            try
            {
                if (targetType.IsArray)
                {
                    var array = Array.CreateInstance(elementType, items.Count);
                    for (int i = 0; i < items.Count; i++)
                        array.SetValue(items[i], i);
                    return array;
                }

                if (targetType.IsGenericType)
                {
                    var genericType = targetType.GetGenericTypeDefinition();

                    if (genericType == typeof(List<>) || genericType == typeof(IList<>))
                    {
                        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                        foreach (var item in items) list.Add(item);
                        return list;
                    }
                    else if (genericType == typeof(HashSet<>))
                    {
                        // Create a new HashSet and add all items
                        // Create the concrete HashSet<T> type
                        var concreteHashSetType = typeof(HashSet<>).MakeGenericType(elementType);
                        var hashSet = Activator.CreateInstance(concreteHashSetType);

                        // Get the Add method via reflection
                        var addMethod = concreteHashSetType.GetMethod("Add");

                        // Add each item to the HashSet
                        foreach (var item in items)
                        {
                            addMethod.Invoke(hashSet, new[] { item });
                        }

                        return hashSet;
                    }
                    else if (genericType == typeof(Queue<>))
                    {
                        var queue = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                        foreach (var item in items) queue.Add(item);
                        return Activator.CreateInstance(targetType, queue);
                    }
                    else if (genericType == typeof(Stack<>))
                    {
                        var stack = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                        foreach (var item in items) stack.Add(item);
                        return Activator.CreateInstance(targetType, stack);
                    }
                }

                // Fallback for non-generic collections
                if (typeof(IList).IsAssignableFrom(targetType))
                {
                    var list = (IList)Activator.CreateInstance(targetType);
                    foreach (var item in items) list.Add(item);
                    return list;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating collection: {ex.Message}");
                return null;
            }

            return null;
        }
        public static T Map<T>(this object source) where T : new()
        {
            return source.MapTo(new T());
        }
        
        public static T Map<T>(this object source, T data) where T : new()
        {
            return source.MapTo(new T());
        }

        public static T Map<T>(this object source, Func<T, string[]> exclude) where T : new()
        {
            return source.MapTo(new T(), exclude);
        }

        public static T FasterMap<T>(this object source, T destination) where T : new()
        {
            return source.MapTo(destination);
        }

        public static T FasterMap<T>(this object source, T destination, Func<T, string[]> excludePred) where T : new()
        {
            return source.MapTo(destination, excludePred);
        }

        public static T ComplexMap<T>(this object source, T destination) where T : new()
        {
            return source.MapTo(destination);
        }

        public static T ComplexMap<T>(this object source, T destination, Func<T, string[]> excludePred) where T : new()
        {
            return source.MapTo(destination, excludePred);
        }
    }
}
