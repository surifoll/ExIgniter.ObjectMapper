using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ExIgniter.ObjectMapper.Objects
{
    public static class ObjectExtension
    {
        public static string ToPrettyString(this object obj)
        {
            var myClassType = obj.GetType();
            var properties = myClassType.GetProperties();
            var result = "";
            foreach (var property in properties)
                result += $"{property.Name}:{property.GetValue(obj, null)} \n";
            return result;
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
                    result += $"{property.Name}:{property.GetValue(item, null)} \n";
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