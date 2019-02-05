# ExIgniter.ObjectMapper
This  is is an intelligent, refactoring safe object mapping library that automatically maps objects to each other event when the property names are not exactly the same. 

Features:

Maps Simple Objects
Complex Objects
This very simple to use. Steps

Install ExIgniter.ObjectMapper from Nuget

sample code: var testCustomer = new Customer() { City = "Eko", FirstName = "Suraj", LastName = "Deji", ID = 1, Order = new Order() { Name = "Benz", Quantity = 1 } };

var mappedObject = testCustomer.Map(new CustomerVm());

That's all.
