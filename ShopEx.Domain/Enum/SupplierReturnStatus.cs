using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Enum
{
    public enum SupplierReturnStatus
    {
        Draft = 1,      // مسودة - لسه ماأثرتش على المخزن ولا الحساب
        Confirmed = 2,  // مؤكد - خرجت من المخزن وأثرت على حساب المورد
        Cancelled = 3
    }
}
