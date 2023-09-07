using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using EFDataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisItemFinder
{
    public class SearchView
    {
        public View Root;

        private TextView _searchView;
        private string _searchViewTextCache;

        private TextView _containerQueue;
        private string _containerQueueCache;

        private ComboBox _comboBoxView;
        private bool _comboBoxLoaded = false;
        private string _comboBoxTextCache = "";

        private char[] _separators = new char[2] { ',', '|' };
        private List<string> _keywords = new List<string>();

        private Dictionary<FilterCriteria, List<SearchCriteria>> _criterias = new Dictionary<FilterCriteria, List<SearchCriteria>> 
        {
            { FilterCriteria.Ql, new List<SearchCriteria>()},
            { FilterCriteria.Id, new List<SearchCriteria>()},
            { FilterCriteria.Location, new List<SearchCriteria>()}
        };

        public SearchView(View searchRootView)
        {
            Root = View.CreateFromXml($"{Main.PluginDir}\\UI\\Views\\SearchView.xml");

            if (Root.FindChild("SearchBar", out _searchView))
            {
                _searchViewTextCache = _searchView.Text;
            }

            if (Root.FindChild("SearchButton", out Button search))
            {
                search.Clicked = SearchClick;
                search.SetAllGfx(TextureId.SearchButton);
            }

            if (Root.FindChild("ScanButton", out Button scan))
            {
                scan.Clicked = ScanClick;
                scan.SetAllGfx(TextureId.ScanButton);
            }

            if (Root.FindChild("Dropdown", out _comboBoxView))
            {

                _comboBoxView.SetText("All");
            }

            if (Root.FindChild("ContainerQueue", out _containerQueue))
            {
                _containerQueue.Text = "0";
            }

            searchRootView.AddChild(Root, true);
        }

        public string GetCharacter() => _comboBoxView.GetText();

        public Dictionary<FilterCriteria, List<SearchCriteria>> GetCriterias() => _criterias;
       

        private void ParseText(string text)
        {
            if (text == null)
                return;

            string[] filterSections = text.ToLower().Split(text.Contains(_separators[0]) ? _separators[0] : _separators[1]);
          
            if (filterSections.Length == 0)
                return;

            _keywords = filterSections[0].Split(' ').ToList();
            filterSections = filterSections.Select(x => x.Replace(" ", "")).ToArray();
          
            if (filterSections.Length == 1)
                return;

            foreach (var filterSection in filterSections.Skip(1))
            {
                string[] filterParts = filterSection.Trim().Split(':');

                if (filterParts.Length == 2)
                {
                    string filterName = filterParts[0].Trim();
                    string filterExpression = filterParts[1].Trim();

                    var numericalCriteriaTypes = new List<Type>
                    {
                        typeof(RangeCriteria),
                        typeof(MinusCriteria),
                        typeof(PlusCriteria),
                        typeof(EqualCriteria)
                    };

                    if (filterName.Equals("ql"))
                    {
                        foreach (Type criteriaType in numericalCriteriaTypes)
                            SetCriteria(_criterias[FilterCriteria.Ql], (SearchCriteria)Activator.CreateInstance(criteriaType, filterExpression));
                    }

                    if (filterName.Equals("id"))
                    {
                        foreach (Type criteriaType in numericalCriteriaTypes)
                            SetCriteria(_criterias[FilterCriteria.Id], (SearchCriteria)Activator.CreateInstance(criteriaType, filterExpression));
                    }

                    if (filterName.Equals("loc"))
                    {
                        SetCriteria(_criterias[FilterCriteria.Location], new LocationCriteria(filterExpression));
                    }
                    // Add more conditions for other filter types as needed
                }
            }

        }

        private void SetCriteria<T>(List<SearchCriteria> criteriaList, T newCriteria) where T : SearchCriteria
        {
            if (newCriteria.IsActive)
            {
                var existingCriteria = criteriaList.FirstOrDefault(x => x is T);
             
                if (existingCriteria != null)
                    criteriaList.Remove(existingCriteria);

                criteriaList.Add(newCriteria);
            }
        }

        public List<string> GetKeywords() => _keywords;
       
        private void OnComboBoxUpdate()
        {
            if (_comboBoxLoaded)
                return;

            try
            {
                var chars = Main.Database.GetAllCharacters();

                if (chars == null)
                    return;

                if (!chars.Contains(DynelManager.LocalPlayer.Name))
                    return;

                _comboBoxView.AppendItem(0, "All");

                for (int i = 0; i < chars.Count; i++)
                    _comboBoxView.AppendItem(i + 1, chars[i]);

                _comboBoxLoaded = true;
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.Message);
                Chat.WriteLine("OnComboBoxUpdate");
            }
        }

        public bool OnComboBoxChange() => _comboBoxLoaded && (_comboBoxTextCache != (_comboBoxTextCache = _comboBoxView.GetText()));

        public void OnUpdate()
        {
            OnComboBoxUpdate();
            OnContainerQueueUpdate();
        }

        private void OnContainerQueueUpdate()
        {
            int queueCount = DatabaseProcessor.QueueCount();

            if (queueCount.ToString() == _containerQueueCache)
                return;

            _containerQueue.Text = queueCount.ToString();
            _containerQueueCache = queueCount.ToString();

            return;
        }

        public bool OnSearchChange()
        {
            var text = _searchView.Text;

            if (_searchViewTextCache == text)
                return false;

            foreach (var criteria in _criterias)
                criteria.Value.Clear();

            ParseText(text);
            _searchViewTextCache = text;

            return true;
        }

        private void SearchClick(object sender, ButtonBase e)
        {
            Midi.Play("Click");

            if (Main.MainWindow.TableView.ItemScrollList.SelectedItem == null)
            {
                Chat.WriteLine("You must select a valid item first!");
                return;
            }

            if (Main.MainWindow.TableView.ItemScrollList.SelectedItem.ItemContainer.Root == ContainerId.Bank && !Inventory.Bank.IsOpen)
            {
                Chat.WriteLine("Item is located in your bank. Please open it first.");
                return;
            }

            Main.ItemFinder = new ItemFinder(Main.MainWindow.TableView.ItemScrollList.SelectedItem);
            Main.MainWindow.TableView.ItemScrollList.RefreshEntryColors();
        }

        private void ScanClick(object sender, ButtonBase e)
        {
            Main.ItemScanner.Scan();
            Midi.Play("Click");
        }
    }
}