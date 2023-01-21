using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DependenciesGraph
{
    public class RangeIntegerField : VisualElement, INotifyValueChanged<int>
    {
        public int value
        {
            get
            {
                return fieldInteger.value;
            }
            set
            {
                SetValueWithoutNotify(value);
            }
        }

        private SliderInt fieldSlider;
        private IntegerField fieldInteger;

        public RangeIntegerField(string label, int min, int max)
        {
            AddToClassList("unity-composite-field");
            AddToClassList("unity-base-field");

            var fieldLabel = new Label(label);
            fieldLabel.AddToClassList("unity-text-element");
            fieldLabel.AddToClassList("unity-label");
            fieldLabel.AddToClassList("unity-base-field__label");
            hierarchy.Add(fieldLabel);

            var fieldInput = new VisualElement();
            fieldInput.AddToClassList("unity-base-field__input");
            fieldInput.AddToClassList("unity-composite-field__input");
            hierarchy.Add(fieldInput);

            fieldSlider = new SliderInt();
            fieldSlider.lowValue = min;
            fieldSlider.highValue = max;
            fieldSlider.style.flexGrow = 3;
            fieldSlider.RegisterValueChangedCallback(ValueChanged);
            fieldInput.Add(fieldSlider);

            fieldInteger = new IntegerField();
            fieldInteger.AddToClassList("unity-composite-field__field");
            fieldInteger.RegisterValueChangedCallback(ValueChanged);
            fieldInput.Add(fieldInteger);
        }

        public void SetValueWithoutNotify(int newValue)
        {
            fieldInteger.SetValueWithoutNotify(newValue);
            fieldSlider.SetValueWithoutNotify(newValue);
        }

        private void ValueChanged(ChangeEvent<int> evt)
        {
            value = evt.newValue;
        }
    }
}
