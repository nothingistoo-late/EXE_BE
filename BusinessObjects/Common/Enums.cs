namespace BusinessObjects.Common
{
    public enum Gender  
    {
        Other,
        Male,
        Female
    }
    public enum RoleType { Customer, Admin, Staff}

    public enum OrderStatus { Cart, Pending, Processing, Completed, Cancelled }
    public enum DeliveryMethod { Standard, Express }
    public enum PaymentMethod { VNPay, CashOnDelivery }
    public enum SubscriptionPackageFrequency { Weekly, Monthly }
    public enum CustomerSubscriptionStatus { Inactive, Active}
    public enum PaymentStatus { Pending, Paid, Failed }

}

