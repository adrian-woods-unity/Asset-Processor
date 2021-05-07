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
        

        public void PopulateComponentSection(VisualElement componentSection, Type assetType)
        {
            var typeChanged = false;
            
            if (hasComponents)
            {
                componentSection.style.width = new StyleLength(StyleKeyword.Auto);
                componentSection.style.visibility = Visibility.Visible;

                var componentFieldPopup = componentSection.Q<PopupField<Type>>("PropertiesField");

                if (componentFieldPopup == null)
                {
                    var selectedIndex = _components.IndexOf(selectedComponentType);
                    selectedIndex = selectedIndex < 0 ? 0 : selectedIndex;
                    
                    componentFieldPopup = new PopupField<Type>(_components, selectedIndex, type => type.Name, type => type.Name)
                        { name = "PropertiesField" };

                    componentFieldPopup.RegisterValueChangedCallback(evt => RefreshPropertyFields(evt.newValue, componentSection));
                    componentFieldPopup.style.height = 18;

                    componentSection.Insert(0, componentFieldPopup);
                }

                if (filterType != componentFieldPopup.value)
                {
                    filterType = componentFieldPopup.value;
                    typeChanged = true;
                }
            }
            else
            {
                componentSection.style.width = 0;
                componentSection.style.visibility = Visibility.Hidden;

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

        private void RefreshPropertyFields(Type type, VisualElement componentSection)
        {
            SetPropertyType(0, type);
            selectedComponentType = type;
            filterType = type;

            var parent = componentSection.parent;
            var fieldUI = parent.Q<VisualElement>("PropertyFields");
            propertyFields.FirstOrDefault()?.selectedProperty.GeneratePropertyFieldsRecursive(0, this, fieldUI);
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
