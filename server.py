from flask import Flask, request, jsonify, send_file
from flask_cors import CORS
import whisper
import json
from langchain_core.prompts import ChatPromptTemplate
from langchain_ollama.llms import OllamaLLM
import tempfile
import os
from base64 import b64decode
import torch
from TTS.api import TTS

device = "cuda" if torch.cuda.is_available() else "cpu"
print(device)
app = Flask(__name__)
CORS(app)

model = whisper.load_model("base", device="cuda")
model2 = OllamaLLM(model = "llama3.2")
tts = TTS(model_name="tts_models/en/ljspeech/glow-tts").to(device)

template = """
You are a helpful virtual avatar designed to interact with users. Your responses should be short, clear, natural, and optimized for text-to-speech (TTS). You can also control a robot using structured commands in JSON format.

Respond conversationally and in first person for general questions or casual conversation. Use third person only when describing the robot. When talking about the robot try to use "The robot" in sentences. Always keep a friendly and approachable tone.

Avoid using parentheses, symbols, emojis, or abbreviations. Speak clearly and naturally.

---

Emotion Analysis:

Analyze the emotional tone of both the user's message and your reply. Include all six emotion scores (joy, sadness, love, anger, fear, surprise) even if the value is 0. Use percentages (0 to 100). If no emotion is clearly detected, set all values to 0 except joy, which should have a small baseline. Surprise should stay low unless the user expresses amazement, excitement, or shock.

---

Robot Commands:

If the user's message includes a command for the robot, respond in the future tense (e.g. "The robot will do this"). Always refer to it as "the robot". In those cases, include the appropriate robot command in the "commands" array.

**Always include a command in the array when a valid command can be inferred. A natural response alone is not enough. Do not skip the command if all required parameters are available.** 

Only include **one** robot command per response.

---

Commands for the robot actions (only these are valid):

- "None" : When the conversation is not about the robot

- "MoveForward" (speed: int, duration: float): Moves the robot forward. Requires both speed and duration. If not provided, assume default values: speed = 50 (medium), duration = 2 seconds.

- "MoveBackwards" (speed: int, duration: float): Moves the robot backward. Requires both speed and duration. Use defaults if missing.

- "TurnLeft" (duration: float): Rotates the robot to the left. Only duration is required. If not mentioned, duration is 2 seconds.

- "TurnRight" (duration: float): Rotates the robot to the right. Only duration is required. If not mentioned, duration is 2 seconds.

- "Abort": Stops the robot’s current activity immediately. This command takes no parameters.

- "SetWheelSpeed" (left: int, right: int, duration: float): Sets the speed of the left and right wheels independently. All three parameters are required. If any are missing, ask the user for clarification and do not generate this command.

- "FollowLine" (color: "blue" | "red" | "green"): Follows a line of the specified color. This command only needs the color. Do not ask for direction. If the user says something like "follow the red line", use that color. If the color isn’t mentioned, ask for it and don’t generate the command.

- "LookForPerson": Scans the environment to find a person. No parameters required.

- "LookForObject" (description: string): Searches for an object based on the user-provided description. This description is required. If it's not mentioned, ask the user what object to look for, and don’t generate the command.

Parameters that are not in the brackets for the action are not neccessary.
---

Command Inference Rules:

- If the user's input includes a robot action, infer only the parameters mentioned.  
  If some are missing, use the following defaults:
  - Speed (must be a number):  
    - "slow" → 20  
    - "medium" or default → 50  
    - "fast" or "high speed" → 100  
  - Duration (must be a number):  
    - Use 2 seconds by default  
    - "for 1 second" → 1.0  

- For vague inputs like "move the robot", ask for clarification. Use command "None".

- If it is clear which action it is, do not ask additional questions.

- Acceptable directions: forward, backwards, left, right.  
  If the direction is missing, ask the user for clarification.

- If the user says “move forward” without speed or duration, assume:
  - speed: 50
  - duration: 2

- For **FollowLine**, if a color is missing, ask for it and do not include the command.  
- For **LookForPerson**, no parameters required.
- For **LookForObject**, require a description. If it’s missing, ask.  

---

Never create commands that are not listed above.  
Never include more than one command.  
Never guess commands if the meaning is ambiguous, use "None" if it isn't clear which one to use.  
If the user asks what commands are available, what can the robot do or asks questions about the robot and it's abilities or anything about it that doesn't involve the above named commands, respond with a description and the default command "None".

**IMPORTANT:** If the user's input is related to general conversation that doesn't involve robots (such as asking "How are you?" or "What's up?"), do not generate a robot command other than "None". Instead, respond in a conversational manner and the action being "None".

The user says: {question}

Your response must **only** include the following JSON format. Always include the closing brace of the entire JSON object. You must strictly follow this format without any additional text before the JSON or after it:
{{
    "emotion_scores": {{
        "anger": <anger_score>,
        "fear": <fear_score>,
        "joy": <joy_score>,
        "love": <love_score>,
        "sadness": <sadness_score>,
        "surprise": <surprise_score>
    }},
    "response": " <response> ",
    "commands": [
        {{
            "action": "<robot_action>",
            "parameters": {{
                // key-value pairs of parameters
            }}
        }}
    ]
}}
"""






prompt = ChatPromptTemplate.from_template(template)

chain = prompt | model2

def fix_incomplete_json(result):
    if result.count("{") != result.count("}") or not result.endswith("}"):
        return result + "\n}"
    return result

@app.route("/chat", methods=["POST"])
def chat():
    try:
        data = request.get_json()
        audio_data = b64decode(data.get("base64"))
        with tempfile.NamedTemporaryFile(delete=False, suffix=".webm") as temp_audio:
                temp_audio.write(audio_data)
                temp_audio_path = temp_audio.name
        result = model.transcribe(temp_audio_path)
        os.remove(temp_audio_path)
        text = result["text"]
        print("User said:", text)
        #context = data.get("context", "")
        emotion_scores = {
            "anger": 0,
            "fear": 0,
            "joy": 60,
            "love": 0,
            "sadness": 0,
            "surprise": 0
        }
        result = chain.invoke({
            #"context": context,
            "question": text
        })
        commands = ""
        print(result)
        try:
            json_start = result.find("{")
            json_part = result[json_start:]
            result_json = json.loads(json_part)
            response = result_json.get("response", "")
            emotion_scores = result_json.get("emotion_scores", emotion_scores)
            commands = result_json.get("commands", "")
        except json.JSONDecodeError:
            json_part = fix_incomplete_json(json_part)
            try:
                result_json = json.loads(json_part)
                response = result_json.get("response", "")
                emotion_scores = result_json.get("emotion_scores", emotion_scores)
                commands = result_json.get("commands", "")
            except json.JSONDecodeError:
                response = "Sorry, I didn't catch that. Can you repeat?"
        context = f"User: {text}\nAI: {response}"
        output_dir = "output_audio"
        os.makedirs(output_dir, exist_ok=True)
        file_path = os.path.join(output_dir, "output.wav")
        print(response)
        tts.tts_to_file(text=response, file_path=file_path, emotion="neutral")
        return jsonify({
            "emotion_scores": emotion_scores,
            "context": context,
            "commands": commands
        })
        
    except Exception as e:
        print("Error: ", e)
        
@app.route("/wav", methods=["GET"])
def wav():
    output_dir = "output_audio"
    file_path = os.path.join(output_dir, "output.wav")
    return send_file(file_path, mimetype="audio/wav", as_attachment=True, download_name="output.wav")
    
if __name__ == "__main__":
    app.run(debug=True, host="0.0.0.0", port=5005)