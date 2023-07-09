using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.Misc;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MalisItemFinder
{
    public class ItemTracker
    {
        private List<ItemTrackModel> _sourceMap = new List<ItemTrackModel>();
        private List<ItemTrackModel> _targetMap = new List<ItemTrackModel>();
        private InventoryManager _invManager;

        public ItemTracker(InventoryManager invManager)
        {
            _invManager = invManager;
        }

        internal void OnN3MessageSent(object sender, N3Message n3Msg)
        {
            switch (n3Msg.N3MessageType)
            {
                case (N3MessageType.ClientContainerAddItem):
                    OnClientContainerAddItem(n3Msg as ClientContainerAddItem);
                    break;
                case (N3MessageType.ClientMoveItemToInventory):
                    OnClientMoveItemToInventory(n3Msg as ClientMoveItemToInventory);
                    break;
            }
        }

        internal void OnN3MessageReceived(object sender, N3Message n3Msg)
        {
            switch (n3Msg.N3MessageType)
            {
                case N3MessageType.ContainerAddItem:
                    OnContainerAddItemReceived(n3Msg as ContainerAddItem);
                    break;
                case N3MessageType.Trade:
                    OnTradeMessage(n3Msg as TradeMessage);
                    break;
            }
        }

        private void OnTradeMessage(TradeMessage msg)
        {
            if (msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (msg.Action != TradeAction.Complete)
                return;

            _invManager.RemoveMissingBagsAsync();
        }

        private void OnContainerAddItemReceived(ContainerAddItem msg)
        {
            if (msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            var source = _sourceMap.FirstOrDefault(x => x.PacketIdentity == msg.Source);
            var target = _targetMap.FirstOrDefault(x => x.PacketIdentity == msg.Target);

            if (source == null || target == null)
                return;

            _invManager.RegisterContainer(source.ContainerId);
            _invManager.RegisterContainer(target.ContainerId);

            _sourceMap.Remove(source);
            _targetMap.Remove(target);
        }

        private void OnClientMoveItemToInventory(ClientMoveItemToInventory n3Msg) => MapContainers(n3Msg.SourceContainer, DynelManager.LocalPlayer.Identity);

        private void OnClientContainerAddItem(ClientContainerAddItem n3Msg) => MapContainers(n3Msg.Source, n3Msg.Target);

        private void MapContainers(Identity n3MsgSource, Identity n3MsgTarget)
        {
            var sourceIdentity = SetCommonIdentity(n3MsgSource);
            var targetIdentity = SetCommonIdentity(n3MsgTarget);

            try
            {
                _sourceMap.Add(new ItemTrackModel { PacketIdentity = n3MsgSource, ContainerId = sourceIdentity == IdentityType.Container ? (ContainerId)n3MsgSource.SlotHandleToBackpack().Identity.Instance : (ContainerId)sourceIdentity.Type });
                _targetMap.Add(new ItemTrackModel { PacketIdentity = n3MsgTarget, ContainerId = targetIdentity == IdentityType.Container ? (ContainerId)targetIdentity.Instance : (ContainerId)targetIdentity.Type });
            }
            catch
            {
            }
        }

        public Identity SetCommonIdentity(Identity identity)
        {
            Identity commonIdentity = identity;

            switch (commonIdentity.Type)
            {
                case IdentityType.SimpleChar:
                case IdentityType.Inventory:
                case IdentityType.ArmorPage:
                case IdentityType.WeaponPage:
                case IdentityType.ImplantPage:
                case IdentityType.SocialPage:
                    commonIdentity.Type = IdentityType.Inventory;
                    break;
                case IdentityType.BankByRef:
                case IdentityType.Bank:
                    commonIdentity.Type = IdentityType.Bank;
                    break;
                default:
                    commonIdentity.Type = IdentityType.Container;
                    break;
            }

            return commonIdentity;
        }
    }
    public class ItemTrackModel
    {
        public Identity PacketIdentity;
        public ContainerId ContainerId;
    }
}
