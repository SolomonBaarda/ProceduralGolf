using UnityEngine;
using UnityEngine.EventSystems;

public class HeldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool IsPressed { get; protected set; }
    public RectTransform Hitbox;

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        IsPressed = RectTransformUtility.RectangleContainsScreenPoint(Hitbox, eventData.position);
    }
}