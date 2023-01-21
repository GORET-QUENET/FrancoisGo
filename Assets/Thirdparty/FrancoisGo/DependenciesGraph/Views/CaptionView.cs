using UnityEditor;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class CaptionView : VisualElement
    {
        public CaptionView()
        {
            TypeColorMap typeColorMap = AssetDatabase.LoadAssetAtPath<TypeColorMap>(GraphPath.ColorMap);
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphPath.CaptionStyleSheet);
            styleSheets.Add(styleSheet);

            AddToClassList("caption__container");

            foreach (var typeColor in typeColorMap.Types)
            {
                Label label = new Label(typeColor.Type.ToString())
                {
                    name = typeColor.Type.ToString()
                };
                label.style.backgroundColor = new StyleColor(typeColor.Color);
                label.AddToClassList("caption__label");
                hierarchy.Add(label);
            }
        }
    }
}
