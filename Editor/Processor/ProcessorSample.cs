using System;
using System.Text;
using UnityEngine;

namespace AssetProcessor_Editor
{
    public class ProcessorSample : IAssetProcessor
    {
        public Type ProcessorType => typeof(GameObject);
        public string Name => "Sample GameObject Processor";
        public string Description => "This will print out a list of all components on any GameObject result.";

        public void OnProcess(object obj)
        {
            if (obj is GameObject go)
            {
                var output = new StringBuilder($"The GameObject {go.name} contains the following components:{Environment.NewLine}");

                foreach (var component in go.GetComponents<Component>())
                {
                    output.Append($"{component.name} : {component.GetType().Name}");
                }

                Debug.Log(output);
            }
        }
    }
}
