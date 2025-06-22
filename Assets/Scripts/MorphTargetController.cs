using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Jobs;

public class MorphTargetController : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer = null;
   private readonly Dictionary<string, Dictionary<string, float>> emotionMappings = new Dictionary<string, Dictionary<string, float>>
    {
        {
            "joy", new Dictionary<string, float>
            {
                { "mouthSmile", 0.6f },
                { "mouthSmileLeft", 0.35f },
                { "mouthSmileRight", 0.35f },
                { "mouthOpen", 0.3f },
                { "cheekSquintLeft", 0.6f },
                { "cheekSquintRight", 0.6f },
                { "eyeSquintLeft", 0.4f },
                { "eyeSquintRight", 0.4f },
                { "browOuterUpLeft", 0.3f },
                { "browOuterUpRight", 0.3f }
            }
        },
        {
            "sadness", new Dictionary<string, float>
            {
                { "browDownLeft", 0.75f },
                { "browDownRight", 0.75f },
                { "browInnerUp", 0.7f },
                { "mouthSmile", -0.4f },
                { "mouthLeft", -0.3f },
                { "mouthRight", -0.3f },
                { "eyeWideLeft", -0.2f },
                { "eyeWideRight", -0.2f },
                { "cheekPuff", -0.3f },
                { "jawOpen", 0.2f }
            }
        },
        {
            "anger", new Dictionary<string, float>
            {
                { "browDownLeft", 0.1f },
                { "browDownRight", 0.1f },
                { "browInnerUp", -0.1f },
                { "noseSneerLeft", 1f },
                { "noseSneerRight", 1f },
                { "jawForward", 0.7f },
                { "mouthPucker", 0.5f },
                { "cheekSquintLeft", 0.3f },
                { "cheekSquintRight", 0.3f },
                { "mouthSmile", -0.5f }
            }
        },
        {
            "fear", new Dictionary<string, float>
            {
                { "eyeWideLeft", 0.45f },
                { "eyeWideRight", 0.45f },
                { "mouthOpen", 0.25f },
                { "browOuterUpLeft", 0.5f },
                { "browOuterUpRight", 0.5f },
                { "browInnerUp", 0.6f },
                { "jawOpen", 0.1f },
                { "cheekSquintLeft", -0.2f },
                { "cheekSquintRight", -0.2f }
            }
        },
        {
            "surprise", new Dictionary<string, float>
            {
                { "mouthOpen", 0.25f },
                { "browOuterUpLeft", 0.45f },
                { "browOuterUpRight", 0.45f },
                { "browInnerUp", 0.45f },
                { "eyeWideLeft", 0.5f },
                { "eyeWideRight", 0.5f },
                { "cheekPuff", 0.3f },
                { "jawOpen", 0.1f }
            }
        }
    };



    void Start()
    {
        SetBlendShapeWeight("browInnerUp", 0.2f);
    }

    void Update()
    {
    }

    public void AdjustMorphTargets(JObject json)
    {
        try
        {
            JObject emotionData = json.GetValue("emotion_scores") as JObject;

            var primaryEmotion = emotionData.Properties()
                .OrderByDescending(e => (float)e.Value)
                .FirstOrDefault();

            if (primaryEmotion != null)
            {
                string emotionKey = primaryEmotion.Name;
                float score = (float)primaryEmotion.Value;
                ResetBlendShapes();

                ApplyEmotion(emotionKey, score);
            }
        }
        catch (Newtonsoft.Json.JsonReaderException jsonEx)
        {
            Debug.LogError($"JSON Parse Error: {jsonEx.Message}. Raw JSON: {json}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception in AdjustMorphTargets: {ex.Message}");
        }
    }

    private void ApplyEmotion(string emotion, float score)
    {
        if (emotionMappings.TryGetValue(emotion, out var morphMap))
        {
            foreach (var morph in morphMap)
            {
                float weight = morph.Value * (score / 100);
                SetBlendShapeWeight(morph.Key, weight);
            }
        }
    }

    private void SetBlendShapeWeight(string morph, float weight)
    {
        int index = GetBlendShapeIndex(morph);
        if (index != -1)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(index, Mathf.Clamp(weight, 0f, 1f));
        }
    }

    private int GetBlendShapeIndex(string morph)
    {
        switch (morph)
        {
            case "mouthOpen": return 0;
            case "viseme_sil": return 1;
            case "viseme_PP": return 2;
            case "viseme_FF": return 3;
            case "viseme_TH": return 4;
            case "viseme_DD": return 5;
            case "viseme_kk": return 6;
            case "viseme_CH": return 7;
            case "viseme_SS": return 8;
            case "viseme_nn": return 9;
            case "viseme_RR": return 10;
            case "viseme_aa": return 11;
            case "viseme_E": return 12;
            case "viseme_I": return 13;
            case "viseme_O": return 14;
            case "viseme_U": return 15;
            case "mouthSmile": return 16;
            case "browDownLeft": return 17;
            case "browDownRight": return 18;
            case "browInnerUp": return 19;
            case "browOuterUpLeft": return 20;
            case "browOuterUpRight": return 21;
            case "eyeSquintLeft": return 22;
            case "eyeSquintRight": return 23;
            case "eyeWideLeft": return 24;
            case "eyeWideRight": return 25;
            case "jawForward": return 26;
            case "jawLeft": return 27;         
            case "jawRight": return 28;        
            case "mouthPucker": return 29;     
            case "noseSneerLeft": return 30;   
            case "noseSneerRight": return 31;
            case "mouthLeft": return 32;
            case "mouthRight": return 33;
            case "eyeLookDownLeft": return 34;
            case "eyeLookDownRight": return 35;
            case "eyeLookUpLeft": return 36;
            case "eyeLookUpRight": return 37;
            case "eyeLookInLeft": return 38;
            case "eyeLookInRight": return 39;
            case "eyeLookOutLeft": return 40;
            case "eyeLookOutRight": return 41;
            case "cheekPuff": return 42;
            case "cheekSquintLeft": return 43;
            case "cheekSquintRight": return 44;
            case "jawOpen": return 45;
            case "mouthSmileLeft": return 46;
            case "mouthSmileRight": return 47;
            case "tongueOut": return 48;
            case "eyeBlinkLeft": return 49; 
            case "eyeBlinkRight": return 50;
            case "eyesClosed": return 51;
            case "eyesLookUp": return 52;
            case "eyesLookDown": return 53;
            default: return -1;
        }
    }

    private void ResetBlendShapes()
    {
        if (skinnedMeshRenderer == null)
        {
            return; 
        }

        int blendShapeCount = skinnedMeshRenderer.sharedMesh.blendShapeCount;
        for (int i = 0; i < blendShapeCount; i++)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(i, 0);
        }
        SetBlendShapeWeight("browInnerUp", 0.2f);
    }
}