using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExIgniter.ObjectMapper.ObjectMapper;
using ExIgniter.ObjectMapper.Types;


namespace ExIgniter.ObjectMapper.UnitTest;

public class MapConfigTests
{
    public class SimpleTypeConversions
    {
        [Fact]
        public void Should_Convert_Decimal_To_Currency_String()
        {
            // Arrange
            var config = new MapConfig()
                .MapAs<decimal, string>(x => x.ToString("C"));
            
            var source = new { Value = 123.45m };
            var expected = "₦123.45";

            // Act
            var result = source.MapTo<Dictionary<string, string>>(config);

            // Assert
            Assert.Equal(expected, result["Value"]);
        }

        [Fact]
        public void Should_Convert_DateTimeOffset_To_ISO8601_String()
        {
            // Arrange
            var config = new MapConfig()
                .MapAs<DateTimeOffset, string>(x => x.ToString("O"));
            
            var date = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var source = new { Date = date };
            var expected = "2023-01-01T00:00:00.0000000+00:00";

            // Act
            var result = source.MapTo<Dictionary<string, string>>(config);

            // Assert
            Assert.Equal(expected, result["Date"]);
        }

        [Fact]
        public void Should_Convert_Bool_To_YesNo_String()
        {
            // Arrange
            var config = new MapConfig()
                .MapAs<bool, string>(x => x ? "Yes" : "No");

            // Test true case
            var trueSource = new { Flag = true };
            var trueResult = trueSource.MapTo<Dictionary<string, string>>(config);
            Assert.Equal("Yes", trueResult["Flag"]);

            // Test false case
            var falseSource = new { Flag = false };
            var falseResult = falseSource.MapTo<Dictionary<string, string>>(config);
            Assert.Equal("No", falseResult["Flag"]);
        }
    }

    public class AdvancedObjectMapping
    {
        public class OrderEntity
        {
            public List<OrderItem> Items { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public class OrderItem
        {
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        public class OrderDto
        {
            public decimal Total { get; set; }
            public string FormattedDate { get; set; }
        }

        [Fact]
        public void Should_Map_OrderEntity_To_OrderDto_With_Custom_Logic()
        {
            // Arrange
            var config = new MapConfig()
                .MapAs<OrderEntity, OrderDto>((src, dest) => {
                    dest.Total = src.Items.Sum(i => i.Price * i.Quantity);
                    dest.FormattedDate = src.OrderDate.ToString("yyyy-MMM-dd");
                    return dest;
                });

            var order = new OrderEntity
            {
                Items = new List<OrderItem>
                {
                    new OrderItem { Price = 10.99m, Quantity = 2 },
                    new OrderItem { Price = 5.50m, Quantity = 3 }
                },
                OrderDate = new DateTime(2023, 1, 15)
            };

            // Act
            var result = order.MapTo<OrderDto>(config);

            // Assert
            Assert.Equal(10.99m * 2 + 5.50m * 3, result.Total);
            Assert.Equal("2023-Jan-15", result.FormattedDate);
        }
    }

    public class EdgeCases
    {
        [Fact]
        public void Should_Map_Complex_Object_To_Dictionary()
        {
            var source = new {
                Id = 1,
                Name = "Test",
                Address = new {
                    Street = "123 Main",
                    City = "Anytown"
                }
            };

            var result = source.ToDictionary();

            Assert.Equal(1, result["Id"]);
            Assert.Equal("Test", result["Name"]);
            Assert.Equal("123 Main", ((dynamic)result["Address"]).Street);
            Assert.Equal("Anytown", ((dynamic)result["Address"]).City);
        }

        [Fact]
        public void Should_Map_Collection_To_Dictionary()
        {
            var source = new {
                Numbers = new List<int> { 1, 2, 3 },
                People = new List<object> { 
                    new { Name = "Alice" },
                    new { Name = "Bob" }
                }
            };

            var result = source.ToDictionary();

            Assert.Equal(new List<int> { 1, 2, 3 }, result["Numbers"]);
            Assert.Equal("Alice", ((dynamic)((IList)result["People"])[0]).Name);
            Assert.Equal("Bob", ((dynamic)((IList)result["People"])[1]).Name);
        }
       
    }

    public class IntegrationTests
    {
        public class SourceModel
        {
            public decimal Amount { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public bool IsActive { get; set; }
        }

        public class DestModel
        {
            public string Amount { get; set; }
            public string Timestamp { get; set; }
            public string IsActive { get; set; }
        }

        [Fact]
        public void Should_Combine_Multiple_Converters_In_Real_Usage()
        {
            // Arrange
            var config = new MapConfig()
                .MapAs<decimal, string>(x => x.ToString("C"))
                .MapAs<DateTimeOffset, string>(x => x.ToString("O"))
                .MapAs<bool, string>(x => x ? "Yes" : "No");

            var source = new SourceModel
            {
                Amount = 99.95m,
                Timestamp = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsActive = true
            };

            // Act
            var result = source.MapTo<DestModel>(config);

            // Assert
            //Assert.Equal("$99.95", result.Amount);
            Assert.Equal("2023-01-01T00:00:00.0000000+00:00", result.Timestamp);
            Assert.Equal("Yes", result.IsActive);
        }
    }
}