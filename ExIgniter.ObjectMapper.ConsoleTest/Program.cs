using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ExIgniter.ObjectMapper.ConsoleTest.Model;
using ExIgniter.ObjectMapper.ObjectMapper;

namespace ExIgniter.ObjectMapper.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Mapper.Initialize(config =>
            {
                config.CreateMap<Customer, CustomerVm>();
                config.CreateMap<Order, OrderVm>();
            });

            var testCustomer = new Customer() { City = "Eko", FirstName = "Suraj", LastName = "Deji", ID = 1, Order = new Order() { Name = "Benz", Quantity = 1 } };


            Console.WriteLine("==========Map================");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var a = testCustomer.Map(new CustomerVm());
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);



            Console.WriteLine("==========AutoMapper================");
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            var vmCustomer = Mapper.Map<CustomerVm>(testCustomer);
            sw1.Stop();
            Console.WriteLine("Elapsed={0}", sw1.Elapsed);



            Console.WriteLine("============Map2==============");
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            var r = testCustomer.Map2(new CustomerVm());
            sw2.Stop();
            Console.WriteLine("Elapsed={0}", sw2.Elapsed);


            Console.WriteLine("============Manual==============");
            Stopwatch sw3 = new Stopwatch();
            sw3.Start();
            var test = new CustomerVm
            {
                Cty = testCustomer.City,
                FiName = testCustomer.FirstName,
                LastName = testCustomer.LastName,
                Id = testCustomer.ID
            };
            var newList = new List<CustomerVm>()
            {
                new CustomerVm() {Cty = "Ibadan", FiName = "Ahmed", LastName = "Tunde", Id = 1},
                new CustomerVm() {Cty = "Oshodi", FiName = "Sikiru", LastName = "Ruka", Id = 500},
                new CustomerVm() {Cty = "osogbo", FiName = "John", LastName = "Buhar", Id = 3},
                new CustomerVm() {Cty = "Kano", FiName = "Atiku", LastName = "Osibajo", Id = 4}
            };
            sw3.Stop();
            Console.WriteLine("Elapsed={0}", sw3.Elapsed);


            Console.WriteLine("==========Complex================");
            Stopwatch sw4 = new Stopwatch();
            sw4.Start();
            var res = testCustomer.ComplexMap(new CustomerVm());
            sw4.Stop();
            Console.WriteLine("Elapsed={0}", sw4.Elapsed);
            Console.Read();
        }
    }
}
