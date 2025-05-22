// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using AutoMapper;
using ExIgniter.ObjectMapper.ConsoleTest.Model;
using ExIgniter.ObjectMapper.ObjectMapper;
using ExIgniter.ObjectMapper.Objects;

class Program
    {
        static void Main(string[] args)
        {
            // Sample test data
            var object1 = new Abc
            {
                Strings = "String",
                IntProperty = 1,
                IntProperty2 = 2
            };

            var object2 = new Abc
            {
                Strings = "String",
                IntProperty = 1,
                SubClass = new List<Efg> { new Efg { BoolProperty = true } }
            };

            // Testing ExIgniter custom mapping
            var result1 = object2.ComplexMap(new AbcVm());
            var result2 = object2.FasterMap(new AbcVm(), vm => new[] { nameof(vm.Strings), nameof(vm.IntProperty) });

            // Setup AutoMapper configuration
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Customer, CustomerVm>();
                cfg.CreateMap<Order, OrderVm>();
            });

            var testCustomer = new Customer
            {
                City = "Eko",
                FirstName = "Suraj",
                LastName = "Deji",
                ID = 1,
                Order = new Order { Name = "Benz", Quantity = 1 }
            };

            // Benchmark: ExIgniter FasterMap
            Console.WriteLine("========== ExIgniter FasterMap ================");
            var swExIgniter = Stopwatch.StartNew();
            var exMapped = testCustomer.FasterMap(new CustomerVm());
            swExIgniter.Stop();
            Console.WriteLine($"Elapsed = {swExIgniter.ElapsedMilliseconds} ms");

            // Benchmark: AutoMapper
            Console.WriteLine("========== AutoMapper ================");
            var swAutoMapper = Stopwatch.StartNew();
            var autoMapped = Mapper.Map<CustomerVm>(testCustomer);
            swAutoMapper.Stop();
            Console.WriteLine($"Elapsed = {swAutoMapper.ElapsedMilliseconds} ms");

            // Benchmark: ExIgniter Map (generic)
            Console.WriteLine("========== ExIgniter Map ================");
            var swMap = Stopwatch.StartNew();
            var genericMapped = testCustomer.Map<CustomerVm>();
            swMap.Stop();
            Console.WriteLine($"Elapsed = {swMap.ElapsedMilliseconds} ms");

            // Benchmark: Manual Mapping
            Console.WriteLine("========== Manual Mapping ================");
            var swManual = Stopwatch.StartNew();
            var manuallyMapped = new CustomerVm
            {
                Cty = testCustomer.City,
                FiName = testCustomer.FirstName,
                LastName = testCustomer.LastName,
                Id = testCustomer.ID,
                OrderVm = new OrderVm { Name = "kmimoi" }
            };

            var customerVmList = new List<CustomerVm>
            {
                new CustomerVm
                {
                    Cty = "Ibadan",
                    FiName = "Ahmed",
                    LastName = "Tunde",
                    Id = 1,
                    OrderVm = new OrderVm
                    {
                        Name = "kmimoi",
                        SubClass = new List<Efg> { new Efg { BoolProperty = true } }
                    }
                },
                new CustomerVm { Cty = "Oshodi", FiName = "Sikiru", LastName = "Ruka", Id = 500 },
                new CustomerVm { Cty = "Osogbo", FiName = "John", LastName = "Buhar", Id = 3 },
                new CustomerVm { Cty = "Kano", FiName = "Atiku", LastName = "Osibajo", Id = 4, OrderVm = new OrderVm { Name = "kmimoi" } }
            };
            swManual.Stop();
            Console.WriteLine($"Elapsed = {swManual.ElapsedMilliseconds} ms");

            // Print one of the results nicely
            Console.WriteLine("Mapped List (pretty):");
            Console.WriteLine(customerVmList.ToPrettyString());

            // Benchmark: ExIgniter ComplexMap
            Console.WriteLine("========== ExIgniter ComplexMap ================");
            var swComplex = Stopwatch.StartNew();
            var complexMapped = testCustomer.ComplexMap(new CustomerVm());
            swComplex.Stop();
            Console.WriteLine($"Elapsed = {swComplex.ElapsedMilliseconds} ms");

            Console.Read();
        }
    }