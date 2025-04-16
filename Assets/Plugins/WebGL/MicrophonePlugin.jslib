mergeInto(LibraryManager.library, {
  mediaStream: null,
  mediaRecorder: null,
  audioChunks: [],
  isRecording: false,

  InitMicrophone: function () {
    if (this.isRecording) {
      console.log("Already recording.");
      return;
    }
    navigator.mediaDevices
      .getUserMedia({ audio: true })
      .then((stream) => {
        this.mediaStream = stream;
        this.mediaRecorder = new MediaRecorder(stream, {
          mimeType: "audio/webm",
        });
        let globalContext = this;
        globalContext.audioChunks = [];
        this.mediaRecorder.ondataavailable = function (event) {
          if (event.data.size > 0) {
            globalContext.audioChunks.push(event.data);
          }
        };
        this.mediaRecorder.onstop = function () {
          if (globalContext.audioChunks.length === 0) {
            console.warn("No audio data available.");
            return;
          }

          let audioBlob = new Blob(globalContext.audioChunks, {
            type: "audio/webm",
          });
          let reader = new FileReader();
          reader.onloadend = function () {
            let base64String = reader.result.split(",")[1];
            SendMessage("AvatarSwitcher", "OnAudioRecorded", base64String);
          };
          reader.readAsDataURL(audioBlob);
          globalContext.audioChunks = [];
        };
        this.mediaRecorder.start();
        this.isRecording = true;
      })
      .catch((err) => {
        console.error("Microphone access denied:", err);
      });
  },

  StopMicrophone: function () {
    if (!this.isRecording) {
      console.warn("No active recording to stop.");
      return;
    }

    this.mediaRecorder.stop();
    this.mediaStream.getTracks().forEach((track) => track.stop());
    this.mediaRecorder = null;
    this.mediaStream = null;
    this.isRecording = false;
  },
});
