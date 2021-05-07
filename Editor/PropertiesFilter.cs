using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetProcessor_Editor
{
    public class PropertyField : ScriptableObject
    {
        public readonly List<PropertyInfo> propertyInfos = new List<PropertyInfo>();
        public PropertyInfo selectedProperty;
        public string selectedValue;
        public Type type;
        public MethodInfo getValue;
    }

    public class PropertiesFilter : ScriptableObject
    {
        public bool hasComponents;
        private readonly List<Type> _components = new List<Type>();
        public Type selectedComponentType;
        public readonly List<PropertyField> propertyFields = new List<PropertyField>();
        public readonly List<string> operatorTypes = new List<string>();
        public Type filterType;
        public string selectedOperator;
        public string operatorValue = string.Empty;
        public AndOr andOrField;

        [NonSerialized]
        public ToolbarPopupSearchField operatorField;

        private void OnEnable()
        {
            _components.Clear();
            _components.AddRange(TypeCache.GetTypesDerivedFrom<Component>()
                .OrderBy(type => type.Name)
                .ToList());
        }
        

        public void PopulateComponentSection(ToolbarPopupSearchField componentSelector, Type assetType)
        {
            var typeChanged = false;
            
            if (hasComponents)
            {
                componentSelector.style.width = new StyleLength(StyleKeyword.Auto);
                componentSelector.style.visibility = Visibility.Visible;

                var selectedType = GetTypeFromFullName(selectedComponentType != null
                    ? selectedComponentType.FullName
                    : _components.First().FullName);
                
                componentSelector.value = selectedType.FullName;
                
                PopulateComponentSuggestions(componentSelector);
                
                componentSelector.RegisterValueChangedCallback(evt =>
                    RefreshPropertyFields(evt.newValue, componentSelector));

                if (filterType != selectedType)
                {
                    filterType = selectedType;
                    typeChanged = true;
                }
            }
            else
            {
                componentSelector.style.width = 0;
                componentSelector.style.visibility = Visibility.Hidden;

                if (filterType != assetType)
                {
                    filterType = assetType;
                    typeChanged = true;
                }
            }

            // only set the type if the current type has changed
            if (typeChanged)
            {
                SetPropertyType(0, filterType);
            }
        }
        
        private void PopulateComponentSuggestions(ToolbarPopupSearchField componentField)
        {
            var items = componentField.menu.MenuItems();
            items.Clear();

            var suggestions = GetTypesFromFullName(componentField.value);

            items.AddRange(suggestions.Select(t => MenuActionFactory(t.FullName, componentField)));
        }

        private DropdownMenuAction MenuActionFactory(string typeName, ToolbarPopupSearchField componentField)
        {
            return new DropdownMenuAction(typeName, action =>
                {
                    var type = GetTypeFromFullName(typeName);

                    if (type != null)
                    {
                        componentField.SetValueWithoutNotify(typeName);
                        RefreshPropertyFields(typeName, componentField);
                    }
                },
                action => DropdownMenuAction.Status.Normal, null);
        }

        private void RefreshPropertyFields(string typeName, ToolbarPopupSearchField componentSelector)
        {
            PopulateComponentSuggestions(componentSelector);
            
            var type = GetTypeFromFullName(typeName);
            if (type == null) return;
            
            SetPropertyType(0, type);
            selectedComponentType = type;
            filterType = type;

            var parent = componentSelector.parent;
            var fieldUI = parent.Q<VisualElement>("PropertyFields");
            propertyFields.FirstOrDefault()?.selectedProperty.GeneratePropertyFieldsRecursive(0, this, fieldUI);
        }

        private Type GetTypeFromFullName(string fullName)
        {
            return _components.FirstOrDefault(comp =>
                comp.AssemblyQualifiedName != null && comp.AssemblyQualifiedName.IndexOf(fullName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private IEnumerable<Type> GetTypesFromFullName(string fullName)
        {
            return _components.Where(comp =>
                comp.AssemblyQualifiedName != null && comp.AssemblyQualifiedName.IndexOf(fullName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public void SetPropertyType(int index, Type type)
        {
            // clear out the propertyList at and after the currently selected object
            while (index < propertyFields.Count)
            {
                propertyFields.RemoveAt(index);
            }

            // exclude currently selected types from the list of possible additional types
            var currentTypes = propertyFields.Select(list => list.selectedProperty).ToList();

            var objProperties = type.GetProperties().ToList().Except(currentTypes).ToList();
            var newPropertyField = CreateInstance<PropertyField>();

            newPropertyField.type = type;
            newPropertyField.propertyInfos.AddRange(objProperties);
            
            newPropertyField.selectedProperty = newPropertyField.propertyInfos.FirstOrDefault();
            newPropertyField.getValue = newPropertyField.selectedProperty?.GetGetMethod();

            propertyFields.Add(newPropertyField);
        }

        public void SetOperatorType(Type type)
        {
            operatorTypes.Clear();
            operatorTypes.AddRange(new[] { "==", "!=" });

            if (type.IsEnum || type.IsAssignableFrom(typeof(string)))
            {
                operatorTypes.AddRange(new[] { "contains", "not contains" });
            }
            else if (type.IsNumeric() || type.IsCustomArrayType())
            {
                operatorTypes.AddRange(new[] { ">", ">=", "<", "<=" });
            }

            // only change the operator if it is not in the newly generated list of operators
            if (!operatorTypes.Contains(selectedOperator))
            {
                selectedOperator = operatorTypes.FirstOrDefault();
            }
        }

        public void SetFilterType(Type type)
        {
            filterType = type;
        }
    }   
}
