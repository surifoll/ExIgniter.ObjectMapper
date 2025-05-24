using System;

namespace ExIgniter.ObjectMapper.Types
{
      

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

    public enum NullHandlingStrategy
    {
        ThrowException,
        UseDefaultValue,
        UseNull
    }

}