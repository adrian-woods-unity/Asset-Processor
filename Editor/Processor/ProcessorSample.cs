using System;
using System.Text;
using UnityEngine;

namespace AssetProcessor_Editor
{
    public class ProcessorSample : Processor
    {
        public ProcessorSample()
        {
            processorType = typeof(GameObject);
            processorName = "Sample GameObject Processor";
            processorDescription = "This will print out a list of all components on any GameObject result.";
        }

        public override void OnProcess(object obj)
        {
            if (obj is AssetProcessorResult result)
            {
                var go = result.gameObject as GameObject;

                if (go != null && result.isChecked)
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
}
