using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelPortal : MonoBehaviour
{
    public string playerTag = "Player";
    public string nextSceneName;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (string.IsNullOrEmpty(nextSceneName)) return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}
