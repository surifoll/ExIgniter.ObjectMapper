using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace ExIgniter.ObjectMapper.Types
{
    public class MapConfig
    {
        private readonly Dictionary<Type, Delegate> _typeConverters = new Dictionary<Type, Delegate>();
        private readonly Dictionary<Type, HashSet<string>> _ignoredProperties = new Dictionary<Type, HashSet<string>>();
        internal NullHandlingStrategy _nullHandling = NullHandlingStrategy.ThrowException;
        private readonly Dictionary<Type, Func<object, IEnumerable<string>>> _conditionalIgnores = 
            new Dictionary<Type, Func<object, IEnumerable<string>>>();
        private readonly Dictionary<Type, Func<object, object, object>> _advancedTypeConverters = 
            new Dictionary<Type, Func<object, object, object>>();
        
        private readonly Dictionary<Type, Func<object, object>> _simpleConverters = new Dictionary<Type, Func<object, object>>();
        private readonly Dictionary<Type, Func<object, object, object>> _advancedConverters = new Dictionary<Type, Func<object, object, object>>();
        
        
        public MapConfig MapAs<TSource, TDest>(Func<TSource, TDest> converter)
        {
            _simpleConverters[typeof(TSource)] = src => converter((TSource)src);
            return this;
        }

        public MapConfig MapAs<TSource, TDest>(Func<TSource, TDest, TDest> converter)
        {
            _advancedConverters[typeof(TSource)] = (src, dest) => converter((TSource)src, (TDest)dest);
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

        internal bool TryConvert(object source, Type targetType, out object result, object existingDest = null)
        {
            // Try advanced converter first if we have a destination
            if (existingDest != null && _advancedConverters.TryGetValue(source.GetType(), out var advancedConverter))
            {
                result = advancedConverter(source, existingDest);
                return true;
            }

            // Fall back to simple converter
            if (_simpleConverters.TryGetValue(source.GetType(), out var simpleConverter))
            {
                result = simpleConverter(source);
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
}