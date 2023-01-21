using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class PaneHeader : VisualElement
    {
        public PaneHeader(string title)
        {
            AddToClassList("pane__header");
            var headerLabel = new Label(title);
            hierarchy.Add(headerLabel);
        }
    }
}
