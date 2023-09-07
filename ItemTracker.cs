using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.GMI;
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
    public static class ItemTracker
    {
        private static List<ItemTrackModel> _sourceMap = new List<ItemTrackModel>();
        private static List<ItemTrackModel> _targetMap = new List<ItemTrackModel>();
        private static List<ContainerId> _tradeTrack = new List<ContainerId>();
        private static List<ContainerId> _charActionTrack = new List<ContainerId>();

        internal static void OnTeleportStarted(object sender, EventArgs e)
        {
            _sourceMap.Clear();
            _sourceMap.Clear();
            _tradeTrack.Clear();
            _charActionTrack.Clear();
        }

        internal static void OnN3MessageSent(object sender, N3Message n3Msg)
        {
            switch (n3Msg.N3MessageType)
            {
                case N3MessageType.ClientContainerAddItem:
                    OnClientContainerAddItem(n3Msg as ClientContainerAddItem);
                    break;
                case N3MessageType.ClientMoveItemToInventory:
                    OnClientMoveItemToInventory(n3Msg as ClientMoveItemToInventory);
                    break;
                case N3MessageType.CharacterAction:
                    OnCharacterActionSent(n3Msg as CharacterActionMessage);
                    break;
            }
        }

        internal static void OnContainerOpened(object sender, OnContainerOpenedArgs args)
        {
            try
            {
                switch (args.Container.Type)
                {
                    case IdentityType.Bank:
                        Main.Database.RegisterBank();
                        break;
                    default:
                        Main.Database.RegisterContainerItems((ContainerId)args.Container.Instance, args.Items);
                        break;
                }
                //Chat.WriteLine($"{args.Container} updated.");
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine("OnContainerOpened");
            }
        }
        internal static void OnN3MessageReceived(object sender, N3Message n3Msg)
        {
            switch (n3Msg.N3MessageType)
            {
                case N3MessageType.ContainerAddItem:
                    OnContainerAddItemReceived(n3Msg as ContainerAddItem);
                    break;
                case N3MessageType.CharacterAction:
                    OnCharacterActionDeleteReceived(n3Msg as CharacterActionMessage);
                    break;
                case N3MessageType.Trade:
                    OnTradeMessage(n3Msg as TradeMessage);
                    break;
                case N3MessageType.MarketSend:
                    OnMarketSendMessage(n3Msg as MarketSendMessage);
                    break;
                case N3MessageType.GenericCmd:
                    OnGenericCmdMessage(n3Msg as GenericCmdMessage);
                    break;
            }
        }

        private static void OnCharacterActionSent(CharacterActionMessage n3Msg)
        {
            Identity n3MsgSource = n3Msg.Target;
            Identity sourceIdentity = SetCommonIdentity(n3MsgSource);
            ItemTrackModel trackModel = new ItemTrackModel 
            { 
                PacketIdentity = n3MsgSource, 
                ContainerId = sourceIdentity == IdentityType.Container ? (ContainerId)n3MsgSource.SlotHandleToBackpack().Identity.Instance : (ContainerId)sourceIdentity.Type 
            };
          
            if (n3Msg.Action == CharacterActionType.DeleteItem)
            {
                _sourceMap.Add(trackModel);
                _charActionTrack = GetContainers(sourceIdentity.Type).ToList();
            }
            else if (n3Msg.Action == CharacterActionType.SplitItem)
            {
                Main.Database.RegisterContainer(trackModel.ContainerId);
            }
        }

        private static void OnCharacterActionDeleteReceived(CharacterActionMessage n3Msg)
        {
            if (n3Msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (n3Msg.Action != CharacterActionType.DeleteItem)
                return;

            var source = _sourceMap.FirstOrDefault(x => x.PacketIdentity == n3Msg.Target);

            if (source == null)
                return;

            List<ContainerId> missingContainers = _charActionTrack.Except(GetContainers((IdentityType)source.ContainerId)).ToList();

            Main.Database.RemoveMissingBags(source.ContainerId, missingContainers);
            Main.Database.RegisterContainer(source.ContainerId);
        }

        private static IEnumerable<ContainerId> GetContainers(IdentityType identity)
        {
            if (identity == IdentityType.Inventory)
                return Inventory.Backpacks.Select(x => (ContainerId)x.Identity.Instance).ToList();
           else if (identity == IdentityType.Bank)
                return Inventory.Bank.Backpacks.Select(x => (ContainerId)x.Identity.Instance).ToList();

            return new List<ContainerId>();
        }

        private static void OnTradeMessage(TradeMessage msg)
        {
            if (msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (msg.Action == TradeAction.Open)
            {
                _tradeTrack = GetContainers(IdentityType.Inventory).ToList();
                return;
            }

            if (msg.Action == TradeAction.Decline)
            {
                _tradeTrack.Clear();
            }

            if (msg.Action == TradeAction.Complete)
            {
                List<ContainerId> missingContainers = _tradeTrack.Except(GetContainers(IdentityType.Inventory)).ToList();
                Main.Database.RemoveMissingBags(ContainerId.Inventory, missingContainers);
                Main.Database.RegisterInventory();
                _tradeTrack.Clear();

                return;
            }
        }

        private static void OnGenericCmdMessage(GenericCmdMessage msg)
        {
            if (msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (msg.Action != GenericCmdAction.Use)
                return;

            Main.Database.RegisterGMI();
        }

        private static void OnMarketSendMessage(MarketSendMessage msg)
        {
            if (msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (msg.Sender != DynelManager.LocalPlayer.Identity)
                return;

            Main.Database.RegisterInventory();
        }

        private static void OnContainerAddItemReceived(ContainerAddItem msg)
        {
            if (msg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (msg.Source == IdentityType.OverflowWindow)
            {
                if (msg.Target == IdentityType.OverflowWindow || msg.Target == DynelManager.LocalPlayer.Identity)
                {
                    Main.Database.RegisterInventory();
                    return;
                }
            }

            var source = _sourceMap.FirstOrDefault(x => x.PacketIdentity == msg.Source);
            var target = _targetMap.FirstOrDefault(x => x.PacketIdentity == msg.Target);

            if (source == null || target == null)
                return;

            Main.Database.RegisterContainer(source.ContainerId);
            Main.Database.RegisterContainer(target.ContainerId);

            _sourceMap.Remove(source);
            _targetMap.Remove(target);
        }

        private static void OnClientMoveItemToInventory(ClientMoveItemToInventory n3Msg) => MapContainers(n3Msg.SourceContainer, DynelManager.LocalPlayer.Identity);

        private static void OnClientContainerAddItem(ClientContainerAddItem n3Msg) => MapContainers(n3Msg.Source, n3Msg.Target);

        private static void MapContainers(Identity n3MsgSource, Identity n3MsgTarget)
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

        private static Identity SetCommonIdentity(Identity identity)
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
