namespace ExIgniter.ObjectMapper.ConsoleTest.Model
{
    public class Customer
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string City { get; set; }
        public Order Order { get; set; }    
    }
    public class CustomerVm
    {
        public int Id { get; set; }
        public string FiName { get; set; }
        public string LastName { get; set; }
        public string Cty { get; set; }
        public OrderVm OrderVm { get; set; }

    }

    public class Order
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
    public class OrderVm
    {
        public string Name { get; set; }
        public int Qty { get; set; }
    }
}