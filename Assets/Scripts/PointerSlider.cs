using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PointerSlider : HeldButton, IDragHandler
{
    public TMP_Text DisplayName;
    public TMP_Text DisplayValue;

    public Image Background;
    public Gradient Gradient;

    public Vector2 DeltaPosition { get { if (IsPressed) { return delta; } else { return Vector2.zero; } } }
    private Vector2 delta;




    public new void OnDrag(PointerEventData eventData)
    {
        // Doesn't get called when dragging but not moving
        // Need to reset delta somehow

        base.OnDrag(eventData);

        delta = eventData.delta;
    }



}
