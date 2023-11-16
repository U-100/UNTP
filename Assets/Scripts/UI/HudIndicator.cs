using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UNTP
{
    public class HudIndicator : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<HudIndicator, UxmlTraits> {}
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlFloatAttributeDescription _iconSize = new() { name = "--icon-size", defaultValue = 40 };
            private readonly UxmlFloatAttributeDescription _markSize = new() { name = "--mark-size", defaultValue = 20 };
            private readonly UxmlFloatAttributeDescription _markSpacing = new() { name = "--mark-spacing", defaultValue = 5 };
            private readonly UxmlFloatAttributeDescription _xFactor = new() { name = nameof(HudIndicator.xFactor) };
            private readonly UxmlFloatAttributeDescription _yFactor = new() { name = nameof(HudIndicator.yFactor) };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                
                HudIndicator hudIndicator = (HudIndicator)ve;
                
                hudIndicator.iconSize = this._iconSize.GetValueFromBag(bag, cc);
                hudIndicator.markSize = this._markSize.GetValueFromBag(bag, cc);
                hudIndicator.markSpacing = this._markSpacing.GetValueFromBag(bag, cc);
                hudIndicator.xFactor = this._xFactor.GetValueFromBag(bag, cc);
                hudIndicator.yFactor = this._yFactor.GetValueFromBag(bag, cc);
            }
        }

        public const string USS_CLASS_HUD_INDICATOR = "hud-indicator";
        public const string USS_CLASS_HUD_INDICATOR_INNER_AREA = "hud-indicator__inner-area";
        public const string USS_CLASS_HUD_INDICATOR_CONTENT_AREA = "hud-indicator__content-area";
        public const string USS_CLASS_HUD_INDICATOR_MARK = "hud-indicator__mark";

        private VisualElement _innerArea;
        private VisualElement _positioningPoint;
        private VisualElement _contentArea;
        private VisualElement _rotationPoint;
        private VisualElement _mark;
        
        public HudIndicator()
        {
            AddToClassList(USS_CLASS_HUD_INDICATOR);

            this._innerArea = new VisualElement();
            this._innerArea.name = nameof(this._innerArea);
            this._innerArea.AddToClassList(USS_CLASS_HUD_INDICATOR_INNER_AREA);
            this._innerArea.style.flexGrow = 1;

            this._positioningPoint = new VisualElement();
            this._positioningPoint.name = nameof(this._positioningPoint);
            this._positioningPoint.style.position = Position.Absolute; 
            this._innerArea.Add(this._positioningPoint);
            
            this._contentArea = new VisualElement();
            this._contentArea.name = nameof(this._contentArea);
            this._contentArea.AddToClassList(USS_CLASS_HUD_INDICATOR_CONTENT_AREA);
            this._contentArea.style.position = Position.Absolute;
            this._positioningPoint.Add(this._contentArea);
            
            this._rotationPoint = new VisualElement();
            this._rotationPoint.name = nameof(this._rotationPoint);
            this._rotationPoint.style.position = Position.Absolute; 
            this._positioningPoint.Add(this._rotationPoint);
            
            this._mark = new VisualElement();
            this._mark.name = nameof(this._mark);
            this._mark.AddToClassList(USS_CLASS_HUD_INDICATOR_MARK);
            this._mark.style.position = Position.Absolute;
            this._rotationPoint.Add(this._mark);
            
            Add(this._innerArea);
            
            RegisterCallback<CustomStyleResolvedEvent>(OnStylesResolved);
        }

        private float _iconSize;
        public float iconSize { get => this._iconSize; set { this._iconSize = value; ApplySizeAndSpacing(); } }

        private float _markSize;
        public float markSize { get => this._markSize; set { this._markSize = value; ApplySizeAndSpacing(); } }

        private float _markSpacing;
        public float markSpacing { get => this._markSpacing; set { this._markSpacing = value; ApplySizeAndSpacing(); } }

        private float _xFactor;
        public float xFactor { get => this._xFactor; set { this._xFactor = value; ApplyFactors(); } }

        private float _yFactor; public float yFactor { get => this._yFactor; set { this._yFactor = value; ApplyFactors(); } }

        private static readonly CustomStyleProperty<float> _iconSizeStyleProperty = new CustomStyleProperty<float>("--icon-size");
        private static readonly CustomStyleProperty<float> _markSizeStyleProperty = new CustomStyleProperty<float>("--mark-size");
        private static readonly CustomStyleProperty<float> _markSpacingStyleProperty = new CustomStyleProperty<float>("--mark-spacing");
        
        void OnStylesResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(_iconSizeStyleProperty, out float newIconSize))
                this.iconSize = newIconSize;
            
            if(evt.customStyle.TryGetValue(_markSizeStyleProperty, out float newMarkSize))
                this.markSize = newMarkSize;
            
            if(evt.customStyle.TryGetValue(_markSpacingStyleProperty, out float newMarkSpacing))
                this.markSpacing = newMarkSpacing;
        }
        
        private void ApplySizeAndSpacing()
        {
            this.style.paddingLeft = this.style.paddingRight = this.style.paddingTop = this.style.paddingBottom = this.iconSize / 2;
            this._contentArea.style.width = this._contentArea.style.height = this.iconSize;
            this._contentArea.style.left = this._contentArea.style.top = -1 * (this.iconSize / 2);

            this._mark.style.width = this._mark.style.height = this.markSize;
            this._mark.style.left = -1 * (this.markSize / 2);
            this._mark.style.top = - this.iconSize / 2 - this.markSize - this.markSpacing;
        }

        private void ApplyFactors()
        {
            this._positioningPoint.style.left = Length.Percent(this.xFactor * 100);
            this._positioningPoint.style.top = Length.Percent(this.yFactor * 100);

            this._rotationPoint.transform.rotation = Quaternion.FromToRotation(new Vector3(0, -1, 0), new Vector3(.5f, .5f, 0) - new Vector3(this.xFactor, this.yFactor));
        }
    }
}
