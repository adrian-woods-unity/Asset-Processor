using System;
using UnityEngine;

namespace AssetProcessor_Editor
{
    public class Processor : ScriptableObject
    {
        public Type processorType;

        public string processorName;

        public string processorDescription;

        public bool isEnabled;
        public virtual void OnProcess(object obj) { }
    }
}
