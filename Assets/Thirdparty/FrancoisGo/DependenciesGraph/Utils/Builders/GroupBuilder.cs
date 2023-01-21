using System.Collections.Generic;
using System.Linq;

namespace DependenciesGraph
{
    public class GroupBuilder
    {
        public Dictionary<string, List<GraphNode>> Build(List<GraphNode> graph,
            Dictionary<string, bool> groupsName)
        {
            var groups = new Dictionary<string, List<GraphNode>>();

            foreach (var groupName in groupsName)
            {
                if (groupName.Value)
                    groups.Add(groupName.Key, new List<GraphNode>());
            }

            foreach (var node in graph)
            {
                if (node.IsCorrupted)
                {
                    node.GroupName = GroupName.Corrupted.ToString();
                }
                else if (node.ParentsScenes.Count == 0)
                {
                    if (node.HaveNoDependencies)
                    {
                        if (node.Type != AssetType.Script
                            && !node.Name.Contains(".asmdef")) // Don't show unused Scripts and Assemblies
                        {
                            node.GroupName = GroupName.Unused.ToString();
                        }
                    }
                    else
                    {
                        node.GroupName = GroupName.Common.ToString();
                    }
                }
                else if (node.ParentsScenes.Count == 1)
                {
                    node.GroupName = node.ParentsScenes.First();
                }
                else
                {
                    node.GroupName = GroupName.Common.ToString();
                }

                if (groups.Keys.Contains(node.GroupName))
                    groups[node.GroupName].Add(node);
            }

            RemoveEmptyGroups(groups);

            return groups;
        }

        public Dictionary<string, List<GraphNode>> BuildSubGroups(List<GraphNode> graph)
        {
            var subGroups = new Dictionary<string, List<GraphNode>>()
            {
                { GroupName.HaveDependencies.ToString(), null },
                { GroupName.NoDependencies.ToString(), null }
            };

            foreach (var node in graph)
            {
                string subGroupName = node.HaveNoDependenciesInSameGroup
                    ? GroupName.NoDependencies.ToString()
                    : GroupName.HaveDependencies.ToString();

                if (subGroups[subGroupName] == null)
                    subGroups[subGroupName] = new List<GraphNode>();

                subGroups[subGroupName].Add(node);
            }

            return subGroups;
        }

        private void RemoveEmptyGroups(Dictionary<string, List<GraphNode>> groups)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                KeyValuePair<string, List<GraphNode>> group = groups.ElementAt(i);

                if (group.Value.Count == 0)
                {
                    groups.Remove(group.Key);
                    i--;
                }
                else
                {
                    group.Value.Sort();
                }
            }
        }
    }
}
