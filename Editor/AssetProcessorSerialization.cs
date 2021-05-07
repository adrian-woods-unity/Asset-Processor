using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class AssetProcessorSerialization
{
    public static SerializedAssetProcessorData SerializeAssetProcessorData(this AssetProcessorData data)
    {
        var result = ScriptableObject.CreateInstance<SerializedAssetProcessorData>();

        result.assetType = data.assetType.AssemblyQualifiedName;
        result.regionType = data.regionType;

        for (var i = 0; i < data.propertyFilters.Count; i++)
        {
            var filter = data.propertyFilters[i];
            
            result.filterType.Add(filter.filterType.AssemblyQualifiedName);
            result.hasComponents.Add(filter.hasComponents);

            if (filter.selectedComponentType != null)
            {
                result.selectedComponentType.Add(filter.selectedComponentType.AssemblyQualifiedName);
            }
            
            result.operatorValue.Add(filter.operatorValue);
            result.selectedOperator.Add(filter.selectedOperator);
            result.andOrField.Add(filter.andOrField);
            
            foreach (var field in filter.propertyFields)
            {
                result.fields.Add(new SerializedPropertyField
                {
                    selectedProperty = field.selectedProperty.Name,
                    selectedValue = field.selectedValue,
                    fieldType = field.type.AssemblyQualifiedName,
                    getValue = field.getValue.Name,
                    filterIndex = i,
                });
            }
        }

        return result;
    }

    public static AssetProcessorData DeserializeAssetProcessorData(this SerializedAssetProcessorData data)
    {
        var result = ScriptableObject.CreateInstance<AssetProcessorData>();

        result.assetType = Type.GetType(data.assetType);
        result.regionType = data.regionType;

        var filterCount = data.filterType.Count;

        for (var i = 0; i < filterCount; i++)
        {
            var filter = ScriptableObject.CreateInstance<PropertiesFilter>();

            filter.filterType = Type.GetType(data.filterType[i]);
            filter.hasComponents = data.hasComponents[i];
            filter.operatorValue = data.operatorValue[i];
            filter.selectedOperator = data.selectedOperator[i];
            filter.andOrField = data.andOrField[i];

            var dataFields = data.fields.Where(field => field.filterIndex == i);
            
            if (data.andOrField.Count > 0)
            {
                filter.filterType = Type.GetType(data.filterType[i]);
                filter.hasComponents = data.hasComponents[i];
                filter.operatorValue = data.operatorValue[i];
                filter.selectedOperator = data.selectedOperator[i];

                if (data.hasComponents[i])
                {
                    filter.selectedComponentType = Type.GetType(data.selectedComponentType[i]);
                }

                var currentTypes = new List<PropertyInfo>();
                foreach (var field in dataFields)
                {
                    filter.propertyFields.Add(field.DeserializePropertyField(currentTypes));
                }
            }
            
            result.propertyFilters.Add(filter);
        }

        return result;
    }

    private static PropertyField DeserializePropertyField(this SerializedPropertyField field, List<PropertyInfo> currentTypes)
    {
        var result = ScriptableObject.CreateInstance<PropertyField>();

        result.type = Type.GetType(field.fieldType);

        if (result.type == null)
        {
            throw (new NullReferenceException("The loaded filter type was null. Please load a different filter."));
        }
        
        result.getValue = result.type?.GetMethod(field.getValue);
        result.selectedProperty = result.type?.GetProperty(field.selectedProperty);
        result.selectedValue = field.selectedValue;

        var objProperties = result.type.GetProperties().ToList().Except(currentTypes).ToList();
        
        currentTypes.Add(result.selectedProperty);
        
        result.propertyInfos.AddRange(objProperties);

        return result;
    }
}
