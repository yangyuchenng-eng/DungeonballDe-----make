using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameStateUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject deathPanel;   
    public GameObject winPanel;     

    private bool gameEnded = false;

    void Start()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!gameEnded) return;

       
        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }

    public void ShowDeath()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ShowWin()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}
