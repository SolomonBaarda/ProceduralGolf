using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Button")]
    public ColorBlock Colours = new ColorBlock();

    private void Start()
    {
        // Apply the preset to all buttons in the scene
        var buttons = FindObjectsOfType<Button>(true); // Include inactive

        foreach (var button in buttons)
        {
            button.colors = Colours;
        }

        Debug.Log($"Applied colour preset to {buttons.Length} buttons");
    }

}
