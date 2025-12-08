using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// AI-assisted: manages win/lose UI, pausing, and restart.
public class GameStateUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject deathPanel;   // “你死了”
    public GameObject winPanel;     // “你赢了”

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

        // 游戏结束后按 R 重新开始当前场景
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
