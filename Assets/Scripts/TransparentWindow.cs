using SatorImaging.AppWindowUtility;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                                              int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] static extern int  GetSystemMetrics(int nIndex);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_NOSIZE       = 0x0001;
    const int  SM_CXSCREEN      = 0;
    const int  SM_CYSCREEN      = 1;

    void Awake()
    {
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = 60;

#if !UNITY_EDITOR
        SetWindowPos(WinApi.GetUnityWindowHandle(), HWND_TOPMOST,
                     -9999, -9999, 0, 0, SWP_NOSIZE);
#endif
    }

    void Start()
    {
        QualitySettings.lodBias = 5f;

        Camera cam = Camera.main;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 1f, 0f, 1f);
        cam.allowHDR        = false;
        cam.allowMSAA       = false;

        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f, 1f);

        foreach (Light light in FindObjectsOfType<Light>())
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity          = 1.2f;
                break;
            }
        }

#if !UNITY_EDITOR
        StartCoroutine(Init());
#endif
    }

    IEnumerator Init()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        int sw = GetSystemMetrics(SM_CXSCREEN);
        int sh = GetSystemMetrics(SM_CYSCREEN);

        AppWindowUtility.FrameVisibility = false;
        AppWindowUtility.SetKeyingColor(0, 255, 0);
        AppWindowUtility.AlwaysOnTop     = true;

        SetWindowPos(WinApi.GetUnityWindowHandle(), HWND_TOPMOST, 0, 0, sw, sh, SWP_FRAMECHANGED);

        Debug.Log($"[TransparentWindow] screen={sw}x{sh}");
    }
}
