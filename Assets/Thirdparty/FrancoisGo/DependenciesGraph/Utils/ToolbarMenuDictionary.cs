using System;
using System.Collections.Generic;

namespace DependenciesGraph
{
    [Serializable]
    public class ToolbarMenuDictionary
    {
        public Dictionary<string, bool> Dictionary;

        public ToolbarMenuDictionary(Dictionary<string, bool> dictionary)
        {
            Dictionary = dictionary;
        }
    }
}
