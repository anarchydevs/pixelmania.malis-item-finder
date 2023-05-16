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
        private List<Item> _currentBankBags;
        private List<Item> _currentInvBags;
        private AutoResetInterval _dbUpdateTimer;
        private int _dbUpdateDelay => 125 * (Inventory.NumFreeSlots - 1);

        internal void Scan()
        {
            Chat.WriteLine("Initialized item scan");

            if (_state != BankScannerState.Idle)
            {
                Chat.WriteLine("Bank scan already in progress.");
                return;
            }

            if (!DynelManager.LocalPlayer.TryOpenBank())
            {
                Chat.WriteLine("No valid bank found. Cancelling bank scan.");
                return;
            }

            Game.OnUpdate += OnUpdate;

            _state = BankScannerState.InitOpeningBank;
        }

        public  void OnUpdate(object sender, float e)
        {
            if (_state == BankScannerState.Idle)
            {
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

        private  void FinishOpeningBank()
        {
            if (!Inventory.Bank.IsOpen)
                return;

            _remainingContainers = Inventory.Bank.Items.Where(x => x.UniqueIdentity.Type == IdentityType.Container).ToList();

            _state = BankScannerState.InitMovingToInventory;
        }

        private  void InitOpeningBank()
        {
            if (!DynelManager.LocalPlayer.TryOpenBank())
            {
                _state = BankScannerState.Idle;
                return;
            }

            _state = BankScannerState.FinishOpeningBank;
        }

        public  void InitMovingToInventory()
        {
            Chat.WriteLine($"Remaining bags to scan: {_remainingContainers.Count}");

            if (_remainingContainers.Count == 0)
            {
                _state = BankScannerState.Idle;
                Main.InventoryManager.RegisterInventoryAsync();
                return;
            }

            _currentBankBags = new List<Item>();

            _dbUpdateTimer = new AutoResetInterval(_dbUpdateDelay);

            foreach (Item item in _remainingContainers.Take(Inventory.NumFreeSlots - 1).ToList())
            {
                item.MoveToInventory();
                _currentBankBags.Add(item);
                _remainingContainers.Remove(item);
            }

            _state = BankScannerState.FinishMovingToInventory;
        }

        private  void FinishMovingToInventory()
        {
            _currentInvBags = new List<Item>();

            foreach (var s in _currentBankBags)
            {
                var bag = Inventory.Items.FirstOrDefault(x => x.UniqueIdentity == s.UniqueIdentity);

                if (bag == null)
                    return;
                else
                    _currentInvBags.Add(bag);
            }

            foreach (var bag in _currentInvBags)
            {
                bag.Use();
                bag.Use();
            }

            _dbUpdateTimer.Reset();

            _state = BankScannerState.InitMovingToBank;
        }

        private  void InitMovingToBank()
        {
            if (!_dbUpdateTimer.Elapsed)
                return;

            if (_currentInvBags.Any(s => !Inventory.Backpacks.FirstOrDefault(x => x.Identity == s.UniqueIdentity).IsOpen))
                return;

            foreach (var bag in _currentInvBags)
            {
                bag.MoveToContainer(_bankIdentity);
            }

            _dbUpdateTimer.Reset();
            _currentInvBags = null;

            _state = BankScannerState.FinishMovingToBank;
        }

        private  void FinishMovingToBank()
        {
            if (!_dbUpdateTimer.Elapsed)
                return;

            if (_currentBankBags.Any(c => Inventory.Items.FirstOrDefault(x => x.UniqueIdentity == c.UniqueIdentity) != null))
                return;

            _state = BankScannerState.InitMovingToInventory;
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
    FinishMovingToBank
}