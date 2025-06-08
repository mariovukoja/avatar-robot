using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using TMPro;

public class MicrophoneManager : MonoBehaviour
{
    public MorphTargetController MTC;
    public AudioSource audioSource;
    public Animator animator;
    public string idleAnimState = "Idle";
    public string[] talkingAnimStates = { "Talking 1", "Talking 2", "Talking 3" };
    public int TalkingAnimIndex = 0;
    public InputField ServerUriInputField;
    public InputField RobotUriInputField;
    public Button recordButton;
    public Button sendCommands;
    public Text sendCommandsText;
    public TextMeshProUGUI sendCommandsPlay;
    public Color sendCommmandsTextColor;
    public Button abortButton;
    public string Server_uri = "http://127.0.0.1:5005";
    public string Robot_uri = "http://127.0.0.1:5000";
    public bool wasPlaying = false;
    public Color originalButtonColor;
    public Text errorDisplay;
    public Text infoDisplay;
    public Text commandsDebug;
    public string Context { get; set; }
    public CommandManager chatUI;
    public List<string?> commandsToSend;

    public int commandsToSendcnt;
    public bool isRecording = false;

    public bool isFull = false;

    [DllImport("__Internal")]
    private static extern void InitMicrophone();

    [DllImport("__Internal")]
    private static extern void StopMicrophone();

    void Start()
    {
        Context = "";
        commandsToSendcnt = 0;
        commandsToSend = new List<string?> { null, null, null, null };
        audioSource = GetComponent<AudioSource>();
        audioSource.Stop();
        animator.SetBool("IsTalking", false);
        isRecording = false;
        sendCommands.interactable = false;
        sendCommmandsTextColor = sendCommandsText.color;
        sendCommandsText.color = Color.gray;
        sendCommandsPlay.color = Color.gray;
        abortButton.interactable = true;
        chatUI.commandsToSend = commandsToSend;


        if (ServerUriInputField != null)
        {
            ServerUriInputField.text = Server_uri;
            ServerUriInputField.onValueChanged.AddListener(OnServerUriChanged);
        }

        if (RobotUriInputField != null)
        {
            RobotUriInputField.text = Robot_uri;
            RobotUriInputField.onValueChanged.AddListener(OnRobotUriChanged);
        }

        if (recordButton != null)
        {
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                originalButtonColor = buttonImage.color;
            }
        }
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            if (!animator.GetBool("IsTalking"))
            {
                TalkingAnimIndex = UnityEngine.Random.Range(0, talkingAnimStates.Length);
                animator.SetBool("IsTalking", true);
                animator.CrossFade(talkingAnimStates[TalkingAnimIndex], 0.15f);
            }
        }
        else if (wasPlaying)
        {
            HandleAudioStopped();
        }
        if (commandsToSendcnt == 0)
        {
            sendCommands.interactable = false;
            sendCommandsText.color = Color.gray;
            sendCommandsPlay.color = Color.gray;
        }
        else if (commandsToSendcnt > 0 && commandsToSendcnt < 4 && isFull && !audioSource.isPlaying)
        {
            recordButton.interactable = true;
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = originalButtonColor;
            }
            isFull = false;
        }
        wasPlaying = audioSource.isPlaying;
    }

    private void OnServerUriChanged(string newUri)
    {
        Server_uri = newUri;
    }

    private void OnRobotUriChanged(string newUri)
    {
        Robot_uri = newUri;
    }

    private void HandleAudioStopped()
    {

        if (animator.GetBool("IsTalking"))
        {
            animator.SetBool("IsTalking", false);
            animator.CrossFade(idleAnimState, 0.15f);
        }
        if (recordButton != null)
        {
            if (commandsToSendcnt == 4)
            {
                recordButton.interactable = false;
                sendCommands.interactable = true;
                sendCommandsText.color = sendCommmandsTextColor;
                sendCommandsPlay.color = sendCommmandsTextColor;
                ServerUriInputField.interactable = true;
                var buttonImage = recordButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.black;
                }
                isFull = true;
                infoDisplay.text = "INFO: Maximum of 4 commands can be sent in one instance!";
                StartCoroutine(ClearErrorAfterDelay(5));
            }
            else
            {
                recordButton.interactable = true;
                if (commandsToSendcnt > 0)
                {
                    sendCommands.interactable = true;
                    sendCommandsText.color = sendCommmandsTextColor;
                    sendCommandsPlay.color = sendCommmandsTextColor;
                }
                ServerUriInputField.interactable = true;
                var buttonImage = recordButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = originalButtonColor;
                }
            }

        }
    }

    public void Recording()
    {
        if (isRecording)
        {
            Text buttonText = recordButton.GetComponentInChildren<Text>();
            isRecording = false;
            Debug.Log("Stopping microphone recording...");
            StopMicrophone();
            ServerUriInputField.interactable = false;
            recordButton.interactable = false;
            sendCommands.interactable = false;
            sendCommandsText.color = Color.gray;
            sendCommandsPlay.color = Color.gray;
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.black;
            }
        }
        else
        {
            sendCommands.interactable = false;
            sendCommandsText.color = Color.gray;
            sendCommandsPlay.color = Color.gray;
            Text buttonText = recordButton.GetComponentInChildren<Text>();
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color newColor;
                if (ColorUtility.TryParseHtmlString("#FECE2D", out newColor))
                {
                    buttonImage.color = newColor;
                }
            }
            infoDisplay.text = "INFO: Recording!";
            isRecording = true;
            Debug.Log("Starting microphone recording...");
            InitMicrophone();
        }
    }

    public void OnAudioRecorded(string base64Audio)
    {
        Debug.Log("Received Base64 audio from JavaScript!");
        var payload = new
        {
            base64 = base64Audio,
            context = Context
        };
        string body = JsonConvert.SerializeObject(payload);
        infoDisplay.text = "INFO: Waiting for response...";
        StartCoroutine(GetRequestData(Server_uri, body));
    }

    IEnumerator GetRequestData(string uri, string body)
    {
        using (UnityWebRequest www = UnityWebRequest.Post($"{uri}/chat", $"{body}", "application/json"))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowError("Error: " + www.error);
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                JObject response = JObject.Parse(www.downloadHandler.text);
                Context = response["context"].ToString();
                if (response != null && response["commands"] != null)
                {
                    JArray commandsArray = response["commands"] as JArray;
                    if (commandsArray != null)
                    {
                        foreach (JObject commandObj in commandsArray)
                        {
                            string action = commandObj["action"].ToString();
                            JObject parameters = (JObject)commandObj["parameters"];
                            ParseAction(action, parameters);
                        }
                    }

                }
                StartCoroutine(GetAudio(Server_uri, response));
            }
        }
    }

    void ParseAction(string action, JObject parameters)
    {
        string command = "";
        switch (action)
        {
            case "forward":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"> {action}(speed: {speed}, duration: {duration})";
                    command = $"forward({duration}, {speed})";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "back":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"> {action}(speed: {speed}, duration: {duration})";
                    command = $"back({duration}, {speed})";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "turn_left":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"> {action}(speed: {speed}, duration: {duration})";
                    command = $"turn_left({duration}, {speed})";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "turn_right":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"> {action}(speed: {speed}, duration: {duration})";
                    command = $"turn_right({duration}, {speed})";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "FollowLine":
                {
                    string color = parameters["color"]?.ToString();
                    string message = $"> {action}(color: {color})";
                    command = message + ";";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "LookForObject":
                {
                    string description = parameters["description"]?.ToString();
                    string message = $"> {action}(description: {description})";
                    command = message + ";";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "SetWheelSpeed":
                {
                    string left = parameters["left"]?.ToString();
                    string right = parameters["right"]?.ToString();
                    string duration = parameters["duration"]?.ToString();
                    string message = $"> {action}(left: {left}, right: {right}, duration: {duration})";
                    command = message + ";";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            case "LookForPerson":
                {
                    string message = $"> {action}";
                    command = message + ";";
                    chatUI.AddMessage(message, commandsToSendcnt);
                    commandsToSendcnt++;
                    break;
                }
            default:
                break;
        }
        for (int i = 0; i < 4; i++)
        {
            if (commandsToSend[i] == null)
            {
                commandsToSend[i] = command;
                break;
            }
        }
        Debug.Log(commandsToSend);
        Debug.Log(commandsToSendcnt);
    }

    IEnumerator GetAudio(string uri, JObject response)
    {
        using (UnityWebRequest www = UnityWebRequest.Get($"{uri}/wav"))
        {
            www.downloadHandler = new DownloadHandlerAudioClip(uri, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowError("Error: " + www.error);
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                if (audioSource.clip != null)
                {
                    audioSource.clip.UnloadAudioData();
                    Destroy(audioSource.clip);
                }
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.LoadAudioData();
                while (!clip.isReadyToPlay)
                {
                    yield return null;
                }
                audioSource.clip = clip;
                audioSource.Play();
                StartCoroutine(RemoveAudioClipAfterPlayback(clip.length - 0.1f));
                var payload = new
                {
                    emotion_scores = new
                    {
                        anger = 0,
                        fear = 0,
                        joy = 40,
                        love = 0,
                        sadness = 0,
                        surprise = 0
                    }
                };
                string jsonString = JsonConvert.SerializeObject(payload);
                JObject json = JObject.Parse(jsonString);
                MTC.AdjustMorphTargets(json);
                infoDisplay.text = "INFO: You said: " + Context;

                Debug.Log("Audio is playing.");
                yield return null;
            }
            www.downloadHandler.Dispose();
            www.Dispose();
        }
    }
    private IEnumerator RemoveAudioClipAfterPlayback(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (audioSource.clip != null)
        {
            audioSource.clip.UnloadAudioData();
            Destroy(audioSource.clip);
            audioSource.clip = null;
            var payload = new
            {
                emotion_scores = new
                {
                    anger = 0,
                    fear = 0,
                    joy = 40,
                    love = 0,
                    sadness = 0,
                    surprise = 0
                }
            };
            string jsonString = JsonConvert.SerializeObject(payload);
            JObject json = JObject.Parse(jsonString);
            infoDisplay.text = "";
            MTC.AdjustMorphTargets(json);
        }
    }
    void ShowError(string message)
    {
        if (errorDisplay != null)
        {
            infoDisplay.text = string.Empty;
            errorDisplay.text = message.Replace("\n", " ");
            recordButton.interactable = true;
            sendCommands.interactable = true;
            sendCommandsText.color = sendCommmandsTextColor;
            sendCommandsPlay.color = sendCommmandsTextColor;
            ServerUriInputField.interactable = true;
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = originalButtonColor;
            }
            StartCoroutine(ClearErrorAfterDelay(5));
        }
        else
        {
            Debug.LogError("UI element is not assigned!");
        }
    }

    public void SendCommands()
    {
        Debug.Log("Slanje naredbi");
        chatUI.ClearMessages();
        infoDisplay.text = "INFO: Sending commands to the robot!";
        sendCommands.interactable = false;
        sendCommandsText.color = Color.gray;
        sendCommandsPlay.color = Color.gray;
        recordButton.interactable = true;
        ServerUriInputField.interactable = true;
        var buttonImage = recordButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = originalButtonColor;
        }
        StartCoroutine(PostCommandsCoroutine());
        commandsToSend.Clear();
        for (int i = 0; i < 4; i++)
        {
            commandsToSend.Add(null);
        }
        commandsToSendcnt = 0;
        StartCoroutine(ClearErrorAfterDelay(5));
    }

    public void Abort()
    {
        infoDisplay.text = "INFO: Aborting actions!";
        StartCoroutine(PostAbort());
        StartCoroutine(ClearErrorAfterDelay(5));
    }

    private IEnumerator PostAbort()
    {
        UnityWebRequest request = new UnityWebRequest(Robot_uri + "/abort", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("POST successful: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("POST failed: " + request.error);
        }
    }


    private IEnumerator PostCommandsCoroutine()
    {
        string allCommands = string.Join("\n", commandsToSend);

        string json = JsonUtility.ToJson(new CommandPayload { code = allCommands });
        UnityWebRequest request = new UnityWebRequest(Robot_uri + "/execute", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("POST successful: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("POST failed: " + request.error);
        }
    }


    [System.Serializable]
    public class CommandPayload
    {
        public string code;
    }

    public void DeleteCommands()
    {
        if (commandsToSendcnt == 0)
        {
            sendCommands.interactable = false;
            sendCommandsText.color = Color.gray;
            sendCommandsPlay.color = Color.gray;
        }
    }

    IEnumerator ClearErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        errorDisplay.text = string.Empty;
        infoDisplay.text = string.Empty;
    }

}

