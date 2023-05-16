using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EFDataAccessLibrary.Models
{
    public class ItemContainer
    {
        [Key]
        public int Id { get; set; }

        public ContainerId ContainerInstance { get; set; } = ContainerId.None;

        public ContainerId Root { get; set; } = ContainerId.None;

        public List<Slot> Slots { get; set; } = new List<Slot>();

        [JsonIgnore]
        public CharacterInventory CharacterInventory { get; set; }

        public int CharacterInventoryId { get; set; }
    }
}
