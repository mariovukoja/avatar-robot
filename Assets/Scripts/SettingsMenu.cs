using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public GameObject popupPanel;
    public GameObject commandsPanel;
    public GameObject Record;

    public void TogglePopup()
    {
        popupPanel.SetActive(!popupPanel.activeSelf);
        commandsPanel.SetActive(!commandsPanel.activeSelf);
        Record.SetActive(!Record.activeSelf);
    }
}
