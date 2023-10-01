using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.Misc;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class ItemScanner
    {
        private BankScannerState _state = BankScannerState.Idle;
        private List<Item> _remainingContainers = new List<Item>();
        private Identity _bankIdentity = new Identity(IdentityType.Bank, DynelManager.LocalPlayer.Identity.Instance);
        private List<Identity> _currentBankBags;
        private List<Identity> _currentInvBags;
  
        internal void Scan()
        {
            Chat.WriteLine("Performing Deep Scan");

            if (_state != BankScannerState.Idle)
            {
                Chat.WriteLine("Deep scan already in progress.");
                return;
            }

            if (!Utils.TryOpenBank())
            {
                Chat.WriteLine("Bank not open. Scanning inventory / gmi.");
                ScanInventoryAndGmi();
                return;
            }

            if (Inventory.NumFreeSlots < 2)
            {
                Chat.WriteLine("Need at least two inventory slots.");
                return;
            }

            _state = BankScannerState.InitOpeningBank;
            Game.OnUpdate += OnUpdate;
        }

        public void OnUpdate(object sender, float e)
        {
            if (DatabaseProcessor.IsOccupied())
                return;

            if (_state == BankScannerState.Idle)
            {
                ScanInventoryAndGmi();
                Game.OnUpdate -= OnUpdate;
                return;
            }

            switch (_state)
            {
                case BankScannerState.InitOpeningBank:
                    InitOpeningBank();
                    break;
                case BankScannerState.FinishOpeningBank:
                    FinishOpeningBank();
                    break;
                case BankScannerState.InitMovingToInventory:
                    InitMovingToInventory();
                    break;
                case BankScannerState.FinishMovingToInventory:
                    FinishMovingToInventory();
                    break;
                case BankScannerState.InitMovingToBank:
                    InitMovingToBank();
                    break;
                case BankScannerState.FinishMovingToBank:
                    FinishMovingToBank();
                    break;
            }
        }

        private void FinishOpeningBank()
        {
            _remainingContainers = Inventory.Bank.Items.Where(x => x.UniqueIdentity.Type == IdentityType.Container).ToList();

            _state = BankScannerState.InitMovingToInventory;
        }

        private  void InitOpeningBank()
        {
            if (Inventory.Bank.IsOpen || Utils.TryOpenBank())
                _state = BankScannerState.FinishOpeningBank;
            else
                _state = BankScannerState.Idle;
        }

        public  void InitMovingToInventory()
        {
            Chat.WriteLine($"Remaining bags to scan: {_remainingContainers.Count}");

            if (_remainingContainers.Count == 0)
            {
                _state = BankScannerState.Idle;
                Chat.WriteLine($"Registering inventory with containers.");

                return;
            }

            _currentBankBags = new List<Identity>();

            foreach (Item item in _remainingContainers.Take(Inventory.NumFreeSlots - 1).ToList())
            {
                item.MoveToInventory();
                _currentBankBags.Add(item.UniqueIdentity);
                _remainingContainers.Remove(item);
            }

            _state = BankScannerState.FinishMovingToInventory;
        }

        private  void FinishMovingToInventory()
        {
            var items = Inventory.Items.Where(x => _currentBankBags.Contains(x.UniqueIdentity));

            if (items.Count() != _currentBankBags.Count())
                return;

            _currentInvBags = new List<Identity>();

            items.PeekBags();
            _currentInvBags.AddRange(items.Select(x => x.UniqueIdentity));

            _state = BankScannerState.InitMovingToBank;
        }

        private  void InitMovingToBank()
        {
            var bags = Inventory.Backpacks.Where(x => _currentInvBags.Contains(x.Identity));

            if (bags.Count() == 0 || bags.Any(x => !x.IsOpen))
                return;

            foreach (var bag in Inventory.Items.Where(x => _currentInvBags.Contains(x.UniqueIdentity)))
                bag.MoveToContainer(_bankIdentity);

            _state = BankScannerState.FinishMovingToBank;

        }

        private void FinishMovingToBank()
        {
            if (Inventory.Bank.Items.FirstOrDefault(x => x.UniqueIdentity == _currentBankBags.LastOrDefault()) == null)
                return;

            _currentInvBags = null;
            _state = BankScannerState.InitMovingToInventory;
        }

        private void ScanInventoryAndGmi()
        {
            Main.Database.RegisterInventory();
            Inventory.Items.PeekBags();
            Main.Database.RegisterGMI();
        }
    }
}

public enum BankScannerState
{
    Idle,
    InitOpeningBank,
    FinishOpeningBank,
    InitMovingToInventory,
    FinishMovingToInventory,
    InitMovingToBank,
    FinishMovingToBank,
}