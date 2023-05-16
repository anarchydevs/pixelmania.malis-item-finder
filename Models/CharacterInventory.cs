using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EFDataAccessLibrary.Models
{
    public class CharacterInventory
    {
        [Key]
        public int Id { get; set; }

        public string CharName { get; set; }

        public List<ItemContainer> ItemContainers { get; set; } = new List<ItemContainer>();
    }
}

public enum ContainerId
{
    None = 0,
    WeaponPage = 101,
    ArmorPage = 102,
    ImplantPage = 103,
    Inventory = 104,
    Backpack = 107,
    SocialPage = 115,
    MailTerminal = 51059,
    Organization = 57002,
    Bank = 57005,
}