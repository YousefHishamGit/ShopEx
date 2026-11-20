namespace ShopEx.Domain.Enum
{
    /// <summary>
    /// طريقة دفع وسداد المصروف
    /// </summary>
    public enum ExpensePaymentMethod
    {
        Cash = 1,              // نقدي (كاش)
        BankTransfer = 2,      // تحويل بنكي / انستاباي
        CreditCard = 3,        // فيزا / كارت
        ElectronicWallet = 4,  // محفظة إلكترونية (فودافون كاش، إلخ)
        Cheque = 5             // شيك
    }
}
