using System;

namespace AssetProcessor_Editor
{
    public interface IAssetProcessor
    {
        /// <summary>
        /// The lowest level type that this processor can run on
        /// </summary>
        Type ProcessorType { get; }

        /// <summary>
        /// The display name of the processor
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A shirt description of what the processor does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// When the processor runs, this is the method that is run on that type
        /// </summary>
        void OnProcess(object obj);
    }
}
