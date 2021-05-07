using System;
using System.Collections.Generic;

[Serializable]
public struct SerializedPropertiesFilter
{
    public bool hasComponents;
    public string selectedComponentType;
    public List<SerializedPropertyField> propertyFields;
    public string filterType;
    public string selectedOperator;
    public string operatorValue;
    public AndOr andOrField;
}
