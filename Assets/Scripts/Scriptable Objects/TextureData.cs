using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureData : ScriptableObject
{


    public void ApplyToMaterial(Material m)
    {
        /*
        Texture t = m.GetTexture("_BaseMap");
        Vector2 scale = new Vector2((float)(width / t.width), (float)(height / t.height));
        meshRenderer.material.SetTextureScale("_BaseMap", scale);
        */
    }
}
