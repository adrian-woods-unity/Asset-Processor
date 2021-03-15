using System;

namespace Editor.AssetProcessor.SerializedData
{
    [Serializable]
    public struct SerializedPropertyField
    {
        public string selectedProperty;
        public string selectedValue;
        public string fieldType;
        public string getValue;
        public int filterIndex;

        public SerializedPropertyField(string value, string type, string property, string get, int index)
        {
            selectedValue = value;
            fieldType = type;
            selectedProperty = property;
            getValue = get;
            filterIndex = index;
        }
    }
}
