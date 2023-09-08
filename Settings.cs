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
    public class Settings
    {
        public bool ItemPreview;
        public bool ShowTutorial;

        [JsonIgnore]
        private string _path;

        public Settings(string path)
        {
            _path = path;
        }

        internal void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    Settings settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_path));
                    ItemPreview = settings.ItemPreview;
                    ShowTutorial = settings.ShowTutorial;
                    return;
                }

                string directoryPath = Path.GetDirectoryName(_path);

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                ItemPreview = true;
                ShowTutorial = true;

            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        internal void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_path, json);
        }
    }
}