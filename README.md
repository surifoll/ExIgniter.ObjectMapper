# ExIgniter.ObjectMapper
This  is is an intelligent, refactoring safe object mapping library that automatically maps objects to each other event when the property names are not exactly the same.

Features:

1. Maps Simple Objects
2. Complex Objects
3. Exclude Properties

Steps To use  ExIgniter.ObjectMapper

1. Install ExIgniter.ObjectMapper from Nuget

2. Sample code:

```cs
    var testCustomer = new Customer() {
        City = "Eko",
        FirstName = "Suraj",
        LastName = "Deji",
        ID = 1,
        Order = new Order() { Name = "Benz", Quantity = 1 }
 };
```
```cs
    var mappedObject = testCustomer.Map(new CustomerVm(), vm => new[] {
            nameof(vm.PropertyA), nameof(vm.PropertyB)
        }
    );
```

3. Enjoy
