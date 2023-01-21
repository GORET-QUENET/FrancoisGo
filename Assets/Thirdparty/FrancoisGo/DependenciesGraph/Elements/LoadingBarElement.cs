using UnityEditor;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class LoadingBarElement : VisualElement
    {
        public VisualElement Fill;
        public Label Description;

        public LoadingBarElement()
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphPath.LoadingBarStyleSheet);
            styleSheets.Add(styleSheet);

            AddToClassList("loading-bar__overlay");

            var loadingBarWindow = new VisualElement();
            loadingBarWindow.AddToClassList("loading-bar__window");
            hierarchy.Add(loadingBarWindow);

            var title = new Label("Creation of the graph running");
            loadingBarWindow.Add(title);

            var loadingBarContainer = new VisualElement();
            loadingBarContainer.AddToClassList("loading-bar__container");
            loadingBarWindow.Add(loadingBarContainer);

            Fill = new VisualElement();
            Fill.AddToClassList("loading-bar__fill");
            loadingBarContainer.Add(Fill);

            Description = new Label();
            Description.AddToClassList("loading-bar__description");
            loadingBarWindow.Add(Description);
        }
    }
}
