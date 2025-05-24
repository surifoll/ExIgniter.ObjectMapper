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
using ExIgniter.ObjectMapper.Types;

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
    public static T MapTo<T>(this object source, MapConfig config) where T : new()
    {
        return source.MapTo(new T(), null, config);
    }
    public static T MapTo<T>(this object source) where T : new()
    {
        return source.MapTo(new T());
    }
    public static Dictionary<string, object> ToDictionary(this object obj)
    {
        return obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p.GetValue(obj));
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
        
        if (config != null && config.TryConvert(source, typeof(T), out var convertedObj, destination))
        {
            return (T)convertedObj;
        }
        // Handle anonymous object to Dictionary<string, string> or Dictionary<string, object>
        if (typeof(T).IsGenericType && typeof(IDictionary).IsAssignableFrom(typeof(T)))
        {
            var dictType = typeof(T);
            var keyType = dictType.GetGenericArguments()[0];
            var valueType = dictType.GetGenericArguments()[1];

            if (keyType == typeof(string))
            {
                var result = (IDictionary)Activator.CreateInstance(dictType);
                foreach (var prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var key = prop.Name;
                    var value = prop.GetValue(source);

                    if (config != null && config.TryConvert(value, valueType, out var converted))
                    {
                        result[key] = converted;
                    }
                    else if (IsSimpleType(valueType))
                    {
                        if (value is IConvertible)
                        {
                            result[key] = Convert.ChangeType(value, valueType);
                        }
                        else
                        {
                            // Fallback: string representation or skip
                            result[key] = value?.ToString();
                        }
                    }
                    else if (value != null)
                    {
                        result[key] = value.MapTo(Activator.CreateInstance(valueType), null, config, visited, currentDepth + 1);
                    }


                    
                }

                return (T)result;
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
    
    // Special handling for dictionary destinations
    if (destination is IDictionary<string, object> dict)
    {
        foreach (var srcProp in srcProps)
        {
            // Skip if property is excluded
            if (excludedProperties != null && excludedProperties.Contains(srcProp.Name))
                continue;

            try 
            {
                var value = srcProp.GetValue(source);
                if (value == null)
                {
                    if (config?._nullHandling == NullHandlingStrategy.ThrowException)
                        continue;

                    value = config?.HandleNull(srcProp.PropertyType);
                    if (value == null) continue;
                }

                // Handle collections differently
                if (typeof(IEnumerable).IsAssignableFrom(srcProp.PropertyType) && !(value is string))
                {
                    var elementType = GetCollectionElementType(srcProp.PropertyType);
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(object)));
                    
                    foreach (var item in (IEnumerable)value)
                    {
                        if (item == null) continue;
                        
                        if (IsSimpleType(item.GetType()))
                        {
                            list.Add(item);
                        }
                        else
                        {
                            var nestedDict = new Dictionary<string, object>();
                            item.MapTo(nestedDict, exclude: null, config: config, 
                                      visited: visited, currentDepth: currentDepth + 1);
                            list.Add(nestedDict);
                        }
                    }
                    
                    dict[srcProp.Name] = list;
                }
                else if (IsSimpleType(srcProp.PropertyType))
                {
                    dict[srcProp.Name] = value;
                }
                else
                {
                    // Handle complex objects
                    var nestedDict = new Dictionary<string, object>();
                    value.MapTo(nestedDict, exclude: null, config: config, 
                              visited: visited, currentDepth: currentDepth + 1);
                    dict[srcProp.Name] = nestedDict;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping property {srcProp.Name} to dictionary: {ex.Message}");
                // Continue with next property
            }
        }
        return;
    }

    // Original property-to-property mapping for non-dictionary destinations
    foreach (var srcProp in srcProps)
    {
        try
        {
            // Skip if property is excluded or not in map
            if (excludedProperties != null && excludedProperties.Contains(srcProp.Name))
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
                    
                    if (config != null && config.TryConvert(value, valueType, out var converted))
                    {
                        convertedValue = converted;
                    }
                    else if (!IsSimpleType(valueType))
                    {
                        convertedValue = value.MapTo(
                            destination: Activator.CreateInstance(valueType),
                            exclude: null,
                            config: config,
                            visited: visited,
                            currentDepth: currentDepth + 1
                        );
                    }
                    else
                    {
                        convertedValue = SafeConvert(value, valueType);
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

   
    
}