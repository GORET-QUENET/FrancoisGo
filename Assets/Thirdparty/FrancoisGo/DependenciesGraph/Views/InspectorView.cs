using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public enum GroupDirection
    {
        Vertical = 0,
        Horizontal = 1
    }

    public class InspectorView : VisualElement
    {
        public Vector2 CanvasResolution
            => canvasResolution.value;

        public int GridColumns
            => rangeIntegerField.value;

        public GroupDirection GroupsDirection
            => (GroupDirection)groupsDirection.value;

        private Vector2Field canvasResolution;
        private RangeIntegerField rangeIntegerField;
        private EnumField groupsDirection;

        private const string inspectorResolution = "inpector_resolution";
        private const string inspectorGridColumns = "inpector_grid-columns";
        private const string inspectorGroupsDirection = "inpector_groups-direction";

        public InspectorView()
        {
            canvasResolution = new Vector2Field("Canvas Resolution");
            RemoveSpacer(canvasResolution);
            canvasResolution.RegisterCallback<ChangeEvent<Vector2>>(OnResolutionChanged);
            canvasResolution.tooltip = "It allow to size the canvas for the preview of the UI prefab in the project";
            hierarchy.Add(canvasResolution);

            rangeIntegerField = new RangeIntegerField("Grid Columns", 1, 15);
            rangeIntegerField.RegisterValueChangedCallback(OnGridColumnsChanged);
            rangeIntegerField.tooltip = "It allow to change the number of columns in the grid part of the groups (the grid part is the one without edge links)";
            hierarchy.Add(rangeIntegerField);

            groupsDirection = new EnumField("Groups Disposition");
            groupsDirection.RegisterValueChangedCallback(OnGroupsDirectionChanged);
            groupsDirection.tooltip = "It allow to change orientation of the groups alignement";
            hierarchy.Add(groupsDirection);

            LoadFields();
        }

        private void LoadFields()
        {
            float x = PlayerPrefs.GetFloat($"{inspectorResolution}_X", 0);
            float y = PlayerPrefs.GetFloat($"{inspectorResolution}_Y", 0);
            canvasResolution.value = new Vector2(x, y);

            int gridColumns = PlayerPrefs.GetInt(inspectorGridColumns, 7);
            rangeIntegerField.value = gridColumns;

            string direction = PlayerPrefs.GetString(inspectorGroupsDirection, "Vertical");
            GroupDirection groupDirection = (GroupDirection)Enum.Parse(typeof(GroupDirection), direction);
            groupsDirection.Init(groupDirection);
        }

        private void OnResolutionChanged(ChangeEvent<Vector2> evt)
        {
            PlayerPrefs.SetFloat($"{inspectorResolution}_X", evt.newValue.x);
            PlayerPrefs.SetFloat($"{inspectorResolution}_Y", evt.newValue.y);
        }


        private void OnGridColumnsChanged(ChangeEvent<int> evt)
        {
            PlayerPrefs.SetInt(inspectorGridColumns, evt.newValue);
        }

        private void OnGroupsDirectionChanged(ChangeEvent<Enum> evt)
        {
            PlayerPrefs.SetString(inspectorGroupsDirection, evt.newValue.ToString());
        }


        // This spacer is made to aligne with vector3 fields but in my case I doesn't need it.
        private void RemoveSpacer(Vector2Field field)
        {
            var container = field.Q(className: "unity-base-field__input");
            var spacer = container.Q(className: "unity-composite-field__field-spacer");
            container.Remove(spacer);
        }
    }
}
