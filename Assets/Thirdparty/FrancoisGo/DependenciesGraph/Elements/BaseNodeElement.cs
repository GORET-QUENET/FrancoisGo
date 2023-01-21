using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class BaseNodeElement : Node
    {
        public string Title { get; set; }

        public Port InputPort { get; set; }
        public Port OutPutPort { get; set; }
        public GraphNode GraphNode { get; set; }

        public virtual void Initialize(Vector2 position, string title)
        {
            Title = title;
            name = title;
            expanded = false;

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("node__main-container");
            extensionContainer.AddToClassList("node__extension-container");
        }

        public virtual void Draw(GraphNode graphNode, TypeColorMap typeColorMap)
        {
            GraphNode = graphNode;

            Label titlelabel = titleContainer.Query<Label>("title-label");
            titlelabel.text = Title;

            titleContainer.style.backgroundColor = new StyleColor(typeColorMap.Types.Find(t => t.Type == graphNode.Type).Color);

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Used by";
            inputContainer.Add(InputPort);

            OutPutPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutPutPort.portName = "Need";
            outputContainer.Add(OutPutPort);

            var customDataContainer = new VisualElement();
            customDataContainer.AddToClassList("node__custom-data-container");

            var size = new Label
            {
                name = "node-size",
                text = graphNode.Size
            };
            customDataContainer.Add(size);

            if (graphNode.Image != null)
            {
                var image = new VisualElement();
                image.style.backgroundImage = graphNode.Image;
                image.AddToClassList("node__image");
                customDataContainer.Add(image);
            }

            extensionContainer.Add(customDataContainer);

            expanded = true;
        }
    }
}
