using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExIgniter.ObjectMapper.Objects
{
    public static class ObjectExtension
    {
        public static void GetTypeOfVariable(PropertyInfo property, out bool type, out bool typeICollection)
        {
            var ns = property?.PropertyType?.FullName;
            type = ns != null && (bool)ns?.StartsWith("System");
            typeICollection = ns != null && (bool)ns?.StartsWith("System.Collection");
        }
        public static string ToPrettyString(this object obj)
        {
            
            var myClassType = obj.GetType();
            var properties = myClassType.GetProperties();
            var result = "";
            foreach (var property in properties)
            {
                GetTypeOfVariable(property, out var isSystemType, out var isICollection);
                if (isSystemType && isICollection)
                {
                    var value = property.GetValue(obj, null);
                    result += $"{property.Name, 5}::: ==> {(value as IEnumerable)?.ToPrettyString()} \n";

                }
                else if (!isSystemType)
                {
                    var value = property.GetValue(obj, null);
                    result += $"{property.Name, 5}::: ==> {value?.ToPrettyString() } \n";
                }
                else
                {
                    result += $"{property.Name, 10}:{property.GetValue(obj, null)} \n";
                }

            }
            result += "=================================================\n";
            return result;

        }

        private static void GetTypeOfVariable(Type myClassType, out bool type, out bool typeICollection)
        {
            var ns = myClassType?.FullName;
            type = ns != null && (bool)ns?.StartsWith("System");
            typeICollection = ns != null && (bool)ns?.StartsWith("System.Collection");
        }

        public static bool IsSystemType(this Type type)
        {
            return type.Assembly == typeof(object).Assembly;
        }

        public static string ToPrettyString(this IEnumerable obj)
        {
             
            var result = "";
            foreach (var item in obj)
            {
                var myClassType = item.GetType();
                var properties = myClassType.GetProperties();

                foreach (var property in properties)
                {
                    GetTypeOfVariable(property, out var isSystemType, out var isICollection);
                    if (isSystemType && isICollection)
                    {
                        var value = property.GetValue(item, null);
                        result += $"{property.Name, 5}::: ==> {value?.ToPrettyString()} \n";

                    }
                    else if (!isSystemType)
                    {
                        var value = property.GetValue(item, null);
                        result += $"{property.Name, 5}::: ==> {value?.ToPrettyString() } \n";

                    }
                    else
                    {
                        var value = property.GetValue(item, null);
                        result += $"{property.Name, 10}:{value?.ToString()} \n";
                    }
                }
                // result += $"{property.Name}:{property.GetValue(item, null)} \n";
                result += "=================================================\n";
            }
            return result;
        }

        public static Type GetGenericIEnumerables(this object o)
        {
            return o.GetType()
                .GetInterfaces()
                .Where(t => t.IsGenericType
                            && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(t => t.GetGenericArguments()[0]).FirstOrDefault();
        }
    }
}