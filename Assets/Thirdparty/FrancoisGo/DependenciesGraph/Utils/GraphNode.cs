using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace DependenciesGraph
{
    public class GraphNode : IComparable
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public string Path { get; set; }
        public ConcurrentBag<GraphNode> Childs { set; get; }
        public ConcurrentBag<GraphNode> Parents { get; set; }
        public BaseNodeElement RenderedElement { get; set; }
        public ConcurrentBag<string> ParentsScenes { set; get; }
        public int Depth { get; set; }
        public AssetType Type { get; set; }
        public string GroupName { get; set; }
        public string Size { get; set; }
        public bool IsCorrupted { get; set; }
        public Texture2D Image { get; set; }
        public bool IsVisible { get; set; }
        public List<string> Dependencies { get; set; }

        public List<GraphNode> ParentsInSameGroup
            => Parents.Where(n => string.Equals(n.GroupName, GroupName)).ToList();

        public List<GraphNode> ChildsInSameGroup
            => Childs.Where(n => string.Equals(n.GroupName, GroupName)).ToList();

        public bool HaveNoDependencies
            => Childs.Count(n => n.IsVisible) == 0 && Parents.Count(n => n.IsVisible) == 0;

        public bool HaveNoDependenciesInSameGroup
            => ChildsInSameGroup.Count(n => n.IsVisible) == 0 && ParentsInSameGroup.Count(n => n.IsVisible) == 0;

        private readonly GraphNodePreviewLoader previewLoader;

        public GraphNode(string guid, string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Guid = guid;
            Path = path;
            Childs = new ConcurrentBag<GraphNode>();
            Parents = new ConcurrentBag<GraphNode>();
            Dependencies = new List<string>();
            ParentsScenes = new ConcurrentBag<string>();
            Depth = 0;
            GroupName = string.Empty;
            IsCorrupted = false;
            IsVisible = false;

            FindAssetType(Path);
            previewLoader = new GraphNodePreviewLoader(Path, Type);
        }

        public int CompareTo(object obj)
        {
            GraphNode node = (GraphNode)obj;
            if (IsCorrupted || node.IsCorrupted)
            {
                return IsCorrupted.CompareTo(node.IsCorrupted);
            }
            else if (HaveNoDependenciesInSameGroup && node.HaveNoDependenciesInSameGroup)
            {
                return Name.CompareTo(node.Name);
            }
            else if (Depth == node.Depth)
            {
                if (ParentsInSameGroup.Count != 0 && node.ParentsInSameGroup.Count != 0)
                {
                    if (ParentsInSameGroup[0] == node.ParentsInSameGroup[0])
                    {
                        return Name.CompareTo(node.Name);
                    }
                    else
                    {
                        return 0; // To prevent cyclic call
                    }
                }
                else
                {
                    return Name.CompareTo(node.Name);
                }
            }
            else
            {
                return Depth.CompareTo(node.Depth);
            }
        }

        public void LoadAssetInformation(Vector2 canvasResolution)
        {
            if (Type == AssetType.Prefab || Type == AssetType.Model)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
                GetSize(prefab);
                Image = previewLoader.LoadPreview(prefab, canvasResolution);
            }
            else if (Type == AssetType.Material)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(Path);
                GetSize(material);
                Image = previewLoader.LoadPreview(material, canvasResolution);
            }
            else
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(Path, typeof(object));
                GetSize(asset);
                Image = previewLoader.LoadPreview(asset, canvasResolution);
            }
        }

        private void GetSize(UnityEngine.Object asset)
        {
            try
            {
                Size = BytesToString(Profiler.GetRuntimeMemorySizeLong(asset));
            }
            catch
            {
                IsCorrupted = true;
            }
        }

        private string BytesToString(long byteCount)
        {
            string[] suffix = { "B", "KB", "MB", "GB" };

            if (byteCount == 0)
                return "0" + suffix[0];

            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double number = Math.Round(bytes / Math.Pow(1024, place), 1);

            return (Math.Sign(byteCount) * number) + suffix[place];
        }

        private void FindAssetType(string path)
        {
            string extension = System.IO.Path.GetExtension(path).Replace(".", "");

            switch (extension)
            {
                case "anim":
                    Type = AssetType.AnimationClip;
                    break;
                case "ttf":
                    Type = AssetType.Font;
                    break;
                case "mat":
                    Type = AssetType.Material;
                    break;
                case "fbx":
                case "obj":
                    Type = AssetType.Model;
                    break;
                case "physicMaterial":
                    Type = AssetType.PhysicalMaterial;
                    break;
                case "prefab":
                    Type = AssetType.Prefab;
                    break;
                case "unity":
                    Type = AssetType.Scene;
                    break;
                case "cs":
                    Type = AssetType.Script;
                    break;
                case "shader":
                case "shadergraph":
                    Type = AssetType.Shader;
                    break;
                case "png":
                case "jpg":
                case "psd":
                    Type = AssetType.Sprite;
                    break;
                case "mp4":
                    Type = AssetType.VideoClip;
                    break;
                case "controller":
                    Type = AssetType.AnimatorController;
                    break;
                case "wav":
                    Type = AssetType.AudioClip;
                    break;
                case "asset":
                    Type = AssetType.ScriptableObject;
                    break;
                default:
                    Type = AssetType.Unknown;
                    break;
            }
        }
    }
}