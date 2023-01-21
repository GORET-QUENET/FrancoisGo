using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class GraphToolbar : Toolbar
    {
        public Dictionary<string, bool> Groups;
        public Dictionary<string, bool> Types;
        public string[] Folders => folders
                                        .ToList()
                                        .Where(f => f.Value)
                                        .Select(f => $"Assets/{f.Key}")
                                        .ToArray();
        public string NameFilter
            => toolbarSearchField.value;

        private Dictionary<string, bool> folders;
        private ToolbarSearchField toolbarSearchField;
        private Label counterLabel;

        private const string toolbarFolders = "toolbar_folders";
        private const string toolbarGroups = "toolbar_groups";
        private const string toolbarTypes = "toolbar_types";
        private const string toolbarFilters = "toolbar_filter";

        public GraphToolbar(Action displayCallback)
        {
            style.unityTextAlign = TextAnchor.MiddleCenter;
            PopulateFolders();
            PopulateGroups(true);
            PopulateTypes();

            AddToolbarDictionaryMenu("Folders", 0, folders, "Filter by root folders in the Assets folder");
            AddToolbarDictionaryMenu("Groups", 1, Groups, "Filter by groups and scenes");
            AddToolbarDictionaryMenu("Types", 2, Types, "Filter by asset type");

            AddSearchField();
            AddDisplayButton(displayCallback);
            AddCounter();
        }

        public void SetCounter(string value)
        {
            counterLabel.text = value;
        }

        private void PopulateFolders()
        {
            if (PlayerPrefs.HasKey(toolbarFolders))
            {
                LoadDictionary(toolbarFolders, ref folders);
                return;
            }

            string path = Application.dataPath + "/";
            string[] directories = Directory.GetDirectories(path);

            folders = new Dictionary<string, bool>();

            foreach (var directory in directories)
            {
                folders.Add(directory.Replace(path, ""), false);
            }
        }

        private void PopulateGroups(bool initialisation = false)
        {
            if (initialisation && PlayerPrefs.HasKey(toolbarGroups))
            {
                LoadDictionary(toolbarGroups, ref Groups);
                return;
            }

            List<string> scenesGuids = AssetDatabase.FindAssets("t:Scene", Folders).ToList();
            Groups = new Dictionary<string, bool>();

            foreach (var guid in scenesGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Groups.Add(Path.GetFileNameWithoutExtension(path), true);
            }

            Groups.Add(GroupName.Common.ToString(), true);
            Groups.Add(GroupName.Unused.ToString(), true);
            Groups.Add(GroupName.Corrupted.ToString(), true);
        }

        private void PopulateTypes()
        {
            TypeColorMap typeColorMap = AssetDatabase.LoadAssetAtPath<TypeColorMap>(GraphPath.ColorMap);

            if (PlayerPrefs.HasKey(toolbarTypes))
            {
                LoadDictionary(toolbarTypes, ref Types);

                if (typeColorMap.Types.Count == Types.Count)
                    return;
            }

            Types = new Dictionary<string, bool>();

            foreach (var typeColor in typeColorMap.Types)
            {
                Types.Add(typeColor.Type.ToString(), true);
            }
        }

        private void AddToolbarDictionaryMenu(string title, int position, Dictionary<string, bool> dictionay, string tooltip)
        {
            var toolbarMenu = new ToolbarMenu
            {
                text = title,
                name = title.ToLower(),
                tooltip = tooltip
            };

            toolbarMenu.menu.AppendAction("Everything", a => UpdateDictionary("Everything", dictionay));
            toolbarMenu.menu.AppendAction("Nothing", a => UpdateDictionary("Nothing", dictionay));
            toolbarMenu.menu.AppendSeparator();

            foreach (var group in dictionay)
            {
                toolbarMenu.menu.AppendAction(group.Key,
                    a => UpdateDictionary(group.Key, dictionay),
                    a => dictionay[group.Key] ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            hierarchy.Insert(position, toolbarMenu);
        }

        private void AddSearchField()
        {
            toolbarSearchField = new ToolbarSearchField
            {
                tooltip = "Filter by asset name only (not case sensitive)"
            };

            if (PlayerPrefs.HasKey(toolbarFilters))
            {
                toolbarSearchField.value = PlayerPrefs.GetString(toolbarFilters);
            }

            hierarchy.Add(toolbarSearchField);
        }

        private void AddDisplayButton(Action displayCallback)
        {
            var toolbarButton = new ToolbarButton
            {
                text = "Display",
                name = "display-button",
                tooltip = "Display the dependencies graph"
            };
            toolbarButton.clicked += () => SaveToolbarSelection(toolbarSearchField.value);
            toolbarButton.clicked += displayCallback;
            hierarchy.Add(toolbarButton);
        }

        private void AddCounter()
        {
            hierarchy.Add(new ToolbarSpacer());

            counterLabel = new Label();
            hierarchy.Add(counterLabel);
        }

        private void UpdateDictionary(string key, Dictionary<string, bool> dictionay)
        {
            if (string.Equals(key, "Everything"))
            {
                var keys = new List<string>(dictionay.Keys);
                foreach (string groupKey in keys)
                    dictionay[groupKey] = true;
            }
            else if (string.Equals(key, "Nothing"))
            {
                var keys = new List<string>(dictionay.Keys);
                foreach (string groupKey in keys)
                    dictionay[groupKey] = false;
            }
            else
            {
                dictionay[key] = !dictionay[key];
            }

            if (dictionay == folders)
            {
                RefreshGroupsMenu();
            }
        }

        private void RefreshGroupsMenu()
        {
            PopulateGroups();
            hierarchy.RemoveAt(1);
            AddToolbarDictionaryMenu("Groups", 1, Groups, "Filter by groups and scenes");
        }

        private void SaveToolbarSelection(string filter)
        {
            var foldersDictionary = new ToolbarMenuDictionary(folders);
            byte[] foldersBytes = BytesConvertor.ToByteArray(foldersDictionary);
            string foldersString64 = Convert.ToBase64String(foldersBytes);
            PlayerPrefs.SetString(toolbarFolders, foldersString64);

            var groupsDictionary = new ToolbarMenuDictionary(Groups);
            byte[] groupsBytes = BytesConvertor.ToByteArray(groupsDictionary);
            string groupsString64 = Convert.ToBase64String(groupsBytes);
            PlayerPrefs.SetString(toolbarGroups, groupsString64);

            var typesDictionary = new ToolbarMenuDictionary(Types);
            byte[] typesBytes = BytesConvertor.ToByteArray(typesDictionary);
            string typesString64 = Convert.ToBase64String(typesBytes);
            PlayerPrefs.SetString(toolbarTypes, typesString64);

            PlayerPrefs.SetString(toolbarFilters, filter);
        }

        private void LoadDictionary(string key, ref Dictionary<string, bool> dictionay)
        {
            string string64 = PlayerPrefs.GetString(key);
            byte[] bytes = Convert.FromBase64String(string64);
            ToolbarMenuDictionary groupsDictionary = BytesConvertor.FromByteArray<ToolbarMenuDictionary>(bytes);
            dictionay = groupsDictionary.Dictionary;
        }
    }
}
