using System;
using System.Collections.Generic;
using ExIgniter.ObjectMapper.ObjectMapper;
using Xunit;

namespace ExIgniter.ObjectMapper.Tests
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

            var result = source.Map<ComplexTarget>();

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

            var result = source.Map<PrimitiveTarget>(x => new[] { nameof(PrimitiveTarget.StringVal), nameof(PrimitiveTarget.BoolVal) });

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
    }
}