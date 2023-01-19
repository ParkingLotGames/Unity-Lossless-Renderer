using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
    private void Start()
    {
        frameTime = (float)1 / settings.frameRate;

#if UNITY_EDITOR
        currentOutputPath = Path.Combine(settings.outputPath, System.DateTime.Now.ToString("MM-dd-yyyy@HH-mm-ss"));
#else
        currentOutputPath = Path.Combine(Application.persistentDataPath, System.DateTime.Now.ToString("MM-dd-yyyy@HH-mm-ss"));
#endif
        if (!Directory.Exists(currentOutputPath))
            Directory.CreateDirectory(currentOutputPath);
        captureFilePath = Path.Combine(currentOutputPath, settings.outputFileName);
        recordingState = RecordingState.Recording;
    }

    //[Button]
    void SpliceRecording()
    {
        // Start the ffmpeg process
        ffmpegProcess = new Process();
#if UNITY_EDITOR
        if (settings.ffmpegInstallToUse == FfmpegInstallToUse.global)
            ffmpegProcess.StartInfo.FileName = "ffmpeg";
        else
            ffmpegProcess.StartInfo.FileName = @"localffmpeg\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg.exe";
#elif UNITY_STANDALONE_WIN
            ffmpegProcess.StartInfo.FileName = Path.Combine(Application.dataPath,@"ffmpeg\\bin\\ffmpeg.exe");
#endif
        ffmpegProcess.StartInfo.Arguments = "-y -r " + settings.frameRate + " -i " + Path.Combine(currentOutputPath, settings.outputFileName + "%d.png") + " -vcodec libx264 -crf 0 " + Path.Combine(currentOutputPath, settings.outputVideoName);
        ffmpegProcess.Start();
        ffmpegProcess.WaitForExit();
    }
    //[Button]
    void StopRecording()
    {
        recordingState = RecordingState.Stopped;
        SpliceRecording();
        currentFrame = 0;
    }

    void CreateAsset()
    {
        settings = ScriptableObject.CreateInstance<LosslessRecorderSettings>();
        AssetDatabase.CreateAsset(settings, "Assets/LosslessRecorderSettings.asset");
        AssetDatabase.SaveAssets();
    }

    private void Update()
    {
        if (recordingState == RecordingState.Recording)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= frameTime)
            {
                ScreenCapture.CaptureScreenshot(captureFilePath + currentFrame + ".png");
                currentFrame += 1;
                elapsed = 0;
            }
        }
    }
}
