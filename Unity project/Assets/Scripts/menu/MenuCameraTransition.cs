using System.Collections;
using UnityEngine;

public class MenuCameraTransition : MonoBehaviour
{
    [Header("Camera")]
    public Transform cam;                  // leave empty, uses the Main Camera
    public Transform settingsViewPoint;    // empty object in front of the wall, facing it

    [Header("UI groups (each needs a CanvasGroup)")]
    public CanvasGroup mainMenuGroup;      // Start / Settings / Quit
    public CanvasGroup settingsGroup;      // the settings on the wall

    [Header("Timing")]
    public float moveTime = 1f;
    public float fadeTime = 0.25f;

    Vector3 mainPos;
    Quaternion mainRot;
    bool moving;
    bool inSettings;

    void Start()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null) { enabled = false; return; }

        // the starting view = the main menu view
        mainPos = cam.position;
        mainRot = cam.rotation;

        SetGroup(mainMenuGroup, 1f, true);
        SetGroup(settingsGroup, 0f, false);
    }

    // hook this to the Settings button
    public void OpenSettings()
    {
        if (moving || inSettings || settingsViewPoint == null) return;
        StartCoroutine(Go(settingsViewPoint.position, settingsViewPoint.rotation, mainMenuGroup, settingsGroup, true));
    }

    // hook this to the Back button
    public void CloseSettings()
    {
        if (moving || !inSettings) return;
        StartCoroutine(Go(mainPos, mainRot, settingsGroup, mainMenuGroup, false));
    }

    IEnumerator Go(Vector3 toPos, Quaternion toRot, CanvasGroup hide, CanvasGroup show, bool settingsOpen)
    {
        moving = true;

        // 1) fade the current UI out
        yield return Fade(hide, 1f, 0f);

        // 2) glide the camera
        Vector3 fromPos = cam.position;
        Quaternion fromRot = cam.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, moveTime);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            cam.position = Vector3.Lerp(fromPos, toPos, e);
            cam.rotation = Quaternion.Slerp(fromRot, toRot, e);
            yield return null;
        }
        cam.position = toPos;
        cam.rotation = toRot;

        // 3) fade the new UI in
        yield return Fade(show, 0f, 1f);

        inSettings = settingsOpen;
        moving = false;
    }

    IEnumerator Fade(CanvasGroup g, float from, float to)
    {
        if (g == null) yield break;

        g.interactable = false;
        g.blocksRaycasts = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime);
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        SetGroup(g, to, to >= 0.99f);
    }

    void SetGroup(CanvasGroup g, float alpha, bool clickable)
    {
        if (g == null) return;
        g.alpha = alpha;
        g.interactable = clickable;
        g.blocksRaycasts = clickable;
    }
}