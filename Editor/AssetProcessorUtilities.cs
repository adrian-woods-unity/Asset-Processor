using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public enum OperatorTypes
{
    Bool,
    Enum,
    String,
    Integer,
    Float,
    Array
}

public static class AssetProcessorUtilities
{
    public static bool IsOperatorType(this Type type, List<PropertyField> fields, int index)
    {
        var previousFieldMakesThisOperator = false;
        if (index > 0)
        {
            previousFieldMakesThisOperator =
                fields[index - 1].selectedProperty.PropertyType.IsAssignableFrom(typeof(Color));
        }
        
        return type.GetProperties().Length == 0 ||
            type.IsAssignableFrom(typeof(string)) ||
            previousFieldMakesThisOperator;
    }

    #region Property Fields
    public static void RefreshPropertyFields(this VisualElement fieldUI, PropertiesFilter filter)
    {
        fieldUI.Clear();

        var firstField = filter.propertyFields.First();
        firstField.selectedProperty.GeneratePropertyFieldsRecursive(0, filter, fieldUI);

        var operatorUI = fieldUI.parent.Q<VisualElement>("OperatorFields");
        RefreshOperatorFields(filter.propertyFields.LastOrDefault()?.selectedProperty.PropertyType, filter, operatorUI);

        var operatorValueUI = fieldUI.parent.Q<ToolbarPopupSearchField>("PropertiesOperatorValue");
        operatorValueUI.value = filter.operatorValue;
    }

    public static void GeneratePropertyFieldsRecursive(this PropertyInfo info, int index, PropertiesFilter filter, VisualElement fieldUI)
    {
        var fields = filter.propertyFields;
        var isOperatorField = info.PropertyType.IsOperatorType(fields, index);

        // set the selected property
        fields[index].selectedProperty = info;

        // generate the popup field
        // TODO: create an interface that can be implemented than handles custom filtering types (like Vector)
        var popupField = GetPropertyUI(fields, index, info, filter, fieldUI);

        // remove all of the UI children after this, they will be regenerated automatically
        while (fieldUI.childCount > index)
        {
            fieldUI.RemoveAt(index);
        }

        fieldUI.Insert(index, popupField);

        // clear all fields after this one as this is either value type and has a next field or doesn't match the currently selected next field type
        if (filter.propertyFields.Count > index + 1 && (isOperatorField || info.PropertyType != fields[index + 1].type))
        {
            // remove fields and UI that should not exist
            fields.RemoveRange(index + 1, fields.Count - (index + 1));
        }

        if (isOperatorField)
        {
            // populate the operators based on the value type and return the field as is
            var operatorUI = fieldUI.parent.Q<VisualElement>("OperatorFields");
            RefreshOperatorFields(info.PropertyType, filter, operatorUI);
        }
        else
        {
            // if the selected field type matches the next field type, then just use that field
            if (filter.propertyFields.Count > index + 1 && info.PropertyType == fields[index + 1].type)
            {
                fields[index + 1].selectedProperty.GeneratePropertyFieldsRecursive(index + 1, filter, fieldUI);
            }
            else
            {
                // otherwise add a new property field to the list and then generate UI
                filter.SetPropertyType(index + 1, fields[index].selectedProperty.PropertyType);
                fields[index + 1].selectedProperty.GeneratePropertyFieldsRecursive(index + 1, filter, fieldUI);
            }
        }
    }

    private static PopupField<MemberInfo> GetPropertyUI(IReadOnlyList<PropertyField> fields, int index, PropertyInfo info, PropertiesFilter filter, VisualElement fieldUI)
    {
        PopupField<MemberInfo> propertyField = null;
        
        var fieldType = fields[index].selectedProperty.PropertyType;
        var previousFieldType = index > 0 ? fields[index - 1].selectedProperty.PropertyType : null;

        // special handling for special classes. Eventually implement an interface that defines custom filtering 
        if (fieldType.HasCustomFilterUI())
        {
            propertyField = fieldType.GetCustomFilterUI();
        }
        else if (previousFieldType?.PreviousFilterRequiresCustomUI() == true)
        {
            // custom system or Unity types will always have the previous field be custom
            propertyField = previousFieldType.GetCustomUIForPreviousProperty(fields, index);
            fields[index].getValue = previousFieldType.GetCustomGetMethod();
        }
        else
        {
            fields[index].selectedValue = info.Name;
            propertyField = new PopupField<MemberInfo>(fields[index].propertyInfos.Cast<MemberInfo>().ToList(),
                info,
                prop => prop.Name,
                prop => prop.Name)
            {
                tooltip = $"{info.PropertyType.Name}"
            };

            // standard PropertyInfo uses simple UI and default GetGetMethod for the property
            propertyField.RegisterValueChangedCallback(field => GeneratePropertyFieldsRecursive(field.newValue as PropertyInfo, index, filter, fieldUI));
            fields[index].getValue = info.GetGetMethod();
        }

        if (propertyField != null)
        {
            propertyField.style.height = 18;
        }

        return propertyField;
    }
    #endregion

    #region Operator Fields
    private static void RefreshOperatorFields(Type type, PropertiesFilter filter, VisualElement fieldUI)
    {
        if (type != null)
        {
            filter.SetOperatorType(type);

            // make custom UI for special types
            var popup = new PopupField<string>(filter.operatorTypes, filter.selectedOperator ?? filter.operatorTypes.FirstOrDefault());
            popup.style.height = 18;
            popup.RegisterValueChangedCallback(op => SetSelectedOperator(op.newValue, filter));

            if (fieldUI.childCount > 0)
            {
                fieldUI.RemoveAt(0);
            }

            fieldUI.Insert(0, popup);
            
            // populate the possible operators with known values
            PopulateOperatorSuggestions(type, filter);
        }
    }

    private static void PopulateOperatorSuggestions(Type type, PropertiesFilter filter)
    {
        var items = filter.operatorField.menu.MenuItems();
        items.Clear();
        
        if (type.IsEnum)
        {
            items.AddRange(Enum.GetNames(type).Select(name => MenuActionFactory(name, filter)));
        }
        else if (type.IsAssignableFrom(typeof(bool)))
        {
            items.AddRange(new[] { 
                MenuActionFactory("true", filter),
                MenuActionFactory("false", filter)
            });
        }
    }

    private static DropdownMenuAction MenuActionFactory(string name, PropertiesFilter filter)
    {
        return new DropdownMenuAction(name, action => filter.operatorField.value = name,
            action => DropdownMenuAction.Status.Normal, null);
    }

    private static void SetSelectedOperator(string newOperator, PropertiesFilter filter)
    {
        filter.selectedOperator = newOperator;
    }
    #endregion
}
