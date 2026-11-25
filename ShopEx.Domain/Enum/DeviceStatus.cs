namespace ShopEx.Domain.Enum
{
    /// <summary>
    /// حالة الجهاز المشترى للبيع
    /// </summary>
    public enum DeviceStatus
    {
        InStock = 1,  // متاح (لم يُبع بعد)
        Sold = 2      // تم بيعه
    }
}
