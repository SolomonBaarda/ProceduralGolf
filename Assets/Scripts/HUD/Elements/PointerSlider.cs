using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEditor.PackageManager.UI;

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


    private Vector2 window = new Vector2();

    private void Awake()
    {
        window = new Vector2(Screen.width, Screen.height);
    }

    public new void OnDrag(PointerEventData eventData)
    {
        // Doesn't get called when dragging but not moving
        // Need to reset delta somehow
        base.OnDrag(eventData);

        delta = eventData.delta / window * Time.deltaTime * 10000.0f * Sensitivity;
        isDragging = true;

        // Start the courotine if we need to
        if (!coroutineIsRunning)
        {
            StartCoroutine(WaitUntilDragStops());
        }
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
