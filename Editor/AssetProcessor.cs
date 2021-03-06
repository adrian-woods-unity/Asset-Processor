using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AssetProcessor_Editor
{
    public class AssetProcessor : EditorWindow
    {
        private const string BasePath = "Packages/com.unity.asset-processor/Editor/UI/";

        private StyleSheet _styleSheet;

        private AssetProcessorData _assetProcessorData;
        private SerializedObject _data;

        private ListView _filtersView;
        private ListView _resultsView;
        private ListView _processorView;
        private Foldout _resultsSection;
        private Toggle _checkAll;

        private readonly List<Processor> _processors = new List<Processor>();

        [MenuItem("Tools/AssetProcessor")]
        public static void OpenAssetProcessor()
        {
            var wnd = GetWindow<AssetProcessor>();
            wnd.titleContent = new GUIContent("AssetProcessor");
        }

        private void OnEnable()
        {
            if (_assetProcessorData == null)
            {
                _assetProcessorData = CreateInstance<AssetProcessorData>();
            }

            _data = new SerializedObject(_assetProcessorData);

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BasePath}AssetProcessor.uxml");
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{BasePath}AssetProcessor.uss");

            var visualElement = visualTree.CloneTree();
            visualElement.styleSheets.Add(_styleSheet);
            rootVisualElement.Add(visualElement);

            rootVisualElement.Bind(_data);

            _filtersView = rootVisualElement.Q<ListView>("FiltersView");

            _resultsView = rootVisualElement.Q<ListView>("ResultsView");
            _resultsView.selectionType = SelectionType.Multiple;
            _resultsView.onSelectionChanged += OnResultSelected;

            _processorView = rootVisualElement.Q<ListView>("ProcessorList");

            var processButton = rootVisualElement.Q<Button>("ProcessButton");
            processButton.clickable.clicked += ProcessResults;

            _resultsSection = rootVisualElement.Q<Foldout>("ResultsFoldout");

            var regionSelector = rootVisualElement.Q<EnumField>("Region");
            regionSelector.RegisterValueChangedCallback(evt => PopulateAssetProcessorData((RegionTypes)evt.newValue));

            var filterButton = rootVisualElement.Q<Button>("FilterButton");
            filterButton.clickable.clicked += FilterAssets;

            var saveFilterButton = rootVisualElement.Q<Button>("SaveFilterButton");
            saveFilterButton.clickable.clicked += SaveFilter;
        
            var openFilterButton = rootVisualElement.Q<Button>("OpenFilterButton");
            openFilterButton.clickable.clicked += OpenFilter;

            var exportResultsButton = rootVisualElement.Q<Button>("ExportCsvButton");
            exportResultsButton.clickable.clicked += ExportResults;

            var checkAllToggle = rootVisualElement.Q<Toggle>("ToggleAll");
            checkAllToggle.RegisterValueChangedCallback(ToggleAll);

            PopulateAssetProcessorData(_assetProcessorData.regionType);
        }

        private void OpenFilter()
        {
            var path = EditorUtility.OpenFilePanel("Choose a filter to open...", "Assets", "filter.asset");
            path = GetRelativePath(path);
        
            var loadedAsset = AssetDatabase.LoadAssetAtPath<SerializedAssetProcessorData>(path);

            if (loadedAsset != null)
            {
                _assetProcessorData = loadedAsset.DeserializeAssetProcessorData();
            }

            var foreachSelector = rootVisualElement.Q<EnumField>("Region");
            foreachSelector.value = _assetProcessorData.regionType;

            PopulateAssetProcessorData(_assetProcessorData.regionType, true);
        }

        private void SaveFilter()
        {
            var path = EditorUtility.SaveFilePanel("Save filter...", "Assets", "DefaultFilter", "filter.asset");

            path = GetRelativePath(path);
        
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
                var saveObject = _assetProcessorData.SerializeAssetProcessorData();
                AssetDatabase.CreateAsset(saveObject, path);
                AssetDatabase.SaveAssets();
            }
        }

        private static string GetRelativePath(string path)
        {
            var result = string.Empty;
            if (path?.StartsWith(Application.dataPath) == true)
            {
                result = "Assets" + path.Substring(Application.dataPath.Length);
            }

            return result;
        }

        #region Populate UI

        private void PopulateAssetProcessorData(RegionTypes regionType, bool forceFilterRefresh = false)
        {
            _assetProcessorData.regionType = regionType;
            var assetTypes = new List<Type>();
            var foreachSection = rootVisualElement.Q<VisualElement>("ForEachSection");
            var assetPopup = foreachSection.Q<PopupField<Type>>("ForeachSelector");
            var currentAssetType = _assetProcessorData.assetType;

            if (assetPopup != null)
            {
                foreachSection.Remove(assetPopup);
                assetPopup.UnregisterValueChangedCallback(RefreshAssetType);
            }

            // populate the section depending on where we want to process the data
            if (regionType == RegionTypes.AssetDatabase)
            { 
                // for now we're just going to look at the Assets folder
                // TODO: make this configurable?
                var pathLookup = new Dictionary<string, Type>();
                var allPaths = AssetDatabase.GetAllAssetPaths()
                    .Where(path => path.StartsWith("Assets"))
                    .Distinct();

                foreach (var path in allPaths)
                {
                    var extension = Path.GetExtension(path);

                    if (!string.IsNullOrWhiteSpace(extension) && !pathLookup.ContainsKey(extension))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);

                        pathLookup.Add(extension, asset.GetType());
                    }
                }

                assetTypes.AddRange(pathLookup.Values);
            }
            else
            {
                assetTypes.AddRange(new[]
                {
                    typeof(GameObject)
                });
            }
        
            if (!assetTypes.Contains(_assetProcessorData.assetType))
            {
                _assetProcessorData.assetType = assetTypes.FirstOrDefault();
            }

            assetPopup = new PopupField<Type>(assetTypes, _assetProcessorData.assetType, type => type.Name, type => type.Name)
            {
                name = "ForeachSelector"

            };
            assetPopup.styleSheets.Add(_styleSheet);
            assetPopup.AddToClassList("enum_field");

            assetPopup.RegisterValueChangedCallback(RefreshAssetType);

            foreachSection.Add(assetPopup);

            // only update the filters if the asset type has changed
            if (_assetProcessorData.assetType != currentAssetType)
            {
                UpdateFilters(_assetProcessorData.assetType);
            }

            if (forceFilterRefresh)
            {
                RefreshFilters();
            }
        }

        private void RefreshAssetType(ChangeEvent<Type> assetType)
        {
            _assetProcessorData.assetType = assetType.newValue;
            UpdateFilters(assetType.newValue);   
        }
        #endregion

        #region Filters

        private void UpdateFilters(Type type)
        {
            _assetProcessorData.propertyFilters.Clear();
            AddFilter(type);
        }

        private void AddFilter(EventBase obj)
        {
            var index = GetFilterIndexFromUi(obj.originalMousePosition);
            var type = _assetProcessorData.assetType;

            AddFilter(type, index + 1);
        }

        private void AddFilter(Type type, int index = 0)
        {
            var filter = CreateInstance<PropertiesFilter>();
            filter.hasComponents = _assetProcessorData.assetType == typeof(GameObject);
        
            filter.SetPropertyType(index, type);
            filter.SetFilterType(type);

            _assetProcessorData.propertyFilters.Insert(index, filter);

            RefreshFilters();
        }

        private void RemoveFilter(EventBase obj)
        {
            // don't remove anything is there is only one filter
            if (_assetProcessorData.propertyFilters.Count > 1)
            {
                var filterIndex = GetFilterIndexFromUi(obj.originalMousePosition);
                var filter = _assetProcessorData.propertyFilters[filterIndex];
                _assetProcessorData.propertyFilters.Remove(filter);
                _filtersView.style.height = _assetProcessorData.propertyFilters.Count * _filtersView.itemHeight;
                _filtersView.Refresh();
            }
        }
        
        private void CopyFilter(EventBase obj)
        {
            var index = GetFilterIndexFromUi(obj.originalMousePosition);
            var currentFilter = _assetProcessorData.propertyFilters[index];

            var newFilter = currentFilter.CopyFilter();

            _assetProcessorData.propertyFilters.Insert(index + 1, newFilter);
            
            RefreshFilters();
        }

        private int GetFilterIndexFromUi(Vector2 mousePosition)
        {
            // get the index from the position in the list view
            var item = _filtersView.Children().FirstOrDefault(child => child.worldBound.Contains(mousePosition));
            return _filtersView.IndexOf(item);
        }

        private void RefreshFilters()
        {
            _filtersView.Clear();
            _filtersView.makeItem = MakeFilterItem;
            _filtersView.bindItem = BindFilterItem;
            _filtersView.itemsSource = _assetProcessorData.propertyFilters;
            _filtersView.selectionType = SelectionType.None;
            _filtersView.visible = true;
            _filtersView.style.height = _assetProcessorData.propertyFilters.Count * _filtersView.itemHeight;
            _filtersView.Refresh();
        }

        private VisualElement MakeFilterItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BasePath}AssetProcessorFilter.uxml");

            var item = visualTree.CloneTree();
            item.styleSheets.Add(_styleSheet);

            var addFilterButton = item.Q<Button>("AddFilterButton");
            addFilterButton.clickable.clickedWithEventInfo += AddFilter;

            var removeFilterButton = item.Q<Button>("RemoveFilterButton");
            removeFilterButton.clickable.clickedWithEventInfo += RemoveFilter;

            var copyFilterButton = item.Q<Button>("CopyFilterButton");
            copyFilterButton.clickable.clickedWithEventInfo += CopyFilter;

            return item;
        }

        private void BindFilterItem(VisualElement element, int i)
        {
            // if the element index is larger than the filter count, ignore
            if (i >= _assetProcessorData.propertyFilters.Count)
            {
                return;
            }

            var propertyFilter = _assetProcessorData.propertyFilters[i];
        
            element.Bind(new SerializedObject(propertyFilter));
        
            // visualize the component section
            var componentSelector = element.Q<ToolbarPopupSearchField>("ComponentField");
            propertyFilter.PopulateComponentSection(componentSelector, _assetProcessorData.assetType);

            // populate operator information into the filter
            propertyFilter.operatorField = element.Q<ToolbarPopupSearchField>("PropertiesOperatorValue");
            propertyFilter.operatorField.RegisterValueChangedCallback(val => propertyFilter.operatorValue = val.newValue);

            // add in the custom filter DropDowns
            var propertyFields = element.Q<VisualElement>("PropertyFields");
            propertyFields.RefreshPropertyFields(propertyFilter);
        }

        private void FilterAssets()
        {
            _assetProcessorData.results.Clear();

            // gather the assets to process
            switch (_assetProcessorData.regionType)
            {
                case RegionTypes.AssetDatabase:
                    _assetProcessorData.results.AddRange(FilterByAsset());
                    break;
                case RegionTypes.SceneOrPrefab:
                    _assetProcessorData.results.AddRange(FilterByObject());
                    break;
            }
        
            RefreshResults();
        }

        private List<AssetProcessorResult> FilterByAsset()
        {
            var result = new List<AssetProcessorResult>();

            EditorUtility.DisplayProgressBar("Parsing asset database...", string.Empty, 0);

            var paths = AssetDatabase.GetAllAssetPaths();
            var prefabs = paths.Where(path => path.EndsWith(".prefab")).ToList();
            var textures = paths.Where(path => path.EndsWith(".tga") || path.EndsWith(".tif") || path.EndsWith(".psd") || path.EndsWith(".png")).ToList();
            var materials = paths.Where(path => path.EndsWith(".mat")).ToList();

            var assetsToProcess = new List<Object>();

            if (_assetProcessorData.assetType == typeof(GameObject))
            {
                for (var i = 0; i < prefabs.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Gathering prefabs...", $"Filtering prefab {prefabs[i]} : {i} / {prefabs.Count}", i / (float)prefabs.Count);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
                    assetsToProcess.Add(prefab);
                }
            }
            else if (_assetProcessorData.assetType == typeof(Texture2D))
            {
                for (var i = 0; i < textures.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Gathering textures...", $"Filtering texture {textures[i]} : {i} / {textures.Count}", i / (float)textures.Count);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textures[i]);
                    assetsToProcess.Add(texture);
                }
            }
            else if (_assetProcessorData.assetType == typeof(Material))
            {
                for (var i = 0; i < materials.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Gathering textures...", $"Filtering texture {materials[i]} : {i} / {materials.Count}", i / (float)materials.Count);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materials[i]);
                    assetsToProcess.Add(material);
                }
            }
        
            for (var i = 0; i < assetsToProcess.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Filtering assets...", $"Filtering asset {assetsToProcess[i]} : {i} / {assetsToProcess.Count}", i / (float)assetsToProcess.Count);

                var asset = assetsToProcess[i];
                _assetProcessorData.propertyFilters.ProcessFilters(asset, result);
            }

            EditorUtility.ClearProgressBar();

            return result;
        }

        private IEnumerable<AssetProcessorResult> FilterByObject()
        {
            var result = new List<AssetProcessorResult>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var currentScene = SceneManager.GetSceneAt(i);
            
                EditorUtility.DisplayProgressBar($"Parsing scene {currentScene.name}", string.Empty, 0);

                var rootObjects = currentScene.GetRootGameObjects();

                for (var j = 0; j < rootObjects.Length; j++)
                {
                    EditorUtility.DisplayProgressBar($"Parsing scene {currentScene.name} : object {j} / " +
                                                     $"{rootObjects.Length}", string.Empty, j / (float)rootObjects.Length);

                    var obj = rootObjects[j];

                    // TODO: Implement grouping
                    _assetProcessorData.propertyFilters.ProcessFilters(obj, result);
                }

                EditorUtility.ClearProgressBar();
            }

            return result;
        }
        #endregion

        #region Results
        private void RefreshResults()
        {
            _resultsSection.value = true;
        
            _resultsView.Clear();
            _resultsView.itemsSource = _assetProcessorData.results;
            _resultsView.makeItem = MakeResultItem;
            _resultsView.bindItem = BindResultItem;
            _resultsView.visible = true;
            _resultsView.contentContainer.style.flexGrow = 1;
            _resultsView.style.height = _assetProcessorData.results.Count * _resultsView.itemHeight;
            _resultsView.Refresh();

            RefreshResultHeaders();
            RefreshProcessors();
        }

        private void RefreshResultHeaders()
        {
            var divisor = _assetProcessorData.propertyFilters.Count;
            var width = GetColumnWidth(divisor);

            var headerObject = rootVisualElement.Q<Label>("ResultsHeaderObject");
            headerObject.style.width = width;

            var headerResults = rootVisualElement.Q<VisualElement>("ResultsHeaderValues");
            headerResults.Clear();
        
            for (var i = 0; i < divisor; i++)
            {
                var label = new Label(_assetProcessorData.propertyFilters[i].propertyFields.LastOrDefault()
                    ?.selectedValue);
                label.style.width = width;
                label.styleSheets.Add(_styleSheet);
                label.AddToClassList("header");
                label.style.flexGrow = 1;

                headerResults.Add(label);
            }

            headerResults.style.flexGrow = 1;
        }

        private static Length GetColumnWidth(int divisor)
        {
            if (divisor <= 0)
            {
                divisor = 1;
            }

            // if we are at 100%, then set it to an explicit width value
            var resultPercent = (1f / divisor) * 100f;
            
            return Math.Abs(100f - resultPercent) > 0.01f
                ? new Length(resultPercent, LengthUnit.Percent)
                : 100f;
        }

        private VisualElement MakeResultItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BasePath}AssetProcessorResult.uxml");

            var item = visualTree.CloneTree();
            item.styleSheets.Add(_styleSheet);

            return item;
        }

        private void BindResultItem(VisualElement element, int i)
        {
            element.Bind(new SerializedObject(_assetProcessorData.results[i]));
        
            // generate the column results
            var values = element.Q<VisualElement>("Values");
            var objectValue = element.Q<VisualElement>("ObjectName");
            var divisor = _assetProcessorData.propertyFilters.Count;
            var width = GetColumnWidth(divisor);
        
            objectValue.style.width = width;

            for (var j = 0; j < divisor; j++)
            {
                var label = new Label(_assetProcessorData.results[i].values[j]);
                label.style.width = width;
                label.styleSheets.Add(_styleSheet);
                label.AddToClassList("result");
                label.style.flexGrow = 1;
            
                values.Add(label);
            }
        }
        
        private void ToggleAll(ChangeEvent<bool> evt)
        {
            foreach (var t in _assetProcessorData.results)
            {
                t.isChecked = evt.newValue;
            }
        }

        private void OnResultSelected(List<object> objects)
        {
            var unityObjects = objects.Cast<AssetProcessorResult>()
                .Select(obj => obj.gameObject)
                .ToArray();
            Selection.objects = unityObjects;
        }
    
        private void ExportResults()
        {
            var path = EditorUtility.SaveFilePanel("Export results to csv file",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                $"AssetProcessorResults_{DateTime.UtcNow.ToFileTime()}",
                "csv");

            if (!string.IsNullOrWhiteSpace(path))
            {
                var writer = File.CreateText(path);

                var headerBuilder = new StringBuilder();
                headerBuilder.Append("Object Name,");
                for (var i = 0; i < _assetProcessorData.propertyFilters.Count(); i++)
                {
                    var value = _assetProcessorData.propertyFilters[i].propertyFields.LastOrDefault()
                        ?.selectedValue;
                    headerBuilder.Append($"{value},");
                }

                headerBuilder.Remove(headerBuilder.Length - 1, 1);
                writer.WriteLine(headerBuilder.ToString());

                foreach (var result in _assetProcessorData.results)
                {
                    var valuesBuilder = new StringBuilder();

                    foreach (var value in result.values)
                    {
                        valuesBuilder.Append($",{value}");
                    }
                    
                    writer.WriteLine($"{result.displayName}{valuesBuilder}");
                }
            
                writer.Close();
            }
        }
        #endregion
        
        #region Processors

        private void RefreshProcessors()
        {
            _processors.Clear();
            var processors = TypeCache.GetTypesDerivedFrom<Processor>();

            foreach (var processor in processors)
            {
                if (CreateInstance(processor) is Processor instantiated)
                {
                    if (instantiated.processorType == _assetProcessorData.assetType)
                    {
                        _processors.Add(instantiated);
                    }   
                }
            }
            
            // update the UI
            UpdateProcessorUI();
        }

        private void UpdateProcessorUI()
        {
            _processorView.Clear();
            _processorView.itemsSource = _processors;
            _processorView.makeItem = MakeProcessorItem;
            _processorView.bindItem = BindProcessorItem;
            _processorView.visible = true;
            _processorView.contentContainer.style.flexGrow = 1;
            _processorView.style.height = _processors.Count * _processorView.itemHeight;
            _processorView.Refresh();
        }

        private VisualElement MakeProcessorItem()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{BasePath}AssetProcessorProcessor.uxml");

            var item = visualTree.CloneTree();
            item.styleSheets.Add(_styleSheet);

            return item;
        }
        
        private void BindProcessorItem(VisualElement element, int i)
        {
            element.Bind(new SerializedObject(_processors[i]));
        
            // generate the column results
            var processorName = element.Q<Label>("ProcessorName");
            var description = element.Q<Label>("ProcessorDescription");
            
            processorName.style.width = new Length(49f, LengthUnit.Percent);
            description.style.width = new Length(49f, LengthUnit.Percent);
        }

        private void ProcessResults()
        {
            var progressTotal = _processors.Count * Selection.objects.Length;
            var currentProgress = 0f;
            
            foreach (var processor in _processors.Where(proc => proc.isEnabled))
            {
                foreach (var obj in _assetProcessorData.results)
                {
                    EditorUtility.DisplayProgressBar($"Running processor {processor.processorName}...", 
                        $"Running on object {obj.name}", 
                        currentProgress / progressTotal);
                    
                    processor.OnProcess(obj);
                    currentProgress++;
                }   
            }
            
            EditorUtility.ClearProgressBar();
        }
        #endregion
    }
}