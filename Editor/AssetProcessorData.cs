﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public enum RegionTypes
{
    AssetDatabase,
    Scene,
}

public enum AndOr
{
    And,
    Or,
}

public class AssetProcessorData : ScriptableObject
{
    public Type assetType;
    public RegionTypes regionType;
    public readonly List<PropertiesFilter> propertyFilters = new List<PropertiesFilter>();
    public readonly List<AssetProcessorResult> results = new List<AssetProcessorResult>();
}

public class AssetProcessorResult : ScriptableObject
{
    public Object gameObject;
    public string displayName;
    public List<string> values = new List<string>();
}
