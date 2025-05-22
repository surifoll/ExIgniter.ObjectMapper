using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ExIgniter.ObjectMapper.Objects;

namespace ExIgniter.ObjectMapper.ObjectMapper
{
    public static class MapObject
    {
        private static readonly Dictionary<Type, List<PropertyInfo>> PropertyCache =
            new Dictionary<Type, List<PropertyInfo>>();

        private static List<PropertyInfo> GetCachedProperties(Type type)
        {
            if (!PropertyCache.TryGetValue(type, out var props))
            {
                props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                PropertyCache[type] = props;
            }

            return props;
        }

        // private static bool IsSimpleType(Type type)
        // {
        //     return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
        // }

        private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
        {
            typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(char),
            typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(short), typeof(ushort), typeof(DateTime),
            typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
        };

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || PrimitiveTypes.Contains(type) || type.IsEnum;
        }


        private static Dictionary<string, PropertyInfo> GetSimilarityMap(List<PropertyInfo> source,
            List<PropertyInfo> destination)
        {
            var map = new Dictionary<string, PropertyInfo>();
            foreach (var src in source)
            {
                var match = Util.CalculateSimilarity(src.Name, destination).FirstOrDefault();
                if (match != null)
                {
                    var matchedProp = destination.FirstOrDefault(d => d.Name == match.DestPropertName);
                    if (matchedProp != null)
                        map[src.Name] = matchedProp;
                }
            }

            return map;
        }

        public static T MapTo<T>(this object source, T destination, Func<T, string[]> exclude = null) where T : new()
        {
            return source.MapTo(destination, exclude, new Dictionary<object, object>());
        }

        private static T MapTo<T>(this object source, T destination, Func<T, string[]> exclude,
            Dictionary<object, object> visited) where T : new()
        {
            if (source == null)
                return default;

            // Check for circular references
            if (visited.TryGetValue(source, out var existing))
                return (T)existing;

            visited.Add(source, destination);

            var excludeProps = exclude?.Invoke(destination) ?? Array.Empty<string>();

            if (source is IEnumerable sourceEnumerable && destination is IList destList)
            {
                var elementType = destination.GetType().GetGenericArguments().FirstOrDefault();
                var destProps = GetCachedProperties(elementType);

                foreach (var item in sourceEnumerable)
                {
                    var srcProps = GetCachedProperties(item.GetType());
                    var similarityMap = GetSimilarityMap(srcProps, destProps);
                    var instance = Activator.CreateInstance(elementType);

                    MapProperties(item, instance, srcProps, destProps, similarityMap, excludeProps, visited);
                    destList.Add(instance);
                }

                return destination;
            }
            else
            {
                var srcProps = GetCachedProperties(source.GetType());
                var destProps = GetCachedProperties(destination.GetType());
                var similarityMap = GetSimilarityMap(srcProps, destProps);

                MapProperties(source, destination, srcProps, destProps, similarityMap, excludeProps, visited);
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
            Dictionary<object, object> visited)
        {
            var destDescriptor = TypeDescriptor.GetProperties(destination);

            foreach (var srcProp in srcProps)
            {
                if (exclude.Contains(srcProp.Name) || !map.TryGetValue(srcProp.Name, out var destProp))
                    continue;

                var value = srcProp.GetValue(source);
                if (value == null)
                    continue;

                if (IsSimpleType(srcProp.PropertyType))
                {
                    destProp.SetValue(destination, value);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(srcProp.PropertyType) &&
                         srcProp.PropertyType != typeof(string))
                {
                    HandleCollectionMapping(source, destination, srcProp, destProp, destDescriptor, visited);
                }
                else
                {
                    var nestedInstance = Activator.CreateInstance(destProp.PropertyType);
                    var mapped = value.MapTo(nestedInstance, null, visited);
                    destDescriptor[destProp.Name].SetValue(destination, mapped);
                }
            }
        }

        private static void HandleCollectionMapping(
            object source,
            object destination,
            PropertyInfo srcProp,
            PropertyInfo destProp,
            PropertyDescriptorCollection destDescriptor,
            Dictionary<object, object> visited)
        {
            var sourceCollection = (IEnumerable)srcProp.GetValue(source);
            var targetType = destProp.PropertyType;

            // Special handling for dictionaries
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                HandleDictionaryMapping(sourceCollection, destProp, destination, destDescriptor, visited);
                return;
            }

            var elementType = GetCollectionElementType(targetType);
            if (elementType == null) return;

            var mappedItems = new List<object>();
            foreach (var item in sourceCollection)
            {
                var mappedItem = IsSimpleType(elementType)
                    ? item
                    : item.MapTo(Activator.CreateInstance(elementType), null, visited);
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
            Dictionary<object, object> visited)
        {
            var dictType = destProp.PropertyType;
            var keyType = dictType.GetGenericArguments()[0];
            var valueType = dictType.GetGenericArguments()[1];

            var dictionary = (IDictionary)Activator.CreateInstance(dictType);

            foreach (var item in sourceCollection)
            {
                var keyProperty = item.GetType().GetProperty("Key");
                var valueProperty = item.GetType().GetProperty("Value");

                if (keyProperty == null || valueProperty == null) continue;

                var key = keyProperty.GetValue(item);
                var value = valueProperty.GetValue(item);

                // Map the value if it's a complex type
                if (!IsSimpleType(valueType))
                {
                    value = value.MapTo(Activator.CreateInstance(valueType), null, visited);
                }

                dictionary.Add(key, value);
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
