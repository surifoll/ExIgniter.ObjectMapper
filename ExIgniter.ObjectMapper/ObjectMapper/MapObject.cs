using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using ExIgniter.ObjectMapper.Objects;

namespace ExIgniter.ObjectMapper.ObjectMapper
{
    public static class MapObject
    {
        #region Helper Methods
        public static string GetParamName(MethodInfo method, int index)
        {
            string retVal = string.Empty;

            if (method != null && method.GetParameters().Length > index)
                retVal = method.GetParameters()[index].Name;


            return retVal;
        }
        public static void GetTypeOfVariable(PropertyInfo property, out bool type, out bool typeICollection)
        {
            var ns = property?.PropertyType?.FullName;
            type = ns != null && (bool)ns?.StartsWith("System");
            typeICollection = ns != null && (bool)ns?.StartsWith("System.Collection");
        }
        private static bool IsSystemType(PropertyInfo property)
        {
            var ns = property?.PropertyType?.FullName;
            var type = ns != null && (bool)ns?.StartsWith("System");
            return type;
        }

        private static Type GetTypeCustom(object item)
        {
            return item.GetType();
        }

        private static Type GetTypeGeneric<T>(T destination)
        {
            return destination.GetGenericIEnumerables();
        }

        private static List<PropertyInfo> GetListPropertyInfo(Type srcTypeL)
        {
            return srcTypeL.GetProperties().ToList();
        }
       
        #endregion
        public static T FasterMap<T>(this object source, T destination)
        {
            if (source == null)
                return default(T);
            if (source is IEnumerable)
            {
                var destinationMain = destination as IList;
                foreach (var item in source as IEnumerable)
                {
                    var srcTypeL = GetTypeCustom(item);
                    var srcPropertiesL = GetListPropertyInfo(srcTypeL);

                    var destinationTypeL = GetTypeGeneric(destination);

                    var destPropertiesL = GetListPropertyInfo(destinationTypeL);

                    var instance = Activator.CreateInstance(destinationTypeL);

                    foreach (var property in srcPropertiesL)
                    {
                        GetTypeOfVariable(property, out var isSystemType, out var isICollection);
                        if (isSystemType && !isICollection)
                        {
                            var destPropertName = Util.CalculateSimilarity(property.Name, destPropertiesL)
                                .FirstOrDefault()?.DestPropertName;
                            var bestMatch = destPropertiesL.Where(r => destPropertName == r.Name);
                            bestMatch.FirstOrDefault()?.SetValue(instance, property.GetValue(item, null));
                        }
                        else if (isSystemType)
                        {
                            var props = TypeDescriptor.GetProperties(destinationTypeL);
                            var destPropertName = Util.CalculateSimilarity(property.Name, destPropertiesL)
                                                      .FirstOrDefault()?.DestPropertName ??
                                                  throw new InvalidOperationException();
                            var selectedProperty = destPropertiesL.FirstOrDefault(r => r.Name == destPropertName);

                            if (selectedProperty == null) continue;
                            var instance1 = Activator.CreateInstance(selectedProperty.PropertyType);
                            var value = property.GetValue(item, null);
                            var mappedData = value.FasterMap(instance1 as IList);
                            props[destPropertName].SetValue(instance, mappedData);
                        }
                    }
                    destinationMain?.Add(instance);
                }
                return (T) destinationMain;
            }
            {
                var srcType = GetTypeCustom(source);
                var srcProperties = GetListPropertyInfo(srcType);

                // store this collection for optimum performance
                var props = TypeDescriptor.GetProperties(
                    typeof(T));
                var destinationType = GetTypeCustom(destination);
                var destProperties = GetListPropertyInfo(destinationType);

                foreach (var property in srcProperties)
                {
                    GetTypeOfVariable(property, out var isSystemType, out var isICollection);

                    if (isSystemType && !isICollection)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties).FirstOrDefault()
                            ?.DestPropertName;
                        var bestMatch = destProperties.Where(r => destPropertName == r.Name);
                        bestMatch.FirstOrDefault()?.SetValue(destination, property.GetValue(source, null));
                    }
                    else if (isSystemType)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);

                        if (selectedProperty != null)
                        {
                            var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                            var value = property.GetValue(source, null);
                            var mappedData = value.FasterMap(instance as IList);
                            props[destPropertName].SetValue(destination, mappedData);
                        }
                    }
                    else
                    {
                        if (property == null) continue;
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);
                        if (selectedProperty == null) continue;
                        var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                        var mappedData = property.GetValue(source, null).FasterMap(instance);
                        props[destPropertName].SetValue(destination, mappedData);
                    }
                }

                return destination;
            }
        }
        public static T FasterMap<T>(this object source, T destination, Func<T,string[]> exceptPred) where T : new()
        {
            if (source == null)
                return default(T);

            var excludePropertie = exceptPred.Invoke(new T());
            if (source is IEnumerable)
            {
                var destinationMain = destination as IList;
                foreach (var item in source as IEnumerable)
                {
                    var srcTypeL = GetTypeCustom(item);
                    var srcPropertiesL = GetListPropertyInfo(srcTypeL);
                    var destinationTypeL = GetTypeGeneric(destination);
                    var destPropertiesL = GetListPropertyInfo(destinationTypeL);
                    var instance = Activator.CreateInstance(destinationTypeL);


                    foreach (var property in srcPropertiesL)
                    {
                        if (excludePropertie.Contains(property.Name))
                            continue;
                        GetTypeOfVariable(property, out var isSystemType, out var isICollection);
                        if (isSystemType && !isICollection)
                        {
                            var destPropertName = Util.CalculateSimilarity(property.Name, destPropertiesL)
                                .FirstOrDefault()?.DestPropertName;
                            var bestMatch = destPropertiesL.Where(r => destPropertName == r.Name);
                            bestMatch.FirstOrDefault()?.SetValue(instance, property.GetValue(item, null));
                        }
                        else if (isSystemType)
                        {
                            var props = TypeDescriptor.GetProperties(destinationTypeL);
                            var destPropertName = Util.CalculateSimilarity(property.Name, destPropertiesL)
                                                      .FirstOrDefault()?.DestPropertName ??
                                                  throw new InvalidOperationException();
                            var selectedProperty = destPropertiesL.FirstOrDefault(r => r.Name == destPropertName);

                            if (selectedProperty == null) continue;
                            var instance1 = Activator.CreateInstance(selectedProperty.PropertyType);
                            var value = property.GetValue(item, null);
                            var mappedData = value.FasterMap(instance1 as IList);
                            props[destPropertName].SetValue(instance, mappedData);
                        }
                    }
                    destinationMain?.Add(instance);
                }
                return (T) destinationMain;
            }
            {
                var srcType = GetTypeCustom(source);
                var srcProperties = GetListPropertyInfo(srcType);

                // store this collection for optimum performance
                var props = TypeDescriptor.GetProperties(
                    typeof(T));
                var destinationType = GetTypeCustom(destination);
                var destProperties = GetListPropertyInfo(destinationType);

                foreach (var property in srcProperties)
                {
                    if (excludePropertie.Contains(property.Name))
                        continue;
                    GetTypeOfVariable(property, out var isSystemType, out var isICollection);

                    if (isSystemType && !isICollection)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties).FirstOrDefault()
                            ?.DestPropertName;
                        var bestMatch = destProperties.Where(r => destPropertName == r.Name);
                        bestMatch.FirstOrDefault()?.SetValue(destination, property.GetValue(source, null));
                    }
                    else if (isSystemType)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);

                        if (selectedProperty != null)
                        {
                            var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                            var value = property.GetValue(source, null);
                            var mappedData = value.FasterMap(instance as IList);
                            props[destPropertName].SetValue(destination, mappedData);
                        }
                    }
                    else
                    {
                        if (property == null) continue;
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);
                        if (selectedProperty == null) continue;
                        var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                        var mappedData = property.GetValue(source, null).FasterMap(instance);
                        props[destPropertName].SetValue(destination, mappedData);
                    }
                }

                return destination;
            }
        }
        public static T Map<T>(this object source, T destination)
        {
            if (source == null)
                return default(T);
            if (source is IEnumerable)
            {
                var destinationMain = destination as IList;
                foreach (var item in source as IEnumerable)
                {
                    var srcTypeL = GetTypeCustom(item);
                    var srcPropertiesL = GetListPropertyInfo(srcTypeL);
                    var destinationTypeR = GetTypeGeneric(destination);
                    var destPropertiesR = GetListPropertyInfo(destinationTypeR);


                    var instance = Activator.CreateInstance(destinationTypeR);

                    var props1 = TypeDescriptor.GetProperties(instance.GetType());

                    foreach (var property in srcPropertiesL)
                    {
                        //property.SetMethod.Attributes.
                        var isSystemType = IsSystemType(property);
                        if (isSystemType)
                        {
                            var destinationPropertName =
                                Util.CalculateSimilarity(property.Name, destPropertiesR).FirstOrDefault()
                                    ?.DestPropertName ?? throw new InvalidOperationException();
                            props1[destinationPropertName].SetValue(instance, property.GetValue(item, null));
                        }
                    }
                    destinationMain?.Add(instance);
                }
                return (T) destinationMain;
            }
            else
            {
                var srcType = GetTypeCustom(source);
                var srcProperties = GetListPropertyInfo(srcType);

                // store this collection for optimum performance
                var props = TypeDescriptor.GetProperties(typeof(T));
                var destinationType = GetTypeCustom(destination);

                var destProperties = GetListPropertyInfo(destinationType);

                foreach (var property in srcProperties)
                {
                    GetTypeOfVariable(property, out var isSystemType, out var isICollection);

                    if (isSystemType && !isICollection)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties).FirstOrDefault()
                            ?.DestPropertName;
                        var bestMatch = destProperties.Where(r => destPropertName == r.Name);
                        bestMatch.FirstOrDefault()?.SetValue(destination, property.GetValue(source, null));
                    }
                    else if (isSystemType)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedPropertyInfo = destProperties.FirstOrDefault(r => r.Name == destPropertName);

                        if (selectedPropertyInfo != null)
                        {
                            var instance = Activator.CreateInstance(selectedPropertyInfo.PropertyType);
                            var value = property.GetValue(source, null);
                            var mappedData = value.FasterMap(instance as IList);
                            props[destPropertName].SetValue(destination, mappedData);
                        }
                    }
                    else
                    {
                        if (property == null) continue;
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);
                        if (selectedProperty == null) continue;
                        var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                        var mappedData = property.GetValue(source, null).FasterMap(instance);
                        props[destPropertName].SetValue(destination, mappedData);
                    }
                }

                return destination;
            }
           
        }
        public static T Map<T>(this object source, T destination, Func<T, string[]> exceptPred) where T : new()
        {
            if (source == null)
                return default(T);

            var excludePropertie = exceptPred.Invoke(new T());
            if (source is IEnumerable)
            {
                var destinationMain = destination as IList;
                foreach (var item in source as IEnumerable)
                {
                   
                    var srcTypeL = GetTypeCustom(item);
                    var srcPropertiesL = GetListPropertyInfo(srcTypeL);
                    var destinationTypeL = GetTypeGeneric(destination);
                    var destPropertiesL = GetListPropertyInfo(destinationTypeL);


                    var instance = Activator.CreateInstance(destinationTypeL);

                    var props1 = TypeDescriptor.GetProperties(instance.GetType());

                    foreach (var property in srcPropertiesL)
                    {
                        if (excludePropertie.Contains(property.Name))
                            continue;
                        var isSystemType = IsSystemType(property);
                        if (isSystemType)
                        {
                            var destinationPropertName =
                                Util.CalculateSimilarity(property.Name, destPropertiesL).FirstOrDefault()
                                    ?.DestPropertName ?? throw new InvalidOperationException();
                            props1[destinationPropertName].SetValue(instance, property.GetValue(item, null));
                        }
                    }
                    destinationMain?.Add(instance);
                }
                return (T)destinationMain;
            }
            else
            {
                var srcType = GetTypeCustom(source);
                var srcProperties = GetListPropertyInfo(srcType);

                // store this collection for optimum performance
                var props = TypeDescriptor.GetProperties(typeof(T));
                var destinationType = GetTypeCustom(destination);

                var destProperties = GetListPropertyInfo(destinationType);

                foreach (var property in srcProperties)
                {
                    GetTypeOfVariable(property, out var isSystemType, out var isICollection);

                    if (isSystemType && !isICollection)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties).FirstOrDefault()
                            ?.DestPropertName;
                        var bestMatch = destProperties.Where(r => destPropertName == r.Name);
                        bestMatch.FirstOrDefault()?.SetValue(destination, property.GetValue(source, null));
                    }
                    else if (isSystemType)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedPropertyInfo = destProperties.FirstOrDefault(r => r.Name == destPropertName);

                        if (selectedPropertyInfo != null)
                        {
                            var instance = Activator.CreateInstance(selectedPropertyInfo.PropertyType);
                            var value = property.GetValue(source, null);
                            var mappedData = value.FasterMap(instance as IList);
                            props[destPropertName].SetValue(destination, mappedData);
                        }
                    }
                    else
                    {
                        if (property == null) continue;
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);
                        if (selectedProperty == null) continue;
                        var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                        var mappedData = property.GetValue(source, null).FasterMap(instance);
                        props[destPropertName].SetValue(destination, mappedData);
                    }
                }

                return destination;
            }

        }
        public static T ComplexMap<T>(this object source, T destination)
        {
            if (source == null)
                return default(T);

            var srcType = GetTypeCustom(source);
            var srcProperties = GetListPropertyInfo(srcType);

            // store this collection for optimum performance
            var props = TypeDescriptor.GetProperties(destination.GetType());

            var destinationType = GetTypeCustom(destination);
            var destProperties = GetListPropertyInfo(destinationType);

            foreach (var property in srcProperties)
            {
                GetTypeOfVariable(property, out var isSystemType, out var isICollection);

                if (isSystemType && !isICollection)
                {
                    var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                              .FirstOrDefault()?.DestPropertName ??
                                          throw new InvalidOperationException();
                    props[destPropertName]
                        .SetValue(destination, property.GetValue(source, null));
                }
                else if (isSystemType)
                {
                    var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                              .FirstOrDefault()?.DestPropertName ??
                                          throw new InvalidOperationException();
                    var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);

                    if (selectedProperty != null)
                    {
                        var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                        var value = property.GetValue(source, null);
                        var mappedData = value.FasterMap(instance as IList);
                        props[destPropertName].SetValue(destination, mappedData);
                    }
                }
                else
                {
                    if (property != null)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);
                        if (selectedProperty != null)
                        {
                            var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                            var data = property.GetValue(source, null).ComplexMap(instance);
                            props[destPropertName].SetValue(destination, data);
                        }
                    }
                }
            }

            return destination;
        }
        public static T ComplexMap<T>(this object source, T destination, Func<T, string[]> exceptPred) where T : new()
        {
            if (source == null)
                return default(T);

            var excludePropertie = exceptPred.Invoke(new T());
            var srcType = GetTypeCustom(source);
            var srcProperties = GetListPropertyInfo(srcType);

            // store this collection for optimum performance
            var props = TypeDescriptor.GetProperties(destination.GetType());

            var destinationType = GetTypeCustom(destination);
            var destProperties = GetListPropertyInfo(destinationType);

            foreach (var property in srcProperties)
            {
                if (excludePropertie.Contains(property.Name))
                    continue;
                GetTypeOfVariable(property, out var isSystemType, out var isICollection);

                if (isSystemType && !isICollection)
                {
                    var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                              .FirstOrDefault()?.DestPropertName ??
                                          throw new InvalidOperationException();
                    props[destPropertName]
                        .SetValue(destination, property.GetValue(source, null));
                }
                else if (isSystemType)
                {
                    var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                              .FirstOrDefault()?.DestPropertName ??
                                          throw new InvalidOperationException();
                    var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);

                    if (selectedProperty != null)
                    {
                        var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                        var value = property.GetValue(source, null);
                        var mappedData = value.FasterMap(instance as IList);
                        props[destPropertName].SetValue(destination, mappedData);
                    }
                }
                else
                {
                    if (property != null)
                    {
                        var destPropertName = Util.CalculateSimilarity(property.Name, destProperties)
                                                  .FirstOrDefault()?.DestPropertName ??
                                              throw new InvalidOperationException();
                        var selectedProperty = destProperties.FirstOrDefault(r => r.Name == destPropertName);
                        if (selectedProperty != null)
                        {
                            var instance = Activator.CreateInstance(selectedProperty.PropertyType);
                            var data = property.GetValue(source, null).ComplexMap(instance);
                            props[destPropertName].SetValue(destination, data);
                        }
                    }
                }
            }

            return destination;
        }
    }
}


