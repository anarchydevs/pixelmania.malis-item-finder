using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.Misc;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class ItemFinder
    {
        private ItemFinderState _state = ItemFinderState.Idle;
        private Slot _itemToFind;
        private List<Item> _activeItemList;

        public ItemFinder(Slot itemToFind)
        {
            _itemToFind = itemToFind;
            Game.OnUpdate += OnUpdate;
            Query();
        }

        internal void Query()
        {
            try
            {
                if (Inventory.NumFreeSlots == 0)
                {
                    Chat.WriteLine("Need at least one free inventory slot to perform item find.");
                    return;
                }

                if (_state != ItemFinderState.Idle)
                {
                    Chat.WriteLine("Item find is already in progress.");
                    return;
                }

                if (_itemToFind.ItemInfo.Slot.ItemContainer.CharacterInventory.CharName != DynelManager.LocalPlayer.Name)
                {
                    Chat.WriteLine("Item is registered on a different character.");
                    return;
                }

                switch (_itemToFind.ItemInfo.Slot.ItemContainer.Root)
                {
                    //case ContainerId.Inventory:
                    //    Chat.WriteLine("Item is already located in your inventory.");
                    //    return;
                    case ContainerId.ArmorPage:
                    case ContainerId.WeaponPage:
                    case ContainerId.SocialPage:
                    case ContainerId.ImplantPage:
                        Chat.WriteLine("You are currently wearing this item.");
                        return;
                    case ContainerId.GMI:
                        Chat.WriteLine("Item is located in your GMI.");
                        return;

                }

                _state = ItemFinderState.DetermineItemSource;
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine("Query");
            }

        }

        internal void OnUpdate(object sender, float e)
        {
            if (_state == ItemFinderState.Idle)
            {
                Chat.WriteLine("Item scan finished.");
                Game.OnUpdate -= OnUpdate;
                return;
            }

            switch (_state)
            {
                case ItemFinderState.DetermineItemSource:
                    DetermineItemSourceState(_itemToFind);
                    break;
                case ItemFinderState.ItemIsInBankState:
                    ItemIsInBankState();
                    break;
                case ItemFinderState.MoveItemFromBankToInventoryState:
                    MoveItemFromBankToInventoryState();
                    break;
                case ItemFinderState.InitBankItemIsInBankInventoryState:
                    InitBankItemIsInBankInventoryState();
                    break;
                case ItemFinderState.InitBankItemIsInBankContainerState:
                    InitBankItemIsInBankContainerState();
                    break;
                case ItemFinderState.InitItemIsInInventory:
                    InitItemIsInInventory();
                    break;
                case ItemFinderState.InitItemIsInInventoryContainerState:
                    InitItemIsInInventoryContainerState();
                    break;
            }
        }

        private void DetermineItemSourceState(Slot itemLookup)
        {
            switch (itemLookup.ItemInfo.Slot.ItemContainer.Root)
            {
                case ContainerId.Inventory:
                case ContainerId.WeaponPage:
                case ContainerId.ImplantPage:
                case ContainerId.SocialPage:
                    _activeItemList = Inventory.Items;
                    _state = ItemFinderState.InitItemIsInInventory;
                    break;
                case ContainerId.Bank:
                    _state = Utils.TryOpenBank() ? ItemFinderState.ItemIsInBankState : _state = ItemFinderState.Idle;
                    break;
                default:
                    Chat.WriteLine("This shouldn't happen (DetermineItemSourceState)");
                    break;
            }
        }

        private void InitItemIsInInventoryContainerState()
        {
            var itemSourceBag = Inventory.Backpacks.FirstOrDefault(x => x.Identity.Instance == (int)_itemToFind.ItemInfo.Slot.ItemContainer.ContainerInstance);

            if (itemSourceBag == null)
                return;

            if (!itemSourceBag.IsOpen)
                return;

            if (!itemSourceBag.Items.Find(_itemToFind, out Item item))
                return;

            item.MoveToInventory();
            _state = ItemFinderState.Idle;
        }

        private void InitItemIsInInventory()
        {
            var itemSourceBag = Inventory.Backpacks.FirstOrDefault(x => x.Identity.Instance == (int)_itemToFind.ItemInfo.Slot.ItemContainer.ContainerInstance);

            if (itemSourceBag == null)
            {
                _state = ItemFinderState.Idle;
                return;
            }

            if (!itemSourceBag.IsOpen)
            {
                var item = Inventory.Items.FirstOrDefault(x => x.UniqueIdentity.Instance == (int)_itemToFind.ItemInfo.Slot.ItemContainer.ContainerInstance);
                item.Use();
            }

            _state = ItemFinderState.InitItemIsInInventoryContainerState;
        }

        private void ItemIsInBankState()
        {
            if (!Inventory.Bank.IsOpen)
                return;

            _activeItemList = Inventory.Bank.Items;
            _state = ItemFinderState.MoveItemFromBankToInventoryState;
        }

        private void MoveItemFromBankToInventoryState()
        {
            switch (_itemToFind.ItemContainer.ContainerInstance)
            {
                case ContainerId.Bank:
                    _state = ItemFinderState.InitBankItemIsInBankInventoryState;
                    break;
                default:
                    _state = ItemFinderState.InitBankItemIsInBankContainerState;
                    break;
            }     
        }

        private void InitBankItemIsInBankContainerState()
        {
            var itemSource = _activeItemList.FirstOrDefault(x => x.UniqueIdentity == new Identity(IdentityType.Container, (int)_itemToFind.ItemContainer.ContainerInstance));

            if (itemSource == null)
            {
                Chat.WriteLine("Could not find item source container inside bank. Exiting.");
                _state = ItemFinderState.Idle;
                return;
            }

            Chat.WriteLine($"Moving bag ({_itemToFind.ItemContainer.ContainerInstance}) to from bank to inventory.");

            itemSource.MoveToInventory(); 
            
            _state = ItemFinderState.InitItemIsInInventoryContainerState;
        }

        private void InitBankItemIsInBankInventoryState()
        {
            if (!_activeItemList.Find(_itemToFind, out Item item))
            {
                Chat.WriteLine("Could not find item inside bank. Exiting.");
                _state = ItemFinderState.Idle;
                return;
            }

            item.MoveToInventory(); 
            
            _state = ItemFinderState.Idle;
        }
    }
}

public enum ItemFinderState
{
    Idle,

    DetermineItemSource,

    ItemIsInBankState,

    InitBankItemIsInBankInventoryState,
    InitBankItemIsInBankContainerState,

    MoveItemFromBankToInventoryState,

    InitItemIsInInventory,
    InitItemIsInInventoryContainerState,

    InitScan,
}