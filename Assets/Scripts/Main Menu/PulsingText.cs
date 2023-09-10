using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class PulsingText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private float initialSize;
    public float HalfMaxFontSizeDecrement = 5;
    public float AnimationSpeed = 1;

    void Start()
    {
        initialSize = text.fontSize;

        StartCoroutine(Animation());
    }

    private IEnumerator Animation()
    {
        for (float t = 0; ; t += Time.deltaTime)
        {
            text.fontSize = initialSize - ((Mathf.Sin(t * AnimationSpeed) + 1) * HalfMaxFontSizeDecrement);
            yield return null;
        }
    }
}
