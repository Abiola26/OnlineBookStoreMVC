﻿namespace OnlineBookStoreMVC.Entities
{
    public class Address : BaseEntity
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }

        // Navigation properties
        public User User { get; set; }

        // Collection of orders associated with this address
        public ICollection<Order> Orders { get; set; }
    }

}
