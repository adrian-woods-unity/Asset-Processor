using System.Collections.Generic;
using UnityEngine;

namespace AssetProcessor_Editor
{
    public class SerializedAssetProcessorData : ScriptableObject
    {
        // single properties
        public string assetType;
        public RegionTypes regionType;
    
        // filter properties
        public List<bool> hasComponents = new List<bool>();
        public List<string> selectedComponentType = new List<string>();
        public List<string> filterType = new List<string>();
        public List<string> selectedOperator = new List<string>();
        public List<string> operatorValue = new List<string>();
        public List<AndOr> andOrField = new List<AndOr>();
    
        // figure out property fields
        public List<SerializedPropertyField> fields = new List<SerializedPropertyField>();
    }
}