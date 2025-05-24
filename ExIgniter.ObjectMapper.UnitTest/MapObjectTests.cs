using ExIgniter.ObjectMapper.ObjectMapper;
using ExIgniter.ObjectMapper.Types;

namespace ExIgniter.ObjectMapper.UnitTest
{
    public class MapObjectTests
    {
        #region Test Models

        public class PrimitiveSource
        {
            public byte ByteVal { get; set; }
            public sbyte SByteVal { get; set; }
            public short ShortVal { get; set; }
            public ushort UShortVal { get; set; }
            public int IntVal { get; set; }
            public uint UIntVal { get; set; }
            public long LongVal { get; set; }
            public ulong ULongVal { get; set; }
            public float FloatVal { get; set; }
            public double DoubleVal { get; set; }
            public decimal DecimalVal { get; set; }
            public bool BoolVal { get; set; }
            public char CharVal { get; set; }
            public string StringVal { get; set; }
            public DateTime DateTimeVal { get; set; }
            public DateTimeOffset DateTimeOffsetVal { get; set; }
            public TimeSpan TimeSpanVal { get; set; }
            public Guid GuidVal { get; set; }
            public DayOfWeek EnumVal { get; set; }
        }

        public class PrimitiveTarget
        {
            public byte ByteVal { get; set; }
            public sbyte SByteVal { get; set; }
            public short ShortVal { get; set; }
            public ushort UShortVal { get; set; }
            public int IntVal { get; set; }
            public uint UIntVal { get; set; }
            public long LongVal { get; set; }
            public ulong ULongVal { get; set; }
            public float FloatVal { get; set; }
            public double DoubleVal { get; set; }
            public decimal DecimalVal { get; set; }
            public bool BoolVal { get; set; }
            public char CharVal { get; set; }
            public string StringVal { get; set; }
            public DateTime DateTimeVal { get; set; }
            public DateTimeOffset DateTimeOffsetVal { get; set; }
            public TimeSpan TimeSpanVal { get; set; }
            public Guid GuidVal { get; set; }
            public DayOfWeek EnumVal { get; set; }
        }

        public class ComplexChild
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public ComplexChild Friend { get; set; }
        }

        public class ComplexSource
        {
            public PrimitiveSource Primitive { get; set; }
            public List<ComplexChild> Children { get; set; }
            public Dictionary<string, ComplexChild> ChildMap { get; set; }
            public ComplexSource SelfReference { get; set; }
        }

        public class ComplexTarget
        {
            public PrimitiveTarget Primitive { get; set; }
            public List<ComplexChild> Children { get; set; }
            public Dictionary<string, ComplexChild> ChildMap { get; set; }
            public ComplexTarget SelfReference { get; set; }
        }

        public class CollectionSource
        {
            public int[] IntArray { get; set; }
            public List<string> StringList { get; set; }
            public HashSet<double> DoubleSet { get; set; }
            public Dictionary<int, string> IntStringDict { get; set; }
            public Queue<DateTime> DateQueue { get; set; }
            public Stack<Guid> GuidStack { get; set; }
        }

        public class CollectionTarget
        {
            public int[] IntArray { get; set; }
            public List<string> StringList { get; set; }
            public HashSet<double> DoubleSet { get; set; }
            public Dictionary<int, string> IntStringDict { get; set; }
            public Queue<DateTime> DateQueue { get; set; }
            public Stack<Guid> GuidStack { get; set; }
        }

        #endregion

        [Fact]
        public void Maps_All_Primitive_Properties_Correctly()
        {
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            var timeSpan = TimeSpan.FromHours(1);
            var guid = Guid.NewGuid();

            var source = new PrimitiveSource
            {
                ByteVal = 1,
                SByteVal = -1,
                ShortVal = -100,
                UShortVal = 100,
                IntVal = -1000,
                UIntVal = 1000,
                LongVal = -10000,
                ULongVal = 10000,
                FloatVal = 1.234f,
                DoubleVal = 3.14159,
                DecimalVal = 99.99m,
                BoolVal = true,
                CharVal = 'X',
                StringVal = "Test",
                DateTimeVal = now,
                DateTimeOffsetVal = new DateTimeOffset(utcNow),
                TimeSpanVal = timeSpan,
                GuidVal = guid,
                EnumVal = DayOfWeek.Wednesday
            };

            var result = source.Map<PrimitiveTarget>();

            Assert.Equal(source.ByteVal, result.ByteVal);
            Assert.Equal(source.SByteVal, result.SByteVal);
            Assert.Equal(source.ShortVal, result.ShortVal);
            Assert.Equal(source.UShortVal, result.UShortVal);
            Assert.Equal(source.IntVal, result.IntVal);
            Assert.Equal(source.UIntVal, result.UIntVal);
            Assert.Equal(source.LongVal, result.LongVal);
            Assert.Equal(source.ULongVal, result.ULongVal);
            Assert.Equal(source.FloatVal, result.FloatVal);
            Assert.Equal(source.DoubleVal, result.DoubleVal);
            Assert.Equal(source.DecimalVal, result.DecimalVal);
            Assert.Equal(source.BoolVal, result.BoolVal);
            Assert.Equal(source.CharVal, result.CharVal);
            Assert.Equal(source.StringVal, result.StringVal);
            Assert.Equal(source.DateTimeVal, result.DateTimeVal);
            Assert.Equal(source.DateTimeOffsetVal, result.DateTimeOffsetVal);
            Assert.Equal(source.TimeSpanVal, result.TimeSpanVal);
            Assert.Equal(source.GuidVal, result.GuidVal);
            Assert.Equal(source.EnumVal, result.EnumVal);
        }

        [Fact]
        public void Maps_Complex_Object_And_Nested_Properties_Correctly()
        {
            var source = new ComplexSource
            {
                Primitive = new PrimitiveSource
                {
                    IntVal = 1,
                    StringVal = "nested",
                    BoolVal = false,
                    DecimalVal = 55.55m,
                    DateTimeVal = DateTime.UtcNow,
                    DoubleVal = 2.71,
                    GuidVal = Guid.NewGuid()
                },
                Children = new List<ComplexChild>
                {
                    new ComplexChild { Name = "Child1", Age = 5 },
                    new ComplexChild { Name = "Child2", Age = 10, Friend = new ComplexChild { Name = "Child3" } }
                },
                ChildMap = new Dictionary<string, ComplexChild>
                {
                    ["First"] = new ComplexChild { Name = "DictChild1" },
                    ["Second"] = new ComplexChild { Name = "DictChild2" }
                }
            };
            source.SelfReference = source;

            var result = source.Map<ComplexTarget>(new ComplexTarget());

            // Test primitive mapping
            Assert.NotNull(result);
            Assert.NotNull(result.Primitive);
            Assert.Equal(source.Primitive.StringVal, result.Primitive.StringVal);
            Assert.Equal(source.Primitive.IntVal, result.Primitive.IntVal);
            Assert.Equal(source.Primitive.GuidVal, result.Primitive.GuidVal);

            // Test list mapping
            Assert.NotNull(result.Children);
            Assert.Equal(2, result.Children.Count);
            Assert.Equal("Child1", result.Children[0].Name);
            Assert.Equal(10, result.Children[1].Age);
            Assert.Equal("Child3", result.Children[1].Friend.Name);

            // Test dictionary mapping
            Assert.NotNull(result.ChildMap);
            Assert.Equal(2, result.ChildMap.Count);
            Assert.Equal("DictChild1", result.ChildMap["First"].Name);
            Assert.Equal("DictChild2", result.ChildMap["Second"].Name);

            // Test circular reference
            Assert.Same(result, result.SelfReference);
        }

        [Fact]
        public void Maps_All_Collection_Types_Correctly()
        {
            var now = DateTime.Now;
            var guid = Guid.NewGuid();

            var dateTime = now.AddDays(1);
            var source = new CollectionSource
            {
                IntArray = new[] { 1, 2, 3 },
                StringList = new List<string> { "a", "b", "c" },
                DoubleSet = new HashSet<double> { 1.1, 2.2, 3.3 },
                IntStringDict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } },
                DateQueue = new Queue<DateTime>(new[] { now, dateTime }),
                GuidStack = new Stack<Guid>(new[] { guid, Guid.NewGuid() })
            };

            var result = source.Map<CollectionTarget>();

            Assert.Equal(source.IntArray, result.IntArray);
            Assert.Equal(source.StringList, result.StringList);
            Assert.Equal(source.DoubleSet, result.DoubleSet);
            Assert.Equal(source.IntStringDict, result.IntStringDict);

            Assert.Equal(2, result.DateQueue.Count);
            var dequeue = result.DateQueue.Dequeue();
            Assert.Equal(now, dequeue);

            Assert.Equal(2, result.GuidStack.Count);
            Assert.Equal(guid, result.GuidStack.Pop());
        }

        [Fact]
        public void Handles_Null_Values_Gracefully()
        {
            var source = new ComplexSource
            {
                Primitive = null,
                Children = null,
                ChildMap = new Dictionary<string, ComplexChild> { ["key"] = null },
                SelfReference = null
            };

            var result = source.Map<ComplexTarget>();

            Assert.Null(result.Primitive);
            Assert.Null(result.Children);
            Assert.NotNull(result.ChildMap);
            Assert.Null(result.ChildMap["key"]);
            Assert.Null(result.SelfReference);
        }

        [Fact]
        public void Maps_Using_FasterMap_Method()
        {
            var source = new PrimitiveSource
            {
                IntVal = 42,
                StringVal = "FasterMapTest"
            };

            var result = source.FasterMap(new PrimitiveTarget());

            Assert.Equal(source.IntVal, result.IntVal);
            Assert.Equal(source.StringVal, result.StringVal);
        }

        [Fact]
        public void Maps_Using_ComplexMap_Method()
        {
            var source = new ComplexSource
            {
                Primitive = new PrimitiveSource { IntVal = 100 },
                Children = new List<ComplexChild> { new ComplexChild { Name = "ComplexMapTest" } }
            };

            var result = source.ComplexMap(new ComplexTarget());

            Assert.Equal(100, result.Primitive.IntVal);
            Assert.Equal("ComplexMapTest", result.Children[0].Name);
        }

        [Fact]
        public void Excludes_Properties_When_Specified()
        {
            var source = new PrimitiveSource
            {
                IntVal = 1,
                StringVal = "ExcludeTest",
                BoolVal = true
            };

            var result = source.Map<PrimitiveTarget>(x => new[]
                { nameof(PrimitiveTarget.StringVal), nameof(PrimitiveTarget.BoolVal) });

            Assert.Equal(1, result.IntVal);
            Assert.Null(result.StringVal); // Should be excluded
            Assert.False(result.BoolVal); // Should be excluded (default value)
        }

        [Fact]
        public void Handles_Empty_Collections_Correctly()
        {
            var source = new CollectionSource
            {
                IntArray = Array.Empty<int>(),
                StringList = new List<string>(),
                DoubleSet = new HashSet<double>(),
                IntStringDict = new Dictionary<int, string>(),
                DateQueue = new Queue<DateTime>(),
                GuidStack = new Stack<Guid>()
            };

            var result = source.Map<CollectionTarget>();

            Assert.NotNull(result.IntArray);
            Assert.Empty(result.IntArray);
            Assert.NotNull(result.StringList);
            Assert.Empty(result.StringList);
            Assert.NotNull(result.DoubleSet);
            Assert.Empty(result.DoubleSet);
            Assert.NotNull(result.IntStringDict);
            Assert.Empty(result.IntStringDict);
            Assert.NotNull(result.DateQueue);
            Assert.Empty(result.DateQueue);
            Assert.NotNull(result.GuidStack);
            Assert.Empty(result.GuidStack);
        }

        [Fact]
        public void Maps_Complex_Object_And_Nested_Properties_Correctly2()
        {
            var source = new ComplexSource
            {
                Primitive = new PrimitiveSource
                {
                    IntVal = 1,
                    StringVal = "nested",
                    BoolVal = false,
                    DecimalVal = 55.55m,
                    DateTimeVal = DateTime.UtcNow,
                    DoubleVal = 2.71,
                    GuidVal = Guid.NewGuid()
                },
                Children = new List<ComplexChild>
                {
                    new ComplexChild { Name = "Child1", Age = 5 },
                    new ComplexChild { Name = "Child2", Age = 10, Friend = new ComplexChild { Name = "Child3" } }
                },
                ChildMap = new Dictionary<string, ComplexChild>
                {
                    ["First"] = new ComplexChild { Name = "DictChild1" },
                    ["Second"] = new ComplexChild { Name = "DictChild2" }
                }
            };
            source.SelfReference = source;

            var result = source.Map<ComplexTarget>(new ComplexTarget());

            // Test primitive mapping
            Assert.NotNull(result);
            Assert.NotNull(result.Primitive);
            Assert.Equal(source.Primitive.StringVal, result.Primitive.StringVal);
            Assert.Equal(source.Primitive.IntVal, result.Primitive.IntVal);
            Assert.Equal(source.Primitive.GuidVal, result.Primitive.GuidVal);

            // Test list mapping
            Assert.NotNull(result.Children);
            Assert.Equal(2, result.Children.Count);
            Assert.Equal("Child1", result.Children[0].Name);
            Assert.Equal(10, result.Children[1].Age);
            Assert.Equal("Child3", result.Children[1].Friend.Name);

            // Test dictionary mapping
            Assert.NotNull(result.ChildMap);
            Assert.Equal(2, result.ChildMap.Count);
            Assert.Equal("DictChild1", result.ChildMap["First"].Name);
            Assert.Equal("DictChild2", result.ChildMap["Second"].Name);

            // Test circular reference
            Assert.Same(result, result.SelfReference);
        }

        [Fact]
        public void Ignores_Specified_Dictionary_Properties_When_Configured()
        {
            // Arrange
            var source = new DictionaryTestSource
            {
                StringIntDict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 },
                IntComplexDict = new Dictionary<int, ComplexChild>
                {
                    [1] = new ComplexChild { Name = "First" },
                    [2] = new ComplexChild { Name = "Second" }
                }
            };

            var config = new MapConfig()
                .Ignore<DictionaryTestTarget>(x => x.StringIntDict);

            // Act
            var result = source.MapWithConfig<DictionaryTestTarget>(config);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.StringIntDict); // Should be ignored

            Assert.NotNull(result.IntComplexDict); // Should still be mapped
            Assert.Equal(2, result.IntComplexDict.Count);
        }

        [Fact]
        public void Ignores_Nested_Dictionary_Properties_When_Configured()
        {
            // Arrange
            var source = new NestedDictionarySource
            {
                Nested = new NestedDictionaryContainer
                {
                    ImportantDict = new Dictionary<string, int> { ["key"] = 42 },
                    IgnoredDict = new Dictionary<string, string> { ["ignore"] = "this" }
                }
            };

            var config = new MapConfig()
                .Ignore<NestedDictionaryTarget>(x => x.Nested.IgnoredDict);

            // Act
            var result = source.MapWithConfig<NestedDictionaryTarget>(config);

            // Assert
            Assert.NotNull(result.Nested);
            Assert.NotNull(result.Nested.ImportantDict);
            Assert.Equal(42, result.Nested.ImportantDict["key"]);

        }

        [Fact]
        public void Handles_Multiple_Ignore_Configurations_For_Dictionaries()
        {
            // Arrange
            var source = new MultiDictionarySource
            {
                Dict1 = new Dictionary<int, string> { [1] = "one" },
                Dict2 = new Dictionary<int, string> { [2] = "two" },
                Dict3 = new Dictionary<int, string> { [3] = "three" }
            };

            var config = new MapConfig()
                .Ignore<MultiDictionaryTarget>(x => x.Dict1)
                .Ignore<MultiDictionaryTarget>(x => x.Dict3);

            // Act
            var result = source.MapWithConfig<MultiDictionaryTarget>(config);

            // Assert
            Assert.Null(result.Dict1); // Ignored
            Assert.NotNull(result.Dict2); // Mapped
            Assert.Equal("two", result.Dict2[2]);
            Assert.Null(result.Dict3); // Ignored
        }

        [Fact]
        public void Ignores_Dictionary_Properties_With_Conditional_Logic()
        {
            // Arrange
            var source = new ConditionalDictionarySource
            {
                DictA = new Dictionary<string, int> { ["a"] = 1 },
                DictB = new Dictionary<string, int> { ["b"] = 2 },
                ShouldIgnore = true
            };

            
            var config = new MapConfig()
                .Ignore<ConditionalDictionaryTarget>(target => target.ShouldIgnore 
                    ? new[] { nameof(ConditionalDictionaryTarget.DictA) } 
                    : new[] { nameof(ConditionalDictionaryTarget.DictB) });

            // Act
            var result = source.MapWithConfig<ConditionalDictionaryTarget>(config);

            // Assert
            Assert.Null(result.DictA); // Should be ignored based on condition
            Assert.NotNull(result.DictB);
            Assert.Equal(2, result.DictB["b"]);
        }
        
        
        [Fact]
public void Maps_Properties_Using_MapAs_Attribute()
{
    // Arrange
    var source = new MapAsAttributeSource
    {
        OriginalName = "Test",
        Value = 100,
        Date = DateTime.Now
    };

    // Act
    var result = source.Map<MapAsAttributeTarget>();

    // Assert
    Assert.Equal(source.OriginalName, result.NewName);
    Assert.Equal(source.Value.ToString(), result.ValueAsString);
    Assert.Equal(source.Date.ToString("yyyy-MM-dd"), result.FormattedDate);
}

[Fact]
public void Maps_Properties_Using_MapAs_Config()
{
    // Arrange
    var source = new MapAsConfigSource
    {
        Amount = 99.99m,
        Timestamp = DateTimeOffset.Now,
        IsActive = true
    };

    var config = new MapConfig()
        .MapAs<decimal, string>(x => x.ToString("C"))
        .MapAs<DateTimeOffset, string>(x => x.ToString("O"))
        .MapAs<bool, string>(x => x ? "Yes" : "No");

    // Act
    var result = source.MapWithConfig<MapAsConfigTarget>(config);

    // Assert
    Assert.Equal(source.Amount.ToString("C"), result.AmountString);
    Assert.Equal(source.Timestamp.ToString("O"), result.TimestampString);
    Assert.Equal("Yes", result.ActiveStatus);
}

[Fact]
public void Combines_MapAs_Attribute_And_Config()
{
    // Arrange
    var source = new CombinedMapSource
    {
        Score = 85,
        Created = DateTime.Now,
        Enabled = false
    };

    var config = new MapConfig()
        .MapAs<bool, string>(x => x ? "Enabled" : "Disabled");

    // Act
    var result = source.MapWithConfig<CombinedMapTarget>(config);

    // Assert
    Assert.Equal($"{source.Score}", result.Grade); // From attribute
    Assert.Equal(source.Created.ToString("G"), result.CreatedDate); // From attribute
    Assert.Equal("Disabled", result.EnabledStatus); // From config
}

[Fact]
public void Handles_Nested_MapAs_Mapping()
{
    // Arrange
    var source = new NestedMapAsSource
    {
        Child = new MapAsChildSource
        {
            Id = 1,
            Name = "Child"
        }
    };

    // Act
    var result = source.Map<NestedMapAsTarget>();

    // Assert
    Assert.NotNull(result.Child);
    Assert.Equal($"{source.Child.Id}", result.Child.ChildId);
    Assert.Equal(source.Child.Name.ToUpper(), result.Child.ChildName.ToUpper());
}

[Fact]
public void Maps_Collections_With_MapAs_Config()
{
    // Arrange
    var source = new CollectionMapAsSource
    {
        Numbers = new List<int> { 1, 2, 3 },
        Flags = new[] { true, false, true }
    };

    var config = new MapConfig()
        .MapAs<int, string>(x => $"#{x}")
        .MapAs<bool, string>(x => x ? "Y" : "N");

    // Act
    var result = source.MapWithConfig<CollectionMapAsTarget>(config);

    // Assert
    Assert.Equal(new[] { "#1", "#2", "#3" }, result.NumberStrings);
    Assert.Equal(new[] { "Y", "N", "Y" }, result.FlagSymbols);
}

[Fact]
public void Handles_MapAs_With_Null_Values()
{
    // Arrange
    var source = new NullMapAsSource
    {
        Text = null,
        Value = null
    };

    var config = new MapConfig()
        .MapAs<string, int>(x => x.Length)
        .WithNullHandling(NullHandlingStrategy.UseDefaultValue);

    // Act
    var result = source.MapWithConfig<NullMapAsTarget>(config);

    // Assert
    Assert.Equal(0, result.TextLength); // Default for int
    Assert.Null(result.Value); // Null remains null
}

    }
    
    public class MapAsAttributeSource
{
    [MapAs("RenamedProperty")]
    public string OriginalName { get; set; }

    [MapAs(typeof(IntToStringConverter))]
    public int Value { get; set; }

    [MapAs(typeof(DateTimeFormatter))]
    public DateTime Date { get; set; }
}

public class MapAsAttributeTarget
{
    public string NewName { get; set; }
    public string ValueAsString { get; set; }
    public string FormattedDate { get; set; }
}

public class IntToStringConverter
{
    public string Convert(int value) => value.ToString();
}

public class DateTimeFormatter
{
    public string Convert(DateTime date) => date.ToString("yyyy-MM-dd");
}

public class MapAsConfigSource
{
    public decimal Amount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public bool IsActive { get; set; }
}

public class MapAsConfigTarget
{
    public string AmountString { get; set; }
    public string TimestampString { get; set; }
    public string ActiveStatus { get; set; }
}

public class CombinedMapSource
{
    [MapAs("Grade")]
    public int Score { get; set; }

    [MapAs("CreatedDate")]
    public DateTime Created { get; set; }

    public bool Enabled { get; set; }
}

public class CombinedMapTarget
{
    public string Grade { get; set; }
    public string CreatedDate { get; set; }
    public string EnabledStatus { get; set; }
}

public class ScoreToGradeConverter
{
    public string Convert(int score) => $"Grade: {score}";
}

public class ShortDateConverter
{
    public string Convert(DateTime date) => date.ToString("d");
}

public class NestedMapAsSource
{
    public MapAsChildSource Child { get; set; }
}

public class MapAsChildSource
{
    [MapAs("ChildId")]
    public int Id { get; set; }

    [MapAs("ChildName")]
    public string Name { get; set; }
}

public class NestedMapAsTarget
{
    public MapAsChildTarget Child { get; set; }
}

public class MapAsChildTarget
{
    public string ChildId { get; set; }
    public string ChildName { get; set; }
}

public class IdFormatter
{
    public string Convert(int id) => $"ID-{id}";
}

public class NameFormatter
{
    public string Convert(string name) => name.ToUpper();
}

public class CollectionMapAsSource
{
    public List<int> Numbers { get; set; }
    public bool[] Flags { get; set; }
}

public class CollectionMapAsTarget
{
    public string[] NumberStrings { get; set; }
    public string[] FlagSymbols { get; set; }
}

public class NullMapAsSource
{
    public string Text { get; set; }
    public int? Value { get; set; }
}

public class NullMapAsTarget
{
    public int TextLength { get; set; }
    public int? Value { get; set; }
}
    // Additional test model classes
    public class NestedDictionarySource
    {
        public NestedDictionaryContainer Nested { get; set; }
    }

    public class NestedDictionaryContainer
    {
        public Dictionary<string, int> ImportantDict { get; set; }
        public Dictionary<string, string> IgnoredDict { get; set; }
    }

    public class NestedDictionaryTarget
    {
        public NestedDictionaryContainer Nested { get; set; }
    }

    public class MultiDictionarySource
    {
        public Dictionary<int, string> Dict1 { get; set; }
        public Dictionary<int, string> Dict2 { get; set; }
        public Dictionary<int, string> Dict3 { get; set; }
    }

    public class MultiDictionaryTarget
    {
        public Dictionary<int, string> Dict1 { get; set; }
        public Dictionary<int, string> Dict2 { get; set; }
        public Dictionary<int, string> Dict3 { get; set; }
    }

    public class ConditionalDictionarySource
    {
        public Dictionary<string, int> DictA { get; set; }
        public Dictionary<string, int> DictB { get; set; }
        public bool ShouldIgnore { get; set; }
    }

    public class ConditionalDictionaryTarget
    {
        public Dictionary<string, int> DictA { get; set; }
        public Dictionary<string, int> DictB { get; set; }
        public bool ShouldIgnore { get; set; } = true;
    }
    
    public class DictionaryTestSource
    {
         
        public Dictionary<string, int> StringIntDict { get; set; }
        public Dictionary<int, MapObjectTests.ComplexChild> IntComplexDict { get; set; }
    }
    
    public class DictionaryTestTarget
    {
         
        public Dictionary<string, int> StringIntDict { get; set; }
        public Dictionary<int, MapObjectTests.ComplexChild> IntComplexDict { get; set; }
    }
}