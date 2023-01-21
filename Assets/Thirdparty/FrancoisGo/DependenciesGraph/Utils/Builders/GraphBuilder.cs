using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DependenciesGraph
{
    public class GraphBuilder
    {
        public int FoundAssets { get; private set; }

        private readonly string[] folders;
        private readonly List<string> scenesGuids;

        public GraphBuilder(string[] folders)
        {
            this.folders = folders;
            AssetDatabaseAsync.MainThread = TaskScheduler.FromCurrentSynchronizationContext();
            scenesGuids = AssetDatabase.FindAssets("t:Scene", folders).ToList();
        }

        public Task<List<GraphNode>> BuildAsync(
            string nameFilter,
            Dictionary<string, bool> typeFilter,
            Action<float, string> progressCallback)
        {
            Task<List<GraphNode>> task = Task.Run(async () =>
            {
                ConcurrentBag<GraphNode> graph = await InitGraphAsync(progressCallback);
                FoundAssets = graph.Count;

                progressCallback(46f, "Create assets hierarchy");
                await CreateHierarchyAsync(graph);

                progressCallback(47f, "Create assets depth");
                PopulateWithDepth(graph);

                progressCallback(48f, "Link assets to scenes");
                await PopulateWithScenesAsync(graph);

                progressCallback(49f, "Apply toolbar filters");
                graph = GraphFilter.FilterGraph(graph, nameFilter, typeFilter);

                progressCallback(50f, "Assets graph ready");
                return graph.ToList();
            });

            return task;
        }

        private async Task<ConcurrentBag<GraphNode>> InitGraphAsync(Action<float, string> progressCallback)
        {
            List<string> assetsGuids = (await AssetDatabaseAsync.FindAssets("", folders)).ToList();
            ConcurrentBag<GraphNode> graph = new ConcurrentBag<GraphNode>();
            List<Task> tasks = new List<Task>();

            foreach (var guid in assetsGuids)
            {
                Task task = Task.Run(async () =>
                {
                    string path = await AssetDatabaseAsync.GUIDToAssetPath(guid);

                    if (Directory.Exists(path)) // Ignore folders
                        return;

                    var node = new GraphNode(guid, path);
                    if (node.Type == AssetType.ScriptableObject)
                    {
                        node.Dependencies = await GetScriptableDependenciesAsync(node);
                    }
                    else
                    {
                        node.Dependencies = (await AssetDatabaseAsync.GetDependencies(node.Path, false)).ToList();
                    }

                    graph.Add(node);
                    progressCallback((graph.Count / (float)assetsGuids.Count) * 50f, "Preparing assets");
                });
                tasks.Add(task);

                if (tasks.Count % (assetsGuids.Count / 10) == 0)
                    await Task.WhenAll(tasks);
            }

            await Task.WhenAll(tasks);

            return graph;
        }

        private async Task<List<string>> GetScriptableDependenciesAsync(GraphNode node)
        {
            string[] scriptableContent = File.ReadAllLines(node.Path);
            string separator = "guid:";
            IEnumerable<string> guidLines = scriptableContent
                                                .Where(l => l.ToLower().Contains(separator))
                                                .Select(l =>
                                                {
                                                    string line = l.ToLower();
                                                    int id = line.IndexOf(separator) + separator.Length;
                                                    return line.Substring(id);
                                                });

            List<string> dependencies = new List<string>();
            foreach (var guidLine in guidLines)
            {
                string guid = guidLine;
                if (guidLine.Contains(','))
                {
                    guid = guidLine.Split(',')[0];
                }

                guid = guid.Trim();

                if (string.IsNullOrEmpty(guid))
                    continue;

                string path = await AssetDatabaseAsync.GUIDToAssetPath(guid);
                dependencies.Add(path);
            }

            return dependencies;
        }

        private async Task CreateHierarchyAsync(ConcurrentBag<GraphNode> graph)
        {
            IEnumerable<string> graphPaths = graph.Select(t => t.Path);
            List<Task> tasks = new List<Task>();

            foreach (var node in graph)
            {
                Task task = Task.Run(() =>
                {
                    List<string> childsPath = node.Dependencies.Intersect(graphPaths).ToList();

                    foreach (var childPath in childsPath)
                    {
                        GraphNode childNode = graph.First(n => string.Equals(n.Path, childPath));

                        if (childNode == node)
                        {
                            node.IsCorrupted = true;
                            continue;
                        }

                        node.Childs.Add(childNode);
                        childNode.Parents.Add(node);
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task PopulateWithScenesAsync(ConcurrentBag<GraphNode> graph)
        {
            IEnumerable<string> graphPaths = graph.Select(n => n.Path);
            List<Task> tasks = new List<Task>();

            foreach (var sceneGuid in scenesGuids)
            {
                Task task = Task.Run(async () =>
                {
                    string scenePath = await AssetDatabaseAsync.GUIDToAssetPath(sceneGuid);
                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    var childsPath = (await AssetDatabaseAsync.GetDependencies(scenePath, true)).Intersect(graphPaths);

                    foreach (var childPath in childsPath)
                    {
                        graph.First(n => string.Equals(n.Path, childPath)).ParentsScenes.Add(sceneName);
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private void PopulateWithDepth(ConcurrentBag<GraphNode> graph)
        {
            IEnumerable<GraphNode> graphSource = graph.Where(n => n.Parents.Count == 0);

            foreach (var node in graphSource)
            {
                if (node.IsCorrupted)
                {
                    continue;
                }

                PopulateChildsDepth(node);
            }
        }

        private void PopulateChildsDepth(GraphNode node)
        {
            if (node.IsCorrupted)
            {
                return;
            }

            foreach (var child in node.Childs)
            {
                if (child.Depth != 0 && child.Childs.Count != 0) // To prevent cyclic issue
                    continue;

                child.Depth = Mathf.Max(child.Depth, node.Depth + 1);
                PopulateChildsDepth(child);
            }
        }
    }
}