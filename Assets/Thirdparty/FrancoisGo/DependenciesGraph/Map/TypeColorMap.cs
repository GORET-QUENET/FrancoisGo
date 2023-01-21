using System;
using System.Collections.Generic;
using UnityEngine;

namespace DependenciesGraph
{
    [Serializable]
    public class TypeColor
    {
        public AssetType Type;
        public Color32 Color;
    }

    [CreateAssetMenu(fileName = "TypeColorMap", menuName = "DependenciesGraph/TypeColorMap")]
    public class TypeColorMap : ScriptableObject
    {
        public List<TypeColor> Types;
    }
}
