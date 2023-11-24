using UnityEngine.UIElements;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace UNTP
{
    [UxmlElement]
    public partial class HudFireStick : VisualElement
    {
        public const string USS_CLASS_HUD_FIRE_STICK = "hud-fire-stick";
        public const string USS_CLASS_HUD_FIRE_STICK_ACTIVE = USS_CLASS_HUD_FIRE_STICK + "--active";
        public const string USS_CLASS_HUD_FIRE_STICK_KNOB = USS_CLASS_HUD_FIRE_STICK + "__knob";
        public const string USS_CLASS_HUD_FIRE_STICK_KNOB_ACTIVE = USS_CLASS_HUD_FIRE_STICK_KNOB + "--active";

        private VisualElement _knobParent;
        private VisualElement _knob;
        private bool _active;
        
        public HudFireStick()
        {
            AddToClassList(USS_CLASS_HUD_FIRE_STICK);
            
            this._knobParent = new VisualElement();
            this._knobParent.name = nameof(this._knobParent);
            this._knobParent.style.position = Position.Absolute;
            this._knobParent.visible = false;
            Add(this._knobParent);
            
            this._knob = new VisualElement();
            this._knob.name = nameof(this._knob);
            this._knob.AddToClassList(USS_CLASS_HUD_FIRE_STICK_KNOB);
            this._knobParent.Add(this._knob);
            
            RegisterCallback<PointerDownEvent>(this.OnPointerDown);
            RegisterCallback<PointerUpEvent>(this.OnPointerUp);
            RegisterCallback<PointerMoveEvent>(this.OnPointerMove);
        }

        private void SetActive(bool active)
        {
            this._active = active;
            this._knobParent.visible = this._active;
            EnableInClassList(USS_CLASS_HUD_FIRE_STICK_ACTIVE, this._active);
            this._knob.EnableInClassList(USS_CLASS_HUD_FIRE_STICK_KNOB_ACTIVE, this._active);
            if (!this._active)
                this.value = null;
        }

        public float2? value { get; private set; }

        private void OnPointerDown(PointerDownEvent evt)
        {
            this.CapturePointer(evt.pointerId);
            SetActive(true);
            SetKnobPositionFromEventLocalPosition(evt.localPosition);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            this.ReleasePointer(evt.pointerId);
            SetActive(false);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if(this._active)
                SetKnobPositionFromEventLocalPosition(evt.localPosition);
        }

        private void SetKnobPositionFromEventLocalPosition(Vector3 localPosition)
        {
            Vector2 size = this.layout.size;
            Vector2 center = size / 2;
            Vector2 correctedLocalPosition = new Vector2(localPosition.x, localPosition.y);
            Vector2 delta = correctedLocalPosition - center;
            delta = Vector2.ClampMagnitude(delta, center.magnitude);
            correctedLocalPosition = center + delta;
            
            this._knobParent.style.left = correctedLocalPosition.x;
            this._knobParent.style.top = correctedLocalPosition.y;

            this.value = normalizesafe(delta) * float2(1, -1); // we need to invert Y-axis for it to point upwards (in UI Toolkit coordinate system Y-axis points downwards)
        }
    }
}
