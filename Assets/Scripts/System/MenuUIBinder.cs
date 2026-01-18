using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuUIBinder : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button startButton;
    public Button sensitivityButton;
    public Button soundButton;
    public Button quitButton;

    [Header("Menu Text (TMP)")]
    public TMP_Text menuSensitivityText;
    public TMP_Text menuSoundText;

    void Start()
    {
        // ?? GameSystemsTMP ????
        if (GameSystemsTMP.I == null)
        {
            Debug.LogError("[MenuUIBinder] GameSystemsTMP.I is null. ??? MenuScene ?? GlobalSystems ??? GameSystemsTMP?");
            return;
        }

        // ??????????????????????????/???
        GameSystemsTMP.I.BindMenuTexts(menuSensitivityText, menuSoundText);

        // ?????????????????????“??????”?
        BindButton(startButton, GameSystemsTMP.I.StartGame);
        BindButton(sensitivityButton, GameSystemsTMP.I.CycleSensitivity);
        BindButton(soundButton, GameSystemsTMP.I.ToggleSound);
        BindButton(quitButton, GameSystemsTMP.I.QuitGame);
    }

    void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }
}
