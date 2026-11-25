using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels (not switched on/off anymore, MenuCameraTransition fades them)")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Settings UI")]
    public Slider fovSlider;
    public Slider sensitivitySlider;
    public Toggle fullscreenToggle;

    [Header("Value labels (optional)")]
    public TextMeshProUGUI fovText;
    public TextMeshProUGUI sensitivityText;

    void Start()
    {
        float fov = PlayerPrefs.GetFloat("FOV", 60f);
        float sens = PlayerPrefs.GetFloat("Sensitivity", 1.5f);
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        Screen.fullScreen = fullscreen;

        if (fovSlider)
        {
            fovSlider.minValue = 60f;
            fovSlider.maxValue = 80f;
            fovSlider.wholeNumbers = true;
            fovSlider.value = fov;
            fovSlider.onValueChanged.AddListener(SetFOV);
        }

        if (sensitivitySlider)
        {
            sensitivitySlider.minValue = 0.5f;
            sensitivitySlider.maxValue = 6f;
            sensitivitySlider.value = sens;
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        if (fullscreenToggle)
        {
            fullscreenToggle.isOn = fullscreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        UpdateLabels(fov, sens);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // kept so old button references don't break, but they no longer switch panels.
    // the camera script (MenuCameraTransition) opens and closes the settings now.
    public void OpenSettings()
    {
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
    }

    public void SetFOV(float value)
    {
        PlayerPrefs.SetFloat("FOV", value);
        UpdateLabels(value, PlayerPrefs.GetFloat("Sensitivity", 2f));
    }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat("Sensitivity", value);
        UpdateLabels(PlayerPrefs.GetFloat("FOV", 80f), value);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    void UpdateLabels(float fov, float sens)
    {
        if (fovText) fovText.text = "FOV: " + Mathf.RoundToInt(fov);
        if (sensitivityText) sensitivityText.text = "Sensitivity: " + sens.ToString("0.0");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}