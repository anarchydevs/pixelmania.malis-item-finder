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
   
    public ContainerProcessor() => _containerProcessing = 0;

    public void AddChange(Action change)
    {
        _queue.Enqueue(change);

        if (Interlocked.CompareExchange(ref _containerProcessing, 1, 0) == 0)
        {
            ProcessQueue();
        }
    }

    private async Task ProcessQueue()
    {
        while (_queue.TryDequeue(out Action change))
        {
            await Task.Delay(1);
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
    //private async Task ProcessQueue()
    //{
    //    while (_queue.TryDequeue(out Action change))
    //    {
    //        change.Invoke();
    //        await Task.Delay(50); // simulate processing time
    //    }

    //    Interlocked.Exchange(ref _containerProcessing, 0);
    //}
}