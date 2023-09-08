using AOSharp.Core;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using MalisItemFinder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


public static class DatabaseProcessor
{
    private static readonly ConcurrentQueue<DatabaseAction> _dbAction = new ConcurrentQueue<DatabaseAction>();
    private static bool _isSaving = false;
    private static bool _resetTableView = false;

    internal static void Load()
    {
        Game.OnUpdate += OnUpdate;
    }

    private static void OnUpdate(object sender, float e)
    {
        if (_resetTableView)
        {
            Main.MainWindow.Refresh();
            _resetTableView = false;
        }

        if (_isSaving)
            return;

        if (_dbAction.Count == 0)
            return;

        _isSaving = true;

        Task.Run(() => ProcessQueue());
    }

    internal static int QueueCount() => _dbAction.Count;

    internal static bool IsOccupied() => _dbAction.Count != 0 || _isSaving;

    internal static void AddChange(DatabaseAction change) => _dbAction.Enqueue(change);

    private static async Task ProcessQueue()
    {
        using (var db = new SqliteContext(Main.Database.Path))
        {
            CharacterInventory localPlayerInventory = db.Inventories.Where(x => x.CharName == DynelManager.LocalPlayer.Name)
                 .Include(x => x.ItemContainers)
                 .ThenInclude(x => x.Slots)
                 .ThenInclude(x => x.ItemInfo)
                 .FirstOrDefault();

            using (var unitOfWork = new UnitOfWork(db))
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        while (_dbAction.Count > 0)
                        {
                            if (!_dbAction.TryDequeue(out var action))
                                break;

                            action.Action.Invoke(db, localPlayerInventory);
                        }

                        await unitOfWork.CommitAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Chat.WriteLine(ex.Message);
                        Chat.WriteLine("Process Queue encountered a problem. Rolling back to previous state.");
                    }
                }
            }
        }
        _isSaving = false;
        _resetTableView = true;
    }
}

public class DatabaseAction
{
    public string Name;
    public Action<SqliteContext, CharacterInventory> Action;
}

public class UnitOfWork : IDisposable
{
    private readonly SqliteContext _db;
    private bool _disposed = false;

    public UnitOfWork(SqliteContext db)
    {
        _db = db;
    }

    public void Commit()
    {
        _db.SaveChanges();
    }

    public async Task CommitAsync()
    {
        await _db.SaveChangesAsync();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _db.Dispose();
            _disposed = true;
        }
    }
}