using UnityEngine;

public class WinSceneController : MonoBehaviour
{
    void Start()
    {
        if (GameSystemsTMP.I != null)
        {
            GameSystemsTMP.I.ShowWin();
        }
    }
}
