using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;

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

    public Button deleteCommands;
    public Button sendCommands;
    public string Server_uri = "http://127.0.0.1:5005";
    public string Robot_uri = "http://127.0.0.1:5000";
    public bool wasPlaying = false;
    public Color originalButtonColor;
    public Text errorDisplay;
    public Text infoDisplay;
    public Text commandsDebug;
    public string Context { get; set; }
    public CommandManager chatUI;
    public string commandsToSend { get; set; }
    public int commandsToSendcnt;
    public bool isRecording = false;

    [DllImport("__Internal")]
    private static extern void InitMicrophone();

    [DllImport("__Internal")]
    private static extern void StopMicrophone();

    void Start()
    {
        Context = "";
        commandsToSend = "";
        commandsToSendcnt = 0;
        audioSource = GetComponent<AudioSource>();
        audioSource.Stop();
        animator.SetBool("IsTalking", false);
        isRecording = false;
        sendCommands.interactable = false;


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

        wasPlaying = audioSource.isPlaying;
    }

    private void OnServerUriChanged(string newUri)
    {
        Server_uri = newUri;
        Debug.Log("Server URI updated to: " + Server_uri);
    }

    private void OnRobotUriChanged(string newUri)
    {
        Robot_uri = newUri;
        Debug.Log("Server URI updated to: " + Robot_uri);
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
            if (commandsToSendcnt == 5)
            {
                recordButton.interactable = false;
                sendCommands.interactable = true;
                ServerUriInputField.interactable = true;
                deleteCommands.interactable = true;
                var buttonImage = recordButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.red;
                }
                Debug.Log(commandsToSend);
                infoDisplay.text = "INFO: Maximum of 5 commands can be sent in one instance!";
            }
            else
            {
                recordButton.interactable = true;
                if (commandsToSendcnt > 0)
                {
                    sendCommands.interactable = true;
                }
                ServerUriInputField.interactable = true;
                deleteCommands.interactable = true;
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
            buttonText.text = "Record";
            isRecording = false;
            Debug.Log("Stopping microphone recording...");
            StopMicrophone();
            ServerUriInputField.interactable = false;
            recordButton.interactable = false;
            sendCommands.interactable = false;
            deleteCommands.interactable = false;
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.red;
            }
        }
        else
        {
            sendCommands.interactable = false;
            deleteCommands.interactable = false;
            Text buttonText = recordButton.GetComponentInChildren<Text>();
            buttonText.text = "Stop";
            var buttonImage = recordButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color newColor;
                if (ColorUtility.TryParseHtmlString("#FECE2D", out newColor))
                {
                    buttonImage.color = newColor;
                }
            }
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
                Debug.LogError("LLM - Error: " + www.error);
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
        switch (action)
        {
            case "forward":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"{action}(speed: {speed}, duration: {duration})";
                    commandsToSend += $"print(\"Go forward\")\nforward(2.0, 50.0)\n";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "back":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"{action}(speed: {speed}, duration: {duration})";
                    commandsToSend += $"print(\"Go back\")\nback({duration}, {speed})\n";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "turn_left":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"{action}(speed: {speed}, duration: {duration})";
                    commandsToSend += $"print(\"Go left\")\nturn_left({duration}, {speed})\n";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "turn_right":
                {
                    float speed = parameters["speed"]?.ToObject<float>() ?? 0f;
                    float duration = parameters["duration"]?.ToObject<float>() ?? 0f;
                    string message = $"{action}(speed: {speed}, duration: {duration})";
                    commandsToSend += $"print(\"Go right\")\nturn_right({duration}, {speed})\n";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "FollowLine":
                {
                    string color = parameters["color"]?.ToString();
                    string message = $"{action}(color: {color})";
                    commandsToSend += message + ";";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "LookForObject":
                {
                    string description = parameters["description"]?.ToString();
                    string message = $"{action}(description: {description})";
                    commandsToSend += message + ";";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "Abort":
                {
                    string message = $"{action}";
                    commandsToSend += message + ";";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "SetWheelSpeed":
                {
                    string left = parameters["left"]?.ToString();
                    string right = parameters["right"]?.ToString();
                    string duration = parameters["duration"]?.ToString();
                    string message = $"{action}(left: {left}, right: {right}, duration: {duration})";
                    commandsToSend += message + ";";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            case "LookForPerson":
                {
                    string message = $"{action}";
                    commandsToSend += message + ";";
                    commandsToSendcnt++;
                    chatUI.AddMessage(message);
                    break;
                }
            default:
                break;
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
                MTC.AdjustMorphTargets(response);
                infoDisplay.text = string.Empty;
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
            deleteCommands.interactable = true;
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
            Debug.LogError("ErrorDisplay UI element is not assigned!");
        }
    }

    public void SendCommands()
    {
        Debug.Log("Slanje naredbi");
        chatUI.ClearMessages();
        infoDisplay.text = "INFO: Sending commands to the robot!";
        sendCommands.interactable = false;
        recordButton.interactable = true;
        ServerUriInputField.interactable = true;
        var buttonImage = recordButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = originalButtonColor;
        }
        StartCoroutine(PostCommandsCoroutine(commandsToSend));
        commandsToSend = string.Empty;
        commandsToSendcnt = 0;
        StartCoroutine(ClearErrorAfterDelay(5));
    }
    private IEnumerator PostCommandsCoroutine(string commands)
    {
        string json = JsonUtility.ToJson(new CommandPayload { code = commands });
        UnityWebRequest request = new UnityWebRequest(Robot_uri, "POST");
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
        Debug.Log("Brisanje naredbi");
        chatUI.ClearMessages();
        commandsToSend = string.Empty;
        commandsToSendcnt = 0;
        infoDisplay.text = "INFO: Commands deleted!";
        sendCommands.interactable = false;
        recordButton.interactable = true;
        ServerUriInputField.interactable = true;
        var buttonImage = recordButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = originalButtonColor;
        }
        StartCoroutine(ClearErrorAfterDelay(5));
    }

    IEnumerator ClearErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        errorDisplay.text = string.Empty;
        infoDisplay.text = string.Empty;
    }

}
