using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class DependenciesEditorWindow : EditorWindow
    {
        private PaneElement topLeftPane;
        private PaneElement bottomLeftPane;
        private PaneElement rightPane;
        private DependenciesGraphView graphView;
        private GraphToolbar graphToolbar;
        private InspectorView inspector;
        private LoadingBarElement loadingBar;
        private bool isRunning;

        [MenuItem("Window/Dependencies Graph")]
        public static void ShowMyEditor()
        {
            GetWindow<DependenciesEditorWindow>("Dependencies Graph");
        }

        private void OnEnable()
        {
            AddPanes();

            AddInspector();
            AddCaption();

            AddGraphView();
            AddGraphTollbar();

            AddLoadingBar();

            AddStyles();
        }

        private void AddPanes()
        {
            var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            var leftPane = new PaneElement();
            splitView.Add(leftPane);
            var leftSplitView = new TwoPaneSplitView(0, 150, TwoPaneSplitViewOrientation.Vertical);
            leftPane.AddToContent(leftSplitView);

            topLeftPane = new PaneElement("Inspector");
            leftSplitView.Add(topLeftPane);
            bottomLeftPane = new PaneElement("Caption");
            leftSplitView.Add(bottomLeftPane);

            rightPane = new PaneElement("Graph");
            splitView.Add(rightPane);
        }

        private void AddInspector()
        {
            inspector = new InspectorView();
            topLeftPane.AddToContent(inspector);
        }

        private void AddCaption()
        {
            var legend = new CaptionView();
            bottomLeftPane.AddToContent(legend);
        }

        private void AddGraphTollbar()
        {
            graphToolbar = new GraphToolbar(ShowGraphAsync);
            rightPane.AddToContent(graphToolbar);
        }

        private void AddLoadingBar()
        {
            loadingBar = new LoadingBarElement();
            loadingBar.style.display = DisplayStyle.None;
            rootVisualElement.Add(loadingBar);
        }

        private void AddGraphView()
        {
            graphView = new DependenciesGraphView();
            graphView.StretchToParentSize();
            rightPane.AddToContent(graphView);
        }

        private async void ShowGraphAsync()
        {
            if (inspector.CanvasResolution == Vector2.zero)
            {
                Debug.LogError("Please fill the Canvas Resolution field before displaying the graph");
                return;
            }

            loadingBar.style.display = DisplayStyle.Flex;
            graphView.UpdateProgress(0, "Loading");
            isRunning = true;

            graphToolbar.SetCounter("Visible Assets : 0");
            await graphView.DisplayAsync(inspector, graphToolbar);

            string value = $"Visible Assets : {graphView.RenderAssets} / {graphView.FoundAssets}";
            graphToolbar.SetCounter(value);

            loadingBar.style.display = DisplayStyle.None;
            isRunning = false;
        }

        private void AddStyles()
        {
            StyleSheet variablesStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphPath.VariablesStyleSheet);
            rootVisualElement.styleSheets.Add(variablesStyleSheet);

            StyleSheet paneStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphPath.PaneStyleSheet);
            rootVisualElement.styleSheets.Add(paneStyleSheet);
        }

        private void Update()
        {
            if (isRunning)
            {
                loadingBar.Fill.style.width = new Length(graphView.Progression, LengthUnit.Percent);
                loadingBar.Description.text = graphView.Description;
            }
        }
    }
}