namespace ExIgniter.ObjectMapper.Types
{
    public static class TypeExtension
    {
        public static TypeDescription GetTypeDescription(this System.Type obj)
        {
            return new TypeDescription()
            {
                Description = obj.AssemblyQualifiedName,
                Name = obj.FullName
            };
        }
    }
    public class TypeDescription
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
