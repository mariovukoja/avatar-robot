using UnityEngine;
using UnityEngine.UI;

public class CommandManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Transform contentPanel;
    public ScrollRect scrollRect;

    public void AddMessage(string text)
    {
        GameObject newMessage = Instantiate(messagePrefab, contentPanel);
        newMessage.GetComponent<Text>().text = text;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
    
    public void ClearMessages()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
