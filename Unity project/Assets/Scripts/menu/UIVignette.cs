using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIVignette : MonoBehaviour
{
    [Header("Look")]
    public Color edgeColor = Color.black;
    [Range(0f, 1f)] public float maxAlpha = 0.9f;     // how dark the edges get
    [Range(0f, 1f)] public float innerRadius = 0.3f;  // clear area in the middle (0 = starts at the center)
    [Range(0f, 1f)] public float outerRadius = 0.9f;  // where the edge is fully dark (1 = the corners)
    public int size = 256;                            // texture size, 256 is plenty

    Texture2D tex;
    Sprite sprite;

    void Awake()
    {
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // -1..1 from the center. the image is stretched to the screen,
                // so this gives an oval that matches the screen shape
                float nx = (x / (size - 1f)) * 2f - 1f;
                float ny = (y / (size - 1f)) * 2f - 1f;

                float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;   // 0 center, 1 corner
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerRadius, outerRadius, d)) * maxAlpha;

                pixels[y * size + x] = new Color(edgeColor.r, edgeColor.g, edgeColor.b, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);

        Image img = GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;   // so it never blocks your buttons
    }

    void OnDestroy()
    {
        if (sprite != null) Destroy(sprite);
        if (tex != null) Destroy(tex);
    }
}