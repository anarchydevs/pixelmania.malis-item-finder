using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MalisItemFinder
{
    public class Settings : Json<Settings>
    {
        public bool ItemPreview { get; set; }
        public bool ShowTutorial { get; set; }

        public Settings(string path) : base(path) => Load();

        public void Save() => Save(this);

        protected override void OnLoad(Settings loadedSettings)
        {
            ItemPreview = loadedSettings.ItemPreview;
            ShowTutorial = loadedSettings.ShowTutorial;
        }
    }

    public class Json<T>
    {
        protected readonly string _path;

        public Json(string path)
        {
            _path = path;
        }

        public virtual void Load()
        {
            if (File.Exists(_path))
            {
                T loadedSettings = JsonConvert.DeserializeObject<T>(File.ReadAllText(_path));
                OnLoad(loadedSettings);
            }
            else
            {
                Chat.WriteLine("File not found");
            }
        }

        public virtual void Save(T settings) => File.WriteAllText(_path, JsonConvert.SerializeObject(settings));

        protected virtual void OnLoad(T loadedSettings) { }
    }
}