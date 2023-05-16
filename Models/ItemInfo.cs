using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EFDataAccessLibrary.Models
{
    public class ItemInfo
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public int LowInstance { get; set; }

        public int HighInstance { get; set; }

        public int Ql { get; set; }

        public int Type { get; set; }

        public int Instance { get; set; }

        [JsonIgnore]
        public Slot Slot { get; set; }

        public int SlotId { get; set; }
    }
}