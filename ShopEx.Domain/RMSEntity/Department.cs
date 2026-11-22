using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    public class Department
    {
        public int HQID { get; set; }
        public int ID { get; set; }

        [MaxLength(30)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(17)]
        public string code { get; set; } = string.Empty;

        [Timestamp]
        public byte[] DBTimeStamp { get; set; } = Array.Empty<byte>();
    }
}
