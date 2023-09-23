using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using Force.DeepCloner;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisItemFinder
{
    public static class EventHandler
    {
        public static int Counter = 0;
        public static EventHandler<N3Message> OnN3MessageReceived;
        public static EventHandler<OnContainerOpenedArgs> OnContainerOpened;
        private static List<DelayedEvent> _events = new List<DelayedEvent>();

        internal static void Load()
        {
            Network.N3MessageReceived += N3MessageReceivedEvent;
            Inventory.ContainerOpened += OnContainerOpenedEvent;
            Game.OnUpdate += OnUpdate;
        }

        private static void N3MessageReceivedEvent(object sender, N3Message n3Msg)
        {
            switch (n3Msg.N3MessageType)
            {
                case N3MessageType.Trade:
                case N3MessageType.ContainerAddItem:
                case N3MessageType.MarketSend:
                    _events.Add(new N3MessageEvent(n3Msg, 15));
                    break;
                case N3MessageType.CharacterAction:
                    var charMsg = ((CharacterActionMessage)n3Msg).Action;
                    if (charMsg == CharacterActionType.DeleteItem || charMsg == CharacterActionType.SplitItem)
                        _events.Add(new N3MessageEvent(n3Msg, 5));
                    break;
                case N3MessageType.GenericCmd:
                    var genericMsg = ((GenericCmdMessage)n3Msg);
                    if (genericMsg.Action == GenericCmdAction.Use && DynelManager.Find(genericMsg.Target, out Dynel dynel) && dynel.Name == "Market Terminal")
                        _events.Add(new N3MessageEvent(n3Msg, 5));
                    break;
            }
        }

        internal static void OnContainerOpenedEvent(object sender, Container container)
        {
            _events.Add(new OnContainerOpenedArgs(container.Identity, container.Items.DeepClone(), 0));
        }

        private static void OnUpdate(object sender, float deltaTime)
        {
            foreach (var ev in _events.ToList())
            {
                ev.Tick();

                if (ev.Elapsed)
                {
                    ev.Trigger();
                    _events.Remove(ev);
                }
            }
        }
    }

    public class DelayedEvent
    {
        public int FrameDelay;
        public bool Elapsed => FrameDelay <= 0;

        public DelayedEvent(int frameDelay)
        {
            FrameDelay = frameDelay;
        }

        internal void Tick() => FrameDelay--;

        public virtual void Trigger() { }
    }

    public class N3MessageEvent : DelayedEvent
    {
        public N3Message N3Message;

        public N3MessageEvent(N3Message n3Message, int frameDelay) : base(frameDelay)
        {
            N3Message = n3Message;
        }

        public override void Trigger()
        {
            EventHandler.OnN3MessageReceived.Invoke(DynelManager.LocalPlayer.Identity.Instance, N3Message);
        }
    }

    public class OnContainerOpenedArgs : DelayedEvent
    {
        public List<Item> Items;
        public Identity Container;

        public OnContainerOpenedArgs(Identity container, List<Item> items, int frameDelay) : base(frameDelay)
        {
            Items = items;
            Container = container;
        }

        public override void Trigger()
        {
            EventHandler.OnContainerOpened.Invoke(DynelManager.LocalPlayer.Identity.Instance, this);
        }
    }

}