namespace DependenciesGraph
{
    public static class GraphPath
    {
        private static string root => "Assets/Thirdparty/FrancoisGo/DependenciesGraph/";

        public static string ColorMap => root + "Map/TypeColorMap.asset";
        public static string VariablesStyleSheet => root + "Styles/Variables.uss";
        public static string PaneStyleSheet => root + "Styles/PaneStyles.uss";
        public static string GraphStyleSheet => root + "Styles/MyGraphViewStyles.uss";
        public static string NodeStyleSheet => root + "Styles/NodeStyles.uss";
        public static string CaptionStyleSheet => root + "Styles/CaptionStyles.uss";
        public static string LoadingBarStyleSheet => root + "Styles/LoadingBarStyles.uss";
        public static string CanvasPrefab => root + "Prefabs/PreviewCanvas.prefab";
    }
}
