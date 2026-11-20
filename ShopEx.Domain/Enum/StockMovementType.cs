using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Enum
{
    public enum StockMovementType
    {
        OpeningBalance = 1,   
        PurchaseIn = 2,       
        SupplierReturnOut = 3,
        SaleOut = 4,          
        CustomerReturnIn = 5, 
        AdjustmentIn = 6,     
        AdjustmentOut = 7,    
        TransferIn = 8,       
        TransferOut = 9,
        ConsignmentOut = 10,   
        ConsignmentReturnIn = 11 
    }
}
