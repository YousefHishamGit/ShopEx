using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    [Table("PUBLIC_Store", Schema = "dbo")]
    public class Store
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(30)]
        public string? StoreCode { get; set; }

        [MaxLength(50)]
        public string? Region { get; set; }

        [MaxLength(50)]
        public string? Address1 { get; set; }

        [MaxLength(50)]
        public string? Address2 { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? Country { get; set; }

        [MaxLength(14)]
        public string? FaxNumber { get; set; }

        [MaxLength(14)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string? State { get; set; }

        [MaxLength(15)]
        public string? Zip { get; set; }

        public int? ParentStoreID { get; set; }

        public int? ScheduleHourMask1 { get; set; }
        public int? ScheduleHourMask2 { get; set; }
        public int? ScheduleHourMask3 { get; set; }
        public int? ScheduleHourMask4 { get; set; }
        public int? ScheduleHourMask5 { get; set; }
        public int? ScheduleHourMask6 { get; set; }
        public int? ScheduleHourMask7 { get; set; }

        public int? ScheduleMinute { get; set; }
        public int? RetryCount { get; set; }
        public int? RetryDelay { get; set; }

        public DateTime? LastUpdated { get; set; }

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        [MaxLength(50)]
        public string? AccountName { get; set; }

        [MaxLength(50)]
        public string? Password { get; set; }

        public int AutoID { get; set; }
    }
}
