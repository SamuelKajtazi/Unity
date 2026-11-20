using System.Collections;
using UnityEngine;

public class KatanaSwing : MonoBehaviour
{
    [Header("Input")]
    public int mouseButton = 0;          // 0 = left click (swing)
    public float cooldown = 0.1f;        // extra delay after a swing ends

    [Header("Timing (seconds)")]
    public float windupTime = 0.12f;     // pulling the katana back to the right
    public float slashTime = 0.12f;      // the fast swipe across the screen
    public float recoverTime = 0.25f;    // coming back to the resting pose

    [Header("Wind-up pose (right side, pulled back)")]
    // X = right, Y = up, Z = forward (relative to the camera)
    public Vector3 windupPosition = new Vector3(0.35f, -0.05f, -0.05f);
    public Vector3 windupRotation = new Vector3(0f, 25f, -20f);

    [Header("Slash end pose (left side)")]
    public Vector3 slashPosition = new Vector3(-0.45f, -0.05f, 0.15f);
    public Vector3 slashRotation = new Vector3(0f, -45f, 20f);

    [Header("Flip (hold right click)")]
    public int flipMouseButton = 1;                          // 1 = right click
    public Vector3 flipRotation = new Vector3(0f, 0f, 180f); // 180 around Z flips it over (try X = 180 if it looks wrong)
    public Vector3 flipPosition = Vector3.zero;              // nudge the katana while flipped (relative to the camera)
    public float flipSpeed = 8f;                             // higher = faster flip in and out

    [Header("Grapple requirement")]
    public KatanaGrapple grapple;    // leave empty, found on this object. Flip only works when it says you can grapple

    Vector3 restPos;
    Quaternion restRot;
    bool swinging;
    float nextSwingTime;

    // current swing offset (set by the swing coroutine)
    Vector3 swingPos;
    Vector3 swingRot;

    // 0 = normal, 1 = fully flipped
    float flipAmount;
    bool flipHeld;

    void Start()
    {
        restPos = transform.localPosition;
        restRot = transform.localRotation;

        if (grapple == null) grapple = GetComponent<KatanaGrapple>();
    }

    void Update()
    {
        if (Time.timeScale == 0f || PauseMenu.GameIsPaused) return;

        if (Input.GetMouseButtonDown(mouseButton) && !swinging && Time.time >= nextSwingTime)
            StartCoroutine(Swing());

        // right click only flips if you are in range and looking at a grapple block
        if (Input.GetMouseButtonDown(flipMouseButton))
            flipHeld = (grapple == null) || grapple.LookingAtTarget();

        // let go = back to normal
        if (!Input.GetMouseButton(flipMouseButton))
            flipHeld = false;

        flipAmount = Mathf.MoveTowards(flipAmount, flipHeld ? 1f : 0f, flipSpeed * Time.deltaTime);
    }

    // LateUpdate: puts the swing and the flip together into one pose
    void LateUpdate()
    {
        float f = Mathf.SmoothStep(0f, 1f, flipAmount);

        transform.localPosition = restPos + swingPos + flipPosition * f;
        transform.localRotation = Quaternion.Euler(swingRot) * Quaternion.Euler(flipRotation * f) * restRot;
    }

    IEnumerator Swing()
    {
        swinging = true;

        // 1) wind-up: pull back to the right
        yield return Move(Vector3.zero, Vector3.zero, windupPosition, windupRotation, windupTime, false);

        // 2) slash: fast swipe from right to left
        yield return Move(windupPosition, windupRotation, slashPosition, slashRotation, slashTime, true);

        // 3) recover: back to the resting pose
        yield return Move(slashPosition, slashRotation, Vector3.zero, Vector3.zero, recoverTime, false);

        swingPos = Vector3.zero;
        swingRot = Vector3.zero;

        swinging = false;
        nextSwingTime = Time.time + cooldown;
    }

    IEnumerator Move(Vector3 fromPos, Vector3 fromRot, Vector3 toPos, Vector3 toRot, float duration, bool snappy)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            float p = Mathf.Clamp01(t);

            // snappy = starts very fast and slows at the end (good for the slash)
            float e = snappy ? Mathf.Sin(p * Mathf.PI * 0.5f) : Mathf.SmoothStep(0f, 1f, p);

            swingPos = Vector3.Lerp(fromPos, toPos, e);
            swingRot = Vector3.Lerp(fromRot, toRot, e);
            yield return null;
        }
    }
}