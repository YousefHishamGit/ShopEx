using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    public class ReasonCode
    {
        public int ID { get; set; }
        public int HQID { get; set; }

        [MaxLength(25)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Description { get; set; } = string.Empty;

        [Timestamp]
        public byte[] DBTimeStamp { get; set; } = Array.Empty<byte>();

        public int Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
