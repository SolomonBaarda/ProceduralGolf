using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PointerSlider : HeldButton, IDragHandler
{
    public TMP_Text DisplayName;
    public TMP_Text DisplayValue;

    public Image Background;
    public Gradient Gradient;

    public float Sensitivity = 5.0f;

    public Vector2 DeltaPosition { get { if (IsPressed) { return delta; } else { return Vector2.zero; } } }
    private Vector2 delta;
    private bool isDragging;
    private bool coroutineIsRunning;


    public new void OnDrag(PointerEventData eventData)
    {
        // Doesn't get called when dragging but not moving
        // Need to reset delta somehow
        base.OnDrag(eventData);

        delta = eventData.delta * Time.deltaTime * 100.0f * Sensitivity;
        isDragging = true;

        // Start the courotine if we need to
        if (!coroutineIsRunning)
        {
            StartCoroutine(WaitUntilDragStops());
        }
    }

    public void UpdateSensitivity(float value)
    {
        Sensitivity = value;
    }

    private IEnumerator WaitUntilDragStops()
    {
        coroutineIsRunning = true;

        // Stay in here while OnDrag is being called each frame
        while (isDragging)
        {
            // Disable it now and if it is set again next frame we will stay in here
            isDragging = false;
            yield return null;
        }

        isDragging = false;
        delta = Vector2.zero;

        coroutineIsRunning = false;
    }

}
