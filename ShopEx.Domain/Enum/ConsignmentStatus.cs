using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Enum
{
    public enum ConsignmentStatus
    {
        Open = 1,      // البضاعة لسه عند التاجر (كلها أو جزء منها) أو فيه فلوس متأخرة
        Closed = 2,    // كل البضاعة اتحاسبت (اتباعت أو اترجعت) والفلوس اتدفعت
        Cancelled = 3  // اتلغت قبل أي تسوية
    }
}
