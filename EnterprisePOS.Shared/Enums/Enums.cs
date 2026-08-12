namespace EnterprisePOS.Shared.Enums
{
    public enum Role
    {
        RestaurantAdmin,
        Manager,
        Cashier,
        Kitchen
    }

    public enum PaymentMethod
    {
        Cash,
        Card,
        Split,
        DigitalWallet
    }

    public enum OrderType
    {
        DineIn,
        Takeaway,
        Delivery
    }
}
