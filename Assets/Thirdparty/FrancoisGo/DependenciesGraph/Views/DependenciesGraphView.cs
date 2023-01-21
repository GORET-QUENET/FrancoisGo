using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class DependenciesGraphView : GraphView
    {
        public float Progression { get; private set; }
        public string Description { get; private set; }
        public int FoundAssets { get; private set; }
        public int RenderAssets { get; private set; }

        private GroupBuilder groupBuilder;
        private TypeColorMap typeColorMap;
        private Vector2 canvasResolution;
        private int gridColumns;
        private GroupDirection groupsDirection;
        private List<Edge> hiddenEdges;
        private ToolbarMenu gotoElement;
        private Dictionary<string, Group> instantiatedGroups;

        private Vector2 groupsDirectionVector
            => groupsDirection == GroupDirection.Vertical ? Vector2.up : Vector2.right;

        private const int xSpacing = 450;
        private const int ySpacing = 350;
        private const int groupSpacing = 150;
        private const int groupBatchSize = 40;
        private const int edgeBatchSize = 200;
        private const int waitingMillisecondsDelay = 100;

        private Color defaultColor = new Color(0.784f, 0.784f, 0.784f);
        private Color selectionColor = new Color(0.267f, 0.753f, 1);

        #region Initialisation
        public DependenciesGraphView()
        {
            AddStyles();

            AddManipulators();
            AddGridBackground();
            AddGoTo();

            typeColorMap = AssetDatabase.LoadAssetAtPath<TypeColorMap>(GraphPath.ColorMap);
            groupBuilder = new GroupBuilder();
            hiddenEdges = new List<Edge>();
        }

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale / 10, ContentZoomer.DefaultMaxScale * 2);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        private void AddGridBackground()
        {
            var gridBackground = new GridBackground() { name = "grid-brackground" };
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
        }

        private void AddGoTo()
        {
            gotoElement = new ToolbarMenu
            {
                text = "Go To",
                tooltip = "You need to display the graph to see the options. When clicked you move to the selected group."
            };
            gotoElement.AddToClassList("goto_field");
            hierarchy.Add(gotoElement);
        }

        private void AddStyles()
        {
            StyleSheet graphViewStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphPath.GraphStyleSheet);
            StyleSheet nodeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphPath.NodeStyleSheet);

            styleSheets.Add(graphViewStyleSheet);
            styleSheets.Add(nodeStyleSheet);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);

            if (selectable is BaseNodeElement node)
            {
                var path = node.GraphNode.Path;
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                Selection.activeObject = asset;

                foreach (var element in graphElements.ToList())
                {
                    if (!(element is Group))
                    {
                        element.style.opacity = 0.1f;

                        if (element is BaseNodeElement nodeElement)
                        {
                            nodeElement.InputPort.portColor = defaultColor;
                            nodeElement.OutPutPort.portColor = defaultColor;
                        }
                    }
                }

                foreach (var hiddenEdge in hiddenEdges)
                {
                    hiddenEdge.style.display = DisplayStyle.None;
                }

                HighLightNode(node);
            }
        }

        public override void ClearSelection()
        {
            base.ClearSelection();

            foreach (var element in graphElements.ToList())
            {
                element.style.opacity = 1f;

                if(element is BaseNodeElement node)
                {
                    node.InputPort.portColor = defaultColor;
                    node.OutPutPort.portColor = defaultColor;
                }
            }

            foreach (var hiddenEdge in hiddenEdges)
            {
                hiddenEdge.style.display = DisplayStyle.None;
            }
        }

        private void HighLightNode(BaseNodeElement node)
        {
            if (node.GraphNode.IsCorrupted)
                return;

            HighLigtNodeParents(node);
            node.style.opacity = 0;
            HighLightNodeChilds(node);
        }

        private void HighLigtNodeParents(BaseNodeElement node)
        {
            if (node == null || node.style.opacity == 1)
                return;

            node.style.opacity = 1;
            node.InputPort.portColor = selectionColor;
            node.OutPutPort.portColor = selectionColor;

            if (node.GraphNode.IsCorrupted)
                return;

            foreach (var parent in node.GraphNode.Parents)
            {
                HighLigtNodeParents(parent.RenderedElement);
            }

            foreach (var edge in node.InputPort.connections)
            {
                edge.style.opacity = 1;

                if (hiddenEdges.Contains(edge))
                    edge.style.display = DisplayStyle.Flex;
            }
        }

        private void HighLightNodeChilds(BaseNodeElement node)
        {
            if (node == null || node.style.opacity == 1)
                return;

            node.style.opacity = 1;
            node.InputPort.portColor = selectionColor;
            node.OutPutPort.portColor = selectionColor;

            if (node.GraphNode.IsCorrupted)
                return;

            foreach (var child in node.GraphNode.Childs)
            {
                HighLightNodeChilds(child.RenderedElement);
            }

            foreach (var edge in node.OutPutPort.connections)
            {
                edge.style.opacity = 1;

                if (hiddenEdges.Contains(edge))
                    edge.style.display = DisplayStyle.Flex;
            }
        }
        #endregion

        #region Display
        public async Task DisplayAsync(InspectorView inspector, GraphToolbar toolbar)
        {
            GraphBuilder graphBuilder = new GraphBuilder(toolbar.Folders);
            canvasResolution = inspector.CanvasResolution;
            gridColumns = inspector.GridColumns;
            groupsDirection = inspector.GroupsDirection;

            DeleteElements(graphElements.ToList());
            FrameOrigin();
            hiddenEdges = new List<Edge>();
            instantiatedGroups = new Dictionary<string, Group>();

            List<GraphNode> graph = await graphBuilder.BuildAsync(toolbar.NameFilter, toolbar.Types, UpdateProgress);
            FoundAssets = graphBuilder.FoundAssets;
            RenderAssets = 0;

            Dictionary<string, List<GraphNode>> groups = groupBuilder.Build(graph, toolbar.Groups);

            int offset = 0;
            foreach (var group in groups)
            {
                float newProgression = Progression + (1 / (float)(groups.Count + 1)) * 50f;
                UpdateProgress(newProgression, "Drawing group : " + group.Key);
                offset = await GenerateGroupAsync(group.Key, group.Value, offset);
            }

            FillGoTo();
            await DrawGraphEdgesAsync(graph, toolbar.Groups);
        }

        private void FillGoTo()
        {
            foreach (var group in instantiatedGroups)
            {
                gotoElement.menu.AppendAction(group.Key, a => GoToGroup(group.Key));
            }
        }

        private void GoToGroup(string groupName)
        {
            ClearSelection();
            AddToSelection(instantiatedGroups[groupName]);
            FrameSelection();
        }

        public void UpdateProgress(float newValue, string description)
        {
            Progression = newValue;
            Description = description;
        }

        private async Task<int> GenerateGroupAsync(string groupName, List<GraphNode> groupGraph, int offset)
        {
            Vector2 groupOffset = groupsDirectionVector * offset;
            Group group = CreateGroup($"{groupName} ({groupGraph.Count})", groupOffset);
            AddElement(group);
            instantiatedGroups.Add(groupName, group);

            Dictionary<string, List<GraphNode>> subGroups = groupBuilder.BuildSubGroups(groupGraph);
            var graph = subGroups[GroupName.HaveDependencies.ToString()];
            var grid = subGroups[GroupName.NoDependencies.ToString()];

            Vector2 groupSize = await DrawGroupAsync(group, groupOffset, graph, grid);

            offset += (groupsDirection == GroupDirection.Vertical)
                ? (int)groupSize.y
                : (int)groupSize.x;

            return offset;
        }

        private async Task<Vector2> DrawGroupAsync(
            Group group,
            Vector2 groupOffset,
            List<GraphNode> graph,
            List<GraphNode> grid)
        {
            Vector2 graphSize = Vector2.zero;
            Vector2 gridSize = Vector2.zero;

            if (graph != null)
            {
                graphSize = await DrawNodesInGraphGroupAsync(graph, group, groupOffset);
                groupOffset.y += graphSize.y;
            }

            if (grid != null)
            {
                gridSize = await DrawNodesInGridGroupAsync(grid, group, groupOffset);
            }

            float groupSizeX = Mathf.Max(graphSize.x, gridSize.x) + groupSpacing;
            float groupSizeY = graphSize.y + gridSize.y;

            return new Vector2(groupSizeX, groupSizeY);
        }

        private Group CreateGroup(string title, Vector2 position)
        {
            Group group = new Group() { name = title, title = title };
            group.SetPosition(new Rect(position, Vector2.zero));

            return group;
        }

        private async Task<Vector2> DrawNodesInGraphGroupAsync(
            List<GraphNode> graph,
            Group group,
            Vector2 offset)
        {
            int x = 0;
            int y = 0;
            int height = 0;

            for (int i = 0; i < graph.Count; i++)
            {
                if (i != 0
                    && graph[i].Depth != graph[i - 1].Depth)
                {
                    y = 0;
                    x += xSpacing;
                }

                Vector2 position = new Vector2(x, y) + offset;
                CreateNodeInGroup(position, graph[i], group);

                y += ySpacing;

                if (y > height)
                    height = y;

                if (i % groupBatchSize == 0)
                    await Task.Run(async () => await Task.Delay(waitingMillisecondsDelay));
            }

            return new Vector2(x + xSpacing, height + groupSpacing);
        }

        private async Task<Vector2> DrawNodesInGridGroupAsync(
            List<GraphNode> graph,
            Group group,
            Vector2 offset)
        {
            int x = 0;
            int y = 0;
            int width = 0;

            for (int i = 0; i < graph.Count; i++)
            {
                if (i != 0
                    && i % gridColumns == 0)
                {
                    x = 0;
                    y += ySpacing;
                }

                Vector2 position = new Vector2(x, y) + offset;
                CreateNodeInGroup(position, graph[i], group);

                x += xSpacing;

                if (x > width)
                    width = x;

                if (i % groupBatchSize == 0)
                    await Task.Run(async () => await Task.Delay(waitingMillisecondsDelay));
            }
            return new Vector2(width, y + ySpacing + groupSpacing);
        }

        private void CreateNodeInGroup(Vector2 position, GraphNode graphNode, Group group)
        {
            graphNode.LoadAssetInformation(canvasResolution);

            var node = new BaseNodeElement();
            node.Initialize(position, graphNode.Name);
            node.Draw(graphNode, typeColorMap);

            AddElement(node);
            graphNode.RenderedElement = node;
            group.AddElement(node);

            RenderAssets++;
        }

        private async Task DrawGraphEdgesAsync(List<GraphNode> graph, Dictionary<string, bool> groupsName)
        {
            int edgeCounter = 0;
            foreach (var node in graph)
            {
                foreach (var child in node.Childs)
                {
                    if (groupsName[node.GroupName] && child.RenderedElement != null)
                    {
                        Edge edge = node.RenderedElement.OutPutPort.ConnectTo(child.RenderedElement.InputPort);
                        edge.name = $"{node.Name} to {child.Name}";

                        if (!string.Equals(node.GroupName, child.GroupName))
                        {
                            edge.style.display = DisplayStyle.None;
                            hiddenEdges.Add(edge);
                        }

                        AddElement(edge);
                        edgeCounter++;

                        if (edgeCounter % edgeBatchSize == 0)
                            await Task.Run(async () => await Task.Delay(waitingMillisecondsDelay));
                    }
                }
            }
        }
        #endregion
    }
}