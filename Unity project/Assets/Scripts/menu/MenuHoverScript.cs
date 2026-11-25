using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuHoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Setup")]
    public TMP_Text text;                 // leave empty, found in the button's children

    [Header("Hover look")]
    public float hoverScale = 1.15f;      // how much bigger it gets
    public Color hoverColor = new Color(1f, 0.25f, 0.25f);   // red
    public float speed = 12f;             // how fast it changes

    Vector3 startScale;
    Color startColor;
    bool hovering;

    void Awake()
    {
        if (text == null) text = GetComponentInChildren<TMP_Text>();
        if (text == null) { enabled = false; return; }

        startScale = text.transform.localScale;
        startColor = text.color;
    }

    void OnEnable()
    {
        hovering = false;
        if (text != null)
        {
            text.transform.localScale = startScale;
            text.color = startColor;
        }
    }

    void Update()
    {
        // unscaled time so it also works while the game is paused
        float t = speed * Time.unscaledDeltaTime;

        Vector3 targetScale = hovering ? startScale * hoverScale : startScale;
        Color targetColor = hovering ? hoverColor : startColor;

        text.transform.localScale = Vector3.Lerp(text.transform.localScale, targetScale, t);
        text.color = Color.Lerp(text.color, targetColor, t);
    }

    public void OnPointerEnter(PointerEventData eventData) { hovering = true; }
    public void OnPointerExit(PointerEventData eventData) { hovering = false; }
}