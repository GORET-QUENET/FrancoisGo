using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class PaneElement : VisualElement
    {
        private VisualElement content;

        public PaneElement()
        {
            CreateContent();
        }

        public PaneElement(string title = null)
        {
            if (title != null)
            {
                name = title;
                var leftHeader = new PaneHeader(title);
                hierarchy.Add(leftHeader);
            }

            CreateContent();
        }

        private void CreateContent()
        {
            content = new VisualElement();
            content.AddToClassList("pane__content");
            hierarchy.Add(content);
        }

        public void AddToContent(VisualElement element)
        {
            content.Add(element);
        }
    }
}
