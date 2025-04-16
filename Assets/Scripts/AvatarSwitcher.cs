using UnityEngine;

public class AvatarSwitcher : MonoBehaviour
{
    public GameObject male;
    public GameObject female;

    public MicrophoneManager malemm;

    public MicrophoneManager femalemm;

    private bool usingMale = false;

    void Start()
    {
        SwitchAvatar(usingMale);
    }

    public void SwitchAvatar(bool useMale)
    {
        usingMale = useMale;
        male.SetActive(usingMale);
        female.SetActive(!usingMale);
    }

    public void ToggleAvatar()
    {
        SwitchAvatar(!usingMale);
    }

    public void OnAudioRecorded(string base64Audio)
    {
        if (usingMale)
            malemm.OnAudioRecorded(base64Audio);
        else femalemm.OnAudioRecorded(base64Audio);
    }
}
