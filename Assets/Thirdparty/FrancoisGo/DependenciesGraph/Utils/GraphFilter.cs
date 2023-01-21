using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DependenciesGraph
{
    public static class GraphFilter
    {
        public static ConcurrentBag<GraphNode> FilterGraph(
            ConcurrentBag<GraphNode> graph,
            string nameFilter,
            Dictionary<string, bool> typeFilter)
        {
            List<string> typeFilterList = typeFilter.ToList()
                                                    .Where(t => t.Value)
                                                    .Select(t => t.Key)
                                                    .ToList();

            bool haveNameFilter = !string.IsNullOrEmpty(nameFilter);
            bool haveTypeFilter = typeFilterList.Count != typeFilter.Count;

            if (haveNameFilter)
            {
                ApplyNameFilter(graph, nameFilter);
                CleanFilteredGraph(ref graph);
            }

            if (haveNameFilter && haveTypeFilter)
            {
                foreach (var node in graph)
                {
                    node.IsVisible = false;
                }
            }

            if (haveTypeFilter)
            {
                ApplyTypeFilter(graph, typeFilterList);
                CleanFilteredGraph(ref graph);
            }

            if (!haveNameFilter && !haveTypeFilter)
            {
                foreach (var node in graph)
                {
                    node.IsVisible = true;
                }
            }

            return graph;
        }

        private static void ApplyNameFilter(ConcurrentBag<GraphNode> graph, string nameFiler)
        {
            foreach (var node in graph)
            {
                if (node.Name.ToLower().Contains(nameFiler.ToLower()))
                {
                    node.IsVisible = true;
                    SetParentsVisible(node);
                    SetChildsVisible(node);
                }
            }
        }

        private static void SetParentsVisible(GraphNode node)
        {
            if (node.Parents.Count == 0)
                return;

            foreach (var parent in node.Parents)
            {
                if (parent.IsVisible) // Prevent cyclic issue
                    continue;

                parent.IsVisible = true;
                SetParentsVisible(parent);
            }
        }

        private static void SetChildsVisible(GraphNode node)
        {
            if (node.Childs.Count == 0)
                return;

            foreach (var child in node.Childs)
            {
                if (child.IsVisible) // Prevent cyclic issue
                    continue;

                child.IsVisible = true;
                SetChildsVisible(child);
            }
        }

        private static void CleanFilteredGraph(ref ConcurrentBag<GraphNode> graph)
        {
            graph = new ConcurrentBag<GraphNode>(graph.Where(x => x.IsVisible));
        }

        private static void ApplyTypeFilter(ConcurrentBag<GraphNode> graph, List<string> typeFilterList)
        {
            foreach (var node in graph)
            {
                if (typeFilterList.Contains(node.Type.ToString()))
                {
                    node.IsVisible = true;
                }
            }
        }
    }
}
