using System.Diagnostics;
using UnityEngine;

[ExecuteInEditMode]
public class RuntimeScreenRecorder : MonoBehaviour
{
    public LosslessRecorderSettings settings;
    RecordingState recordingState;
    Process ffmpegProcess;
    int currentFrame = 0;
    float elapsed;
    float frameTime;
    string currentOutputPath;
    string captureFilePath;

    //[Button]
    void StartRecording()
    {
        // InitializeRecorder overload that starts recording
        LosslessRecorder.InitializeRecorder(frameTime, settings, currentOutputPath,captureFilePath,recordingState, true);
    }

    void StopRecording()
    {
        recordingState = RecordingState.Stopped;
        LosslessRecorder.SpliceRecording(ffmpegProcess,settings, currentOutputPath);
        currentFrame = 0;
    }

    void SpliceRecording()
    {
        LosslessRecorder.SpliceRecording(ffmpegProcess,settings, currentOutputPath);
    }

    private void Update()
    {
        if (recordingState == RecordingState.Recording)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= frameTime)
            {
                StartCoroutine(LosslessRecorder.CaptureScreenshot(captureFilePath, currentFrame, elapsed));
            }
        }
    }
}
