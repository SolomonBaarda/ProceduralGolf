using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class Screenshot : MonoBehaviour
{
    public int Width = 1920;
    public int Height = 1080;


    public static string GetScreenshotName()
    {
        string path = Application.dataPath;
        return string.Format("{0}/Screenshots/{1}.png", path, System.DateTime.Now.ToString("yyyy.MM.dd@HH.mm.ss"));
    }



    public static void TakeBasicScreenshot()
    {
        string path = GetScreenshotName();
        ScreenCapture.CaptureScreenshot(path, ScreenCapture.StereoScreenCaptureMode.BothEyes);

        Logger.Log("Saved screenshot at: " + path);
    }



    public void TakeFullResolutionScreenshot()
    {
        StartCoroutine(WaitForEndOfFrameThenCapture());
    }

    public IEnumerator WaitForEndOfFrameThenCapture()
    {
        yield return new WaitForEndOfFrame();

        Camera c = GetComponent<Camera>();


        // Create render texture 
        RenderTexture rt = new RenderTexture(Width, Height, 24);
        Texture2D screenShot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        // Draw the camera view to the texture
        c.targetTexture = rt;
        c.Render();



        

        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        c.targetTexture = null;
        RenderTexture.active = null; 

        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();


        string filename = GetScreenshotName();
        System.IO.File.WriteAllBytes(filename, bytes);

        Logger.Log("Saved screenshot at: " + filename);


    }
}