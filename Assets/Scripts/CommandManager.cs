using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CommandManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Transform contentPanel;
    public ScrollRect scrollRect;
    public List<string> commandsToSend;

    public MicrophoneManager microphoneManager;

    public void AddMessage(string text, int index)
    {
        Debug.Log("dodana " + text + " na indexu: " + index);
        GameObject newMessage = Instantiate(messagePrefab, contentPanel);
        newMessage.SetActive(true);

        Text messageText = newMessage.transform.Find("Text").GetComponent<Text>();
        messageText.text = text;

        Button deleteButton = newMessage.transform.Find("Delete").GetComponent<Button>();
        deleteButton.onClick.RemoveAllListeners();

        int copyInd = index;
        deleteButton.onClick.AddListener(() =>
        {
            commandsToSend[copyInd] = null;
            Destroy(newMessage);
            microphoneManager.commandsToSendcnt--;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        });

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


