using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    public GameObject deathPanel;           // drag the red DeathScreen panel here
    public string menuSceneName = "Menu";   // exact name of your menu scene

    void Awake()
    {
        // hidden when the game starts
        deathPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Show()
    {
        deathPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Menu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}