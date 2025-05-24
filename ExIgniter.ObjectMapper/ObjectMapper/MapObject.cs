using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace ExIgniter.ObjectMapper.ObjectMapper
{
    public static class MapObject
{
    // Configuration Constants
    private const int MaxRecursionDepth = 32;

    private static readonly HashSet<string> BannedNamespaces = new HashSet<string>
    {
        "System.IO", "System.Diagnostics", "System.Reflection.Emit"
    };

    // Type Caches
    private static readonly Dictionary<Type, List<PropertyInfo>> PropertyCache =
        new Dictionary<Type, List<PropertyInfo>>();

    private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
    {
        typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(char),
        typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(short), typeof(ushort), typeof(DateTime),
        typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
    };

    #region Core Mapping Methods

    public static T Map<T>(this object source) where T : new()
    {
        return source.MapTo(new T());
    }

    public static T Map<T>(this object source, T destination) where T : new()
    {
        return source.MapTo(destination);
    }

    public static T Map<T>(this object source, Func<T, string[]> exclude) where T : new()
    {
        return source.MapTo(new T(), exclude);
    }

    public static T MapWithConfig<T>(this object source, MapConfig config) where T : new()
    {
        return source.MapTo(new T(), null, config);
    }

    public static T MapWithConfig<T>(this object source, T destination, MapConfig config) where T : new()
    {
        return source.MapTo(destination, null, config);
    }
    
    public static T FasterMap<T>(this object source, T destination, Func<T, string[]> excludePred) where T : new()
    {
        return source.MapTo(destination, excludePred);
    }

    public static T FasterMap<T>(this object source, T destination) where T : new()
    {
        return source.MapTo(destination);
    }

    public static T ComplexMap<T>(this object source, T destination) where T : new()
    {
        return source.MapTo(destination);
    }

    public static T ComplexMap<T>(this object source, T destination, Func<T, string[]> excludePred) where T : new()
    {
        return source.MapTo(destination, excludePred);
    }

    #endregion

    #region Implementation Methods

    private static T MapTo<T>(this object source, T destination, Func<T, string[]> exclude = null,
        MapConfig config = null, Dictionary<object, object> visited = null, int currentDepth = 0) where T : new()
    {
        // Validate input parameters
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (currentDepth > MaxRecursionDepth)
            throw new InvalidOperationException($"Maximum depth {MaxRecursionDepth} exceeded");

        // Check type security
        if (!IsAllowedType(source.GetType()) || !IsAllowedType(typeof(T)))
            throw new SecurityException("Type mapping not allowed");
        
        var ignoredProps = new HashSet<string>();
        
        if (exclude != null)
        {
            foreach (var prop in exclude(destination))
            {
                ignoredProps.Add(prop);
            }
        }
        
        if (config != null)
        {
            foreach (var prop in config.GetIgnoredProperties(typeof(T), destination))
            {
                ignoredProps.Add(prop);
            }
        }


        // Initialize visited dictionary if not provided
        visited ??= new Dictionary<object, object>(new ReferenceEqualityComparer());

        // Check for circular references
        if (visited.TryGetValue(source, out var existing))
            return (T)existing;

        // Add current mapping to visited
        visited.Add(source, destination);

         
        // Handle collections differently than regular objects
        if (source is IEnumerable sourceEnumerable && destination is IList destList)
        {
            HandleCollectionMapping(sourceEnumerable, destList, ignoredProps.ToArray(), visited, currentDepth, config);
        }
        else
        {
            HandleObjectMapping(source, destination, ignoredProps.ToArray(), visited, currentDepth, config);
        }

        return destination;
    }

   
    private static void HandleObjectMapping(
        object source,
        object destination,
        string[] ignoreProps,
        Dictionary<object, object> visited,
        int currentDepth,
        MapConfig config)
    {
        var srcProps = GetCachedProperties(source.GetType());
        var destProps = GetCachedProperties(destination.GetType());
        var propertyMap = GetSimilarityMap(srcProps, destProps);

        // First pass: Set all ignored properties to null
        foreach (var destProp in destProps)
        {
            if (ignoreProps.Contains(destProp.Name))
            {
                try
                {
                    // Special handling for dictionaries to ensure they're nulled
                    if (typeof(IDictionary).IsAssignableFrom(destProp.PropertyType))
                    {
                        destProp.SetValue(destination, null);
                    }
                    else
                    {
                        destProp.SetValue(destination, null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to set ignored property {destProp.Name} to null: {ex.Message}");
                }
            }
        }

        // Second pass: Map non-ignored properties
        foreach (var srcProp in srcProps)
        {
            if (!ShouldMapProperty(srcProp, ignoreProps, propertyMap, out var destProp)) continue;

            var value = GetPropertyValue(source, srcProp, destProp, config);
            if (value == null) continue;

            MapPropertyValue(value, srcProp, destProp, destination, visited, currentDepth, config);
        }
    }

    private static void HandleCollectionMapping(
        IEnumerable source,
        IList destination,
        string[] excludeProps,
        Dictionary<object, object> visited,
        int currentDepth,
        MapConfig config)
    {
        var elementType = GetCollectionElementType(destination.GetType());
        if (elementType == null || !IsAllowedType(elementType))
            throw new SecurityException("Collection element type not allowed");

        var destProps = GetCachedProperties(elementType);

        foreach (var item in source)
        {
            if (item == null || !IsAllowedType(item.GetType())) continue;

            var srcProps = GetCachedProperties(item.GetType());
            var instance = Activator.CreateInstance(elementType);
            var propertyMap = GetSimilarityMap(srcProps, destProps);

            MapProperties(
                source: item,
                destination: instance,
                srcProps: srcProps,
                destProps: destProps,
                propertyMap: propertyMap,
                excludedProperties: excludeProps,
                visited: visited,
                currentDepth: currentDepth + 1,
                config: config
            );

            destination.Add(instance);
        }
    }

    #endregion

    #region Helper Methods

 private static bool ShouldMapProperty(
    PropertyInfo srcProp,
    string[] excludeProps,
    Dictionary<string, PropertyInfo> propertyMap,
    out PropertyInfo destProp)
{
    destProp = null;
    
    // Check if property is in map
    if (!propertyMap.TryGetValue(srcProp.Name, out destProp))
        return false;

    // Check if property or any of its parent paths are excluded
    if (excludeProps != null)
    {
        var propertyPath = $"{destProp.DeclaringType?.Name}.{destProp.Name}";
        
        // Check exact match
        if (excludeProps.Contains(srcProp.Name) || excludeProps.Contains(propertyPath))
            return false;
            
        // Check nested paths (e.g., if "Parent" is excluded, "Parent.Child" should be too)
        foreach (var excludedPath in excludeProps)
        {
            if (propertyPath.StartsWith(excludedPath + ".") || 
                excludedPath.StartsWith(propertyPath + "."))
            {
                return false;
            }
        }
    }

    // Validate types are allowed
    return IsAllowedType(srcProp.PropertyType) && 
           IsAllowedType(destProp.PropertyType);
}

    private static object GetPropertyValue(
        object source,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        MapConfig config)
    {
        try
        {
            var mapAsAttribute = srcProp.GetCustomAttribute<MapAsAttribute>();
            if (mapAsAttribute?.ConverterType != null)
            {
                try
                {
                    var converter = Activator.CreateInstance(mapAsAttribute.ConverterType);
                    var convertMethod = mapAsAttribute.ConverterType.GetMethod("Convert");
                    if (convertMethod != null)
                    {
                        return convertMethod.Invoke(converter, new[] { srcProp.GetValue(source) });
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Custom converter failed for {srcProp.Name}: {ex.Message}"); 
                }
            }

            var value = srcProp.GetValue(source);
            if (value == null)
            {
                return config?._nullHandling == NullHandlingStrategy.ThrowException
                    ? null
                    : config?.HandleNull(destProp.PropertyType);
            }

            if (config != null && config.TryConvert(value, destProp.PropertyType, out var converted))
            {
                return converted;
            }

            return value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting property {srcProp.Name}: {ex.Message}");
            return null;
        }
    }

    private static void MapPropertyValue(
        object value,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        object destination,
        Dictionary<object, object> visited,
        int currentDepth,
        MapConfig config)
    {
        try
        {
            if (IsSimpleType(srcProp.PropertyType))
            {
                MapSimpleType(value, destProp, destination);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(srcProp.PropertyType) &&
                     !(value is string))
            {
                HandleCollectionPropertyMapping(
                    value: (IEnumerable)value,
                    destProp: destProp,
                    destination: destination,
                    visited: visited,
                    currentDepth: currentDepth,
                    config: config
                );
            }
            else
            {
                MapComplexType(value, destProp, destination, visited, currentDepth, config);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error mapping property {srcProp.Name}: {ex.Message}");
        }
    }

    private static void MapComplexType(
        object value,
        PropertyInfo destProp,
        object destination,
        Dictionary<object, object> visited,
        int currentDepth,
        MapConfig config)
    {
        try
        {
            var nestedInstance = Activator.CreateInstance(destProp.PropertyType);
        
            // Get the parent property path
            var parentPath = $"{destProp.DeclaringType?.Name}.{destProp.Name}";
            var nestedIgnoreProps = config?.GetIgnoredProperties(destProp.PropertyType, nestedInstance)
                .Select(p => $"{parentPath}.{p}") // Prefix with parent path
                .ToArray() ?? Array.Empty<string>();

            var mapped = value.MapTo(
                destination: nestedInstance,
                exclude: null,
                visited: visited,
                currentDepth: currentDepth + 1,
                config: config
            );
            destProp.SetValue(destination, mapped);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating complex type {destProp.PropertyType.Name}: {ex.Message}");
        }
    }

    private static bool IsSafeAssignment(Type sourceType, Type destType)
    {
        try
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
        catch
        {
            return false;
        }
    }

    private static void MapSimpleType(object value, PropertyInfo destProp, object destination)
    {
        try
        {
            if (value != null && IsSafeAssignment(value.GetType(), destProp.PropertyType))
            {
                var convertedValue = Convert.ChangeType(value, destProp.PropertyType);
                destProp.SetValue(destination, convertedValue);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error mapping simple type {value?.GetType().Name} to {destProp.PropertyType.Name}: {ex.Message}");
        }
    }

private static void HandleCollectionPropertyMapping(
    IEnumerable value,
    PropertyInfo destProp,
    object destination,
    Dictionary<object, object> visited,
    int currentDepth,
    MapConfig config)
{
    try
    {
        var targetType = destProp.PropertyType;

        // Handle dictionaries
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dictionary = CreateAndPopulateDictionary(
                sourceCollection: value,
                dictType: targetType,
                visited: visited,
                currentDepth: currentDepth,
                config: config
            );
            if (dictionary != null)
            {
                destProp.SetValue(destination, dictionary);
            }
            return;
        }

        // Handle other collections
        var elementType = GetCollectionElementType(targetType);
        if (elementType == null || !IsAllowedType(elementType)) return;

        var mappedItems = new List<object>();
        foreach (var item in value)
        {
            if (item == null || !IsAllowedType(item.GetType())) continue;

            object mappedItem;
            if (config != null && config.TryConvert(item, elementType, out var converted))
            {
                mappedItem = converted;
            }
            else if (IsSimpleType(elementType))
            {
                mappedItem = SafeConvert(item, elementType);
            }
            else
            {
                mappedItem = item.MapTo(
                    destination: Activator.CreateInstance(elementType),
                    exclude: null,
                    config: config,
                    visited: visited,
                    currentDepth: currentDepth + 1
                );
            }

            if (mappedItem != null)
            {
                mappedItems.Add(mappedItem);
            }
        }

        var targetCollection = CreateTargetCollection(targetType, elementType, mappedItems);
        if (targetCollection != null)
        {
            destProp.SetValue(destination, targetCollection);
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error handling collection property {destProp.Name}: {ex.Message}");
    }
}

    private static void MapProperties(
        object source,
        object destination,
        List<PropertyInfo> srcProps,
        List<PropertyInfo> destProps,
        Dictionary<string, PropertyInfo> propertyMap,
        string[] excludedProperties,
        Dictionary<object, object> visited,
        int currentDepth,
        MapConfig config)
    {
        foreach (var srcProp in srcProps)
        {
            try
            {
                // Skip if property is excluded or not in map
                if (excludedProperties.Contains(srcProp.Name))
                    continue;
                if (!propertyMap.TryGetValue(srcProp.Name, out var destProp)) continue;

                // Validate property types are allowed
                if (!IsAllowedType(srcProp.PropertyType) || !IsAllowedType(destProp.PropertyType))
                    continue;

                // Get source value with null handling
                var value = srcProp.GetValue(source);
                if (value == null)
                {
                    if (config?._nullHandling == NullHandlingStrategy.ThrowException)
                        continue;

                    value = config?.HandleNull(destProp.PropertyType);
                    if (value == null) continue;
                }

                // Handle different mapping scenarios
                if (IsSimpleType(srcProp.PropertyType))
                {
                    // Simple type conversion
                    if (IsSafeAssignment(srcProp.PropertyType, destProp.PropertyType))
                    {
                        var convertedValue = Convert.ChangeType(value, destProp.PropertyType);
                        destProp.SetValue(destination, convertedValue);
                    }
                }
                else if (typeof(IEnumerable).IsAssignableFrom(srcProp.PropertyType) &&
                         !(value is string))
                {
                    // Collection mapping
                    HandleCollectionPropertyMapping(
                        value: (IEnumerable)value,
                        destProp: destProp,
                        destination: destination,
                        visited: visited,
                        currentDepth: currentDepth,
                        config: config
                    );
                }
                else
                {
                    // Complex type mapping
                    var nestedInstance = Activator.CreateInstance(destProp.PropertyType);
                    var mapped = value.MapTo(
                        destination: nestedInstance,
                        exclude: null,
                        config: config,
                        visited: visited,
                        currentDepth: currentDepth + 1
                    );
                    destProp.SetValue(destination, mapped);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping property {srcProp.Name}: {ex.Message}");
            }
        }
    }

    #endregion

    #region Type System Helpers

    private static List<PropertyInfo> GetCachedProperties(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (type.IsDefined(typeof(UnmappableAttribute), inherit: true))
            throw new SecurityException($"Type {type.FullName} is marked as unmappable");

        if (!PropertyCache.TryGetValue(type, out var props))
        {
            props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetSetMethod() != null)
                .Where(p => !p.IsDefined(typeof(NoMapAttribute)))
                .ToList();
            PropertyCache[type] = props;
        }

        return props;
    }

    private static bool IsSimpleType(Type type)
    {
        if (type == null) return false;
        if (BannedNamespaces.Any(ns => type.FullName?.StartsWith(ns) ?? false))
            return false;

        return type.IsPrimitive ||
               PrimitiveTypes.Contains(type) ||
               type.IsEnum ||
               (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)));
    }

    private static bool IsAllowedType(Type type)
    {
        if (type == null) return false;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            return true;

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

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType == null) return null;

        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
        {
            var genericType = collectionType.GetGenericTypeDefinition();
            return genericType == typeof(Dictionary<,>)
                ? typeof(object)
                : collectionType.GetGenericArguments()[0];
        }

        return typeof(object);
    }

    #endregion

    #region Collection Creation

    private static IDictionary CreateAndPopulateDictionary(
        IEnumerable sourceCollection,
        Type dictType,
        Dictionary<object, object> visited,
        int currentDepth,
        MapConfig config)
    {
        if (currentDepth > MaxRecursionDepth)
        {
            throw new InvalidOperationException($"Maximum recursion depth {MaxRecursionDepth} exceeded");
        }

        // Validate dictionary type
        if (!dictType.IsGenericType || dictType.GetGenericArguments().Length != 2)
        {
            throw new ArgumentException("Invalid dictionary type - must be generic with two type parameters", nameof(dictType));
        }

        // Get key and value types
        var keyType = dictType.GetGenericArguments()[0];
        var valueType = dictType.GetGenericArguments()[1];
        
        // Security check for types
        if (!IsAllowedType(keyType))
            throw new SecurityException($"Key type {keyType.FullName} is not allowed");
        if (!IsAllowedType(valueType))
            throw new SecurityException($"Value type {valueType.FullName} is not allowed");

        // Create dictionary instance
        var dictionary = (IDictionary)Activator.CreateInstance(dictType);
        if (dictionary == null)
        {
            throw new InvalidOperationException($"Failed to create dictionary instance of type {dictType.FullName}");
        }

        // Process each item in source collection
        foreach (var item in sourceCollection)
        {
            try
            {
                // Skip null items or disallowed types
                if (item == null || !IsAllowedType(item.GetType()))
                {
                    continue;
                }

                // Get Key and Value properties
                var key = GetDictionaryKey(item);
                var value = GetDictionaryValue(item);
                
                if (key == null)
                {
                    Debug.WriteLine("Skipping dictionary item with null key");
                    continue;
                }

                // Convert key
                var convertedKey = SafeConvert(key, keyType);
                if (convertedKey == null)
                {
                    Debug.WriteLine($"Failed to convert key {key} to type {keyType}");
                    continue;
                }

                // Convert value
                object convertedValue = null;
                if (value != null)
                {
                    if (IsSimpleType(valueType))
                    {
                        convertedValue = SafeConvert(value, valueType);
                    }
                    else
                    {
                        convertedValue = value.MapTo(
                            destination: Activator.CreateInstance(valueType),
                            exclude: null,
                            config: config,
                            visited: visited,
                            currentDepth: currentDepth + 1
                        );
                    }
                }
                
                dictionary.Add(convertedKey, convertedValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing dictionary item: {ex.Message}");
                if (config?._nullHandling == NullHandlingStrategy.ThrowException)
                {
                    throw;
                }
            }
        }

        return dictionary;
    }

    private static object SafeConvert(object value, Type targetType)
    {
        try
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    private static object GetDictionaryKey(object item)
    {
        if (item == null) return null;

        // Handle KeyValuePair
        if (item.GetType().IsGenericType && 
            item.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            return item.GetType().GetProperty("Key")?.GetValue(item);
        }

        // Handle dynamic objects
        return item.GetType().GetProperty("Key", 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(item);
    }

    private static object GetDictionaryValue(object item)
    {
        if (item == null) return null;

        // Handle KeyValuePair
        if (item.GetType().IsGenericType && 
            item.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            return item.GetType().GetProperty("Value")?.GetValue(item);
        }

        // Handle dynamic objects
        return item.GetType().GetProperty("Value", 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(item);
    }

    private static object CreateTargetCollection(Type targetType, Type elementType, List<object> items)
    {
        try
        {
            if (targetType == null || elementType == null) return null;

            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(elementType, items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    array.SetValue(items[i], i);
                }
                return array;
            }

            if (targetType.IsGenericType)
            {
                var genericType = targetType.GetGenericTypeDefinition();
                var concreteType = genericType.MakeGenericType(elementType);

                if (genericType == typeof(List<>) || genericType == typeof(IList<>))
                {
                    var list = (IList)Activator.CreateInstance(concreteType);
                    foreach (var item in items) 
                    {
                        if (item != null) list.Add(item);
                    }
                    return list;
                }
                else if (genericType == typeof(HashSet<>))
                {
                    var hashSet = (IEnumerable)Activator.CreateInstance(concreteType);
                    var addMethod = concreteType.GetMethod("Add");
                    foreach (var item in items)
                    {
                        if (item != null) addMethod.Invoke(hashSet, new[] { item });
                    }
                    return hashSet;
                }
                else if (genericType == typeof(Queue<>) || genericType == typeof(Stack<>))
                {
                    var tempList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    foreach (var item in items)
                    {
                        if (item != null) tempList.Add(item);
                    }
                    return Activator.CreateInstance(concreteType, tempList);
                }
            }

            if (typeof(IList).IsAssignableFrom(targetType))
            {
                var list = (IList)Activator.CreateInstance(targetType);
                foreach (var item in items)
                {
                    if (item != null) list.Add(item);
                }
                return list;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating collection: {ex.Message}");
        }

        return null;
    }

    #endregion
}

// Helper class for reference equality comparison
    public class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    #region Attributes and Configuration

    [AttributeUsage(AttributeTargets.Property)]
    public class NoMapAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UnmappableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class MapAsAttribute : Attribute
    {
        public string TargetPropertyName { get; }
        public Type ConverterType { get; }

        public MapAsAttribute(string targetPropertyName) => TargetPropertyName = targetPropertyName;
        public MapAsAttribute(Type converterType) => ConverterType = converterType;
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public class MapConstructorAttribute : Attribute
    {
    }

public class MapConfig
    {
        private readonly Dictionary<Type, Delegate> _typeConverters = new Dictionary<Type, Delegate>();
        private readonly Dictionary<Type, HashSet<string>> _ignoredProperties = new Dictionary<Type, HashSet<string>>();
        internal NullHandlingStrategy _nullHandling = NullHandlingStrategy.ThrowException;
        private readonly Dictionary<Type, Func<object, IEnumerable<string>>> _conditionalIgnores = 
            new Dictionary<Type, Func<object, IEnumerable<string>>>();
        
        public MapConfig MapAs<TSource, TDest>(Func<TSource, TDest> converter)
        {
            _typeConverters[typeof(TSource)] = converter;
            return this;
        }
        public MapConfig Ignore<T>(Func<T, IEnumerable<string>> condition)
        {
            _conditionalIgnores[typeof(T)] = target => condition((T)target);
            return this;
        }

    
        public IEnumerable<string> GetIgnoredProperties(Type type, object instance)
        {
            // First check for conditional ignores
            if (_conditionalIgnores.TryGetValue(type, out var condition))
            {
                return condition(instance) ?? Enumerable.Empty<string>();
            }
        
            // Fall back to static ignores if no condition exists
            if (_ignoredProperties.TryGetValue(type, out var ignoredSet))
            {
                return ignoredSet;
            }
        
            return Enumerable.Empty<string>();
        }
        
        public MapConfig Ignore<T>(Expression<Func<T, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                if (expression.Body is UnaryExpression unary && 
                    unary.Operand is MemberExpression operand)
                {
                    memberExpression = operand;
                }
                else
                {
                    throw new ArgumentException("Expression must be a member expression");
                }
            }

            // Build the full property path
            var path = new List<string>();
            var current = memberExpression;
            while (current != null)
            {
                path.Insert(0, current.Member.Name);
                current = current.Expression as MemberExpression;
            }

            var fullPath = string.Join(".", path);
    
            // Initialize the HashSet if it doesn't exist
            if (!_ignoredProperties.TryGetValue(typeof(T), out var ignoredSet))
            {
                ignoredSet = new HashSet<string>();
                _ignoredProperties[typeof(T)] = ignoredSet;
            }
    
            // Add the path to the set
            ignoredSet.Add(fullPath);
    
            return this;
        }
        internal bool ShouldIgnore(Type targetType, string propertyName, object targetInstance)
        {
            // Check conditional ignores first
            if (_conditionalIgnores.TryGetValue(targetType, out var condition))
            {
                var ignoredProps = condition(targetInstance);
                if (ignoredProps != null)
                {
                    // Check both direct match and nested paths
                    foreach (var ignoredProp in ignoredProps)
                    {
                        if (propertyName == ignoredProp || 
                            propertyName.StartsWith(ignoredProp + ".") ||
                            ignoredProp.StartsWith(propertyName + "."))
                        {
                            return true;
                        }
                    }
                }
            }

            // Check static ignores with path matching
            if (_ignoredProperties.TryGetValue(targetType, out var props))
            {
                foreach (var prop in props)
                {
                    if (propertyName == prop || 
                        propertyName.StartsWith(prop + ".") ||
                        prop.StartsWith(propertyName + "."))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        public MapConfig WithNullHandling(NullHandlingStrategy strategy)
        {
            _nullHandling = strategy;
            return this;
        }

        internal bool ShouldIgnore(Type targetType, string propertyName)
        {
            // Simplified version without instance for cases where we don't have the target instance
            if (_ignoredProperties.TryGetValue(targetType, out var props))
            {
                foreach (var prop in props)
                {
                    if (propertyName == prop || 
                        propertyName.StartsWith(prop + ".") ||
                        prop.StartsWith(propertyName + "."))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool TryConvert(object value, Type targetType, out object result)
        {
            if (_typeConverters.TryGetValue(value.GetType(), out var converter))
            {
                result = ((Delegate)converter).DynamicInvoke(value);
                return true;
            }

            result = null;
            return false;
        }

        internal object HandleNull(Type targetType) => _nullHandling switch
        {
            NullHandlingStrategy.ThrowException => throw new ArgumentNullException(),
            NullHandlingStrategy.UseDefaultValue => targetType.IsValueType
                ? Activator.CreateInstance(targetType)
                : null,
            _ => null
        };
    }

    public enum NullHandlingStrategy
    {
        ThrowException,
        UseDefaultValue,
        UseNull
    }

    #endregion
}