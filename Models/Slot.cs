using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EFDataAccessLibrary.Models
{
    public class Slot
    {
        [Key]
        public int Id { get; set; }

        public int SlotInstance { get; set; }

        public ItemInfo ItemInfo { get; set; }

        [JsonIgnore]
        public ItemContainer ItemContainer { get; set; }

        public int ItemContainerId { get; set; }
    }
}