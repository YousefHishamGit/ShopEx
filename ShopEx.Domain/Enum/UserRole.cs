namespace ShopEx.Domain.Enum
{
    /// <summary>
    /// دور المستخدم: Admin = موظف الـ HQ (كل الصلاحيات)، Employee = موظف فرع (صلاحيات محدودة).
    /// </summary>
    public enum UserRole
    {
        Admin = 1,
        Employee = 2
    }
}
