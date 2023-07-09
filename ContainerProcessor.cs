using AOSharp.Core;
using AOSharp.Core.UI;
using MalisItemFinder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class ContainerProcessor
{
    private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
    private int _containerProcessing = 0;
    private bool _nextFrame = false;

    public ContainerProcessor()
    {
        _containerProcessing = 0;
        Game.OnUpdate += FrameLoop;
    }

    public void AddChange(Action change)
    {
        _queue.Enqueue(change);

        if (Interlocked.CompareExchange(ref _containerProcessing, 1, 0) == 0)
        {
            ProcessQueue(_nextFrame);
        }
    }


    private void FrameLoop(object sender, float deltaTime)
    {
        _nextFrame = !_nextFrame;
    }

    private async Task ProcessQueue(bool frame)
    {
        while (_nextFrame == frame)
        {
            await Task.Yield();
        }

        while (_queue.TryDequeue(out Action change))
        {
            change.Invoke();

            while (true)
            {
                try
                {
                    await Main.InventoryManager.DbSaveAsync();
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                }
                catch (DbUpdateException)
                {
                }
            }
        }

        Interlocked.Exchange(ref _containerProcessing, 0);
    }
}