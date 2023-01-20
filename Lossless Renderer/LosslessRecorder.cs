using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.IO.Compression;
using System.Net;

public enum FfmpegInstallToUse
{
    global = 1, local = 2
}

public enum RecordingState
{
    Stopped,
    Recording,
    Stopping
}

public class EditorRecorder : EditorWindow
{
    LosslessRecorderSettings settings;
    RecordingState recordingState;

    int currentFrame = 0;
    float frameTime;
    float elapsed;
    bool ffmpegInstalledByTool;
    public string currentOutputPath;
    public string captureFilePath;
    Process ffmpegProcess;

    [MenuItem("Tools/Lossless Recorder")]
    private static void ShowWindow()
    {
        var window = GetWindow<EditorRecorder>();
        window.titleContent = new GUIContent("Lossless Recorder");
        window.Show();
    }
    private void OnEnable()
    {
        if (File.Exists("Assets/LosslessRecorderSettings.asset"))
        {
            settings = AssetDatabase.LoadAssetAtPath<LosslessRecorderSettings>("Assets/LosslessRecorderSettings.asset");
            RecheckInstalls();
        }
    }

    private void RecheckInstalls()
    {
        settings.globalFfmpegInstallationFound = LocalFfmpegInstallationManager.IsGlobalFfmpegInstalled();
        settings.localFfmpegInstallationFound = LocalFfmpegInstallationManager.IsLocalFfmpegInstalled();
    }

    private void OnGUI()
    {
        if (settings)
            if (GUILayout.Button("Recheck Installs",GUILayout.Width(112)))
            {
                RecheckInstalls();
            }
        settings = (LosslessRecorderSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(LosslessRecorderSettings), false);
        if (!settings)
        {


            EditorGUILayout.HelpBox("No Settings asset found. Please select or create one.", MessageType.Warning);
            if (GUILayout.Button("Create Asset"))
            {
                CreateAsset();
            }
            return;
        }
        else
        {
            settings.ffmpegInstallToUse = (FfmpegInstallToUse)EditorGUILayout.EnumPopup("Select ffmpeg installation", settings.ffmpegInstallToUse);
            // if we're using global
            if (settings.ffmpegInstallToUse == FfmpegInstallToUse.global)
            {
                //and we find an install
                if (settings.globalFfmpegInstallationFound)
                {
                    //and no local
                    if (!settings.localFfmpegInstallationFound)
                    {
                    if (ffmpegInstalledByTool)
                        EditorGUILayout.HelpBox(@"Local ffmpeg installation created at ""Project/ffmpeg/"".", MessageType.Info);
                        settings.outputFileName = EditorGUILayout.TextField("Image Sequence File Name", settings.outputFileName);
                        settings.outputVideoName = EditorGUILayout.TextField("Output Video Name", settings.outputVideoName);
                        EditorGUILayout.BeginHorizontal();
                        settings.outputPath = EditorGUILayout.TextField("Output Path", settings.outputPath);
                        if (GUILayout.Button("...", GUILayout.Width(30)))
                        {
                            settings.outputPath = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
                        }
                        EditorGUILayout.EndHorizontal();
                        settings.frameRate = EditorGUILayout.IntField("Frame Rate", settings.frameRate);
                        frameTime = (float)1 / settings.frameRate;

                        // Show the start/stop recording controls
                        switch (recordingState)
                        {
                            case RecordingState.Stopped:
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Play and Record"))
                                {
                                EditorApplication.EnterPlaymode();
                                    StartRecording();
                                }
                                if (GUILayout.Button("Start Recording"))
                                {
                                    StartRecording();
                                }
                                if (GUILayout.Button("Splice Image Sequence"))
                                {
                                    SpliceRecording("ffmpeg");
                                }
                                EditorGUILayout.EndHorizontal();

                                break;
                            case RecordingState.Recording:
                                if (Application.isPlaying)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                    if (GUILayout.Button("Stop Playing"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                EditorApplication.EnterPlaymode();
                                        StopRecording();
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                {
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                }

                                break;
                            case RecordingState.Stopping:
                                EditorGUI.BeginDisabledGroup(recordingState == RecordingState.Stopping);
                                if (GUILayout.Button("Stopping...")) { }
                                EditorGUI.EndDisabledGroup();
                                break;
                        }
                    }
                    // but we also find a local one
                    else
                    {
                        EditorGUILayout.HelpBox(@"A local ffmpeg installation was found at ""Project/ffmpeg"", consider removing it to save some disk space.", MessageType.Info);
                    if (ffmpegInstalledByTool)
                        EditorGUILayout.HelpBox(@"Local ffmpeg installation created at ""Project/ffmpeg/"".", MessageType.Info);
                        settings.outputFileName = EditorGUILayout.TextField("Image Sequence File Name", settings.outputFileName);
                        settings.outputVideoName = EditorGUILayout.TextField("Output Video Name", settings.outputVideoName);
                        EditorGUILayout.BeginHorizontal();
                        settings.outputPath = EditorGUILayout.TextField("Output Path", settings.outputPath);
                        if (GUILayout.Button("...", GUILayout.Width(30)))
                        {
                            settings.outputPath = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
                        }
                        EditorGUILayout.EndHorizontal();
                        settings.frameRate = EditorGUILayout.IntField("Frame Rate", settings.frameRate);
                        frameTime = (float)1 / settings.frameRate;

                        // Show the start/stop recording controls
                        switch (recordingState)
                        {
                            case RecordingState.Stopped:
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Play and Record"))
                                {
                                EditorApplication.EnterPlaymode();
                                    StartRecording();
                                }
                                if (GUILayout.Button("Start Recording"))
                                {
                                    StartRecording();
                                }
                                if (GUILayout.Button("Splice Image Sequence"))
                                {
                                    SpliceRecording("ffmpeg");
                                }
                                EditorGUILayout.EndHorizontal();

                                break;
                            case RecordingState.Recording:
                                if (Application.isPlaying)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                    if (GUILayout.Button("Stop Playing"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                EditorApplication.EnterPlaymode();
                                        StopRecording();
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                {
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                }

                                break;
                            case RecordingState.Stopping:
                                EditorGUI.BeginDisabledGroup(recordingState == RecordingState.Stopping);
                                if (GUILayout.Button("Stopping...")) { }
                                EditorGUI.EndDisabledGroup();
                                break;
                        }
                    }
                }
                //and we don't find a global install
                else
                {
                    //nor a local one
                    if (!settings.localFfmpegInstallationFound)
                    {
                        EditorGUILayout.HelpBox("No ffmpeg entry found in PATH, please install ffmpeg and add it to PATH or check your Environment Variables and verify the specified path specified for ffmpeg in PATH is correct if you have already installed it.", MessageType.Error);
                    if (GUILayout.Button("Download and install ffmpeg"))
                    {
                        LocalFfmpegInstallationManager.DownloadFfmpeg();
                        LocalFfmpegInstallationManager.InstallFfmpeg("C:/",true);
                        ffmpegInstalledByTool = true;
                        settings.globalFfmpegInstallationFound = LocalFfmpegInstallationManager.IsGlobalFfmpegInstalled();
                    }
                    }
                    //but we find a local one
                    else
                    {
                        EditorGUILayout.HelpBox("A local installation of ffmpeg was found, consider using only an install to save some disk space", MessageType.Info);
                        EditorGUILayout.HelpBox("No ffmpeg entry found in PATH, please install ffmpeg and add it to PATH or check your Environment Variables and verify the specified path specified for ffmpeg in PATH is correct if you have already installed it.", MessageType.Error);
                    if (GUILayout.Button("Download and install ffmpeg"))
                    {

                        LocalFfmpegInstallationManager.DownloadFfmpeg();
                        LocalFfmpegInstallationManager.InstallFfmpeg("C:/",true);
                        ffmpegInstalledByTool = true;
                        settings.globalFfmpegInstallationFound = LocalFfmpegInstallationManager.IsGlobalFfmpegInstalled();
                    }
                    }
                }
            }
            // if we're using local
            else
            {
                //and we find an install
                if (settings.localFfmpegInstallationFound)
                {
                    //and no global
                    if (!settings.globalFfmpegInstallationFound)
                    {

                    if (ffmpegInstalledByTool)
                        EditorGUILayout.HelpBox(@"Local ffmpeg installation created at ""Project/ffmpeg/"".", MessageType.Info);
                        settings.outputFileName = EditorGUILayout.TextField("Image Sequence File Name", settings.outputFileName);
                        settings.outputVideoName = EditorGUILayout.TextField("Output Video Name", settings.outputVideoName);
                        EditorGUILayout.BeginHorizontal();
                        settings.outputPath = EditorGUILayout.TextField("Output Path", settings.outputPath);
                        if (GUILayout.Button("...", GUILayout.Width(30)))
                        {
                            settings.outputPath = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
                        }
                        EditorGUILayout.EndHorizontal();
                        settings.frameRate = EditorGUILayout.IntField("Frame Rate", settings.frameRate);
                        frameTime = (float)1 / settings.frameRate;

                        // Show the start/stop recording controls
                        switch (recordingState)
                        {
                            case RecordingState.Stopped:
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Play and Record"))
                                {
                                EditorApplication.EnterPlaymode();
                                    StartRecording();
                                }
                                if (GUILayout.Button("Start Recording"))
                                {
                                    StartRecording();
                                }
                                if (GUILayout.Button("Splice Image Sequence"))
                                {
                                    SpliceRecording(@"localffmpeg\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg.exe");
                                }
                                EditorGUILayout.EndHorizontal();

                                break;
                            case RecordingState.Recording:
                                if (Application.isPlaying)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                    if (GUILayout.Button("Stop Playing"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                EditorApplication.EnterPlaymode();
                                        StopRecording();
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                {
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                }

                                break;
                            case RecordingState.Stopping:
                                EditorGUI.BeginDisabledGroup(recordingState == RecordingState.Stopping);
                                if (GUILayout.Button("Stopping...")) { }
                                EditorGUI.EndDisabledGroup();
                                break;
                        }
                    }
                    // but we also find a global one
                    else
                    {

                        EditorGUILayout.HelpBox("A global ffmpeg installation was found, consider using it instead to save some disk space.", MessageType.Info);
                        if (ffmpegInstalledByTool)
                            EditorGUILayout.HelpBox(@"Local ffmpeg installation created at ""Project/ffmpeg/"".", MessageType.Info);
                        settings.outputFileName = EditorGUILayout.TextField("Image Sequence File Name", settings.outputFileName);
                        settings.outputVideoName = EditorGUILayout.TextField("Output Video Name", settings.outputVideoName);
                        EditorGUILayout.BeginHorizontal();
                        settings.outputPath = EditorGUILayout.TextField("Output Path", settings.outputPath);
                        if (GUILayout.Button("...", GUILayout.Width(30)))
                        {
                            settings.outputPath = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
                        }
                        EditorGUILayout.EndHorizontal();
                        settings.frameRate = EditorGUILayout.IntField("Frame Rate", settings.frameRate);
                        frameTime = (float)1 / settings.frameRate;

                        // Show the start/stop recording controls
                        switch (recordingState)
                        {
                            case RecordingState.Stopped:
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("Play and Record"))
                                {
                                EditorApplication.EnterPlaymode();
                                    StartRecording();
                                }
                                if (GUILayout.Button("Start Recording"))
                                {
                                    StartRecording();
                                }
                                if (GUILayout.Button("Splice Image Sequence"))
                                {
                                    SpliceRecording(@"localffmpeg\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg.exe");
                                }
                                EditorGUILayout.EndHorizontal();

                                break;
                            case RecordingState.Recording:
                                if (Application.isPlaying)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                    if (GUILayout.Button("Stop Playing"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                EditorApplication.EnterPlaymode();
                                        StopRecording();
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                {
                                    if (GUILayout.Button("Stop Recording"))
                                    {
                                        recordingState = RecordingState.Stopping;
                                        StopRecording();
                                    }
                                }

                                break;
                            case RecordingState.Stopping:
                                EditorGUI.BeginDisabledGroup(recordingState == RecordingState.Stopping);
                                if (GUILayout.Button("Stopping...")) { }
                                EditorGUI.EndDisabledGroup();
                                break;
                        }
                    }
                }
                //and we don't find a local install
                else
                {
                    //nor a global one
                    if (!settings.globalFfmpegInstallationFound)
                    {
                        EditorGUILayout.HelpBox("No local ffmpeg installation found, please download and install ffmpeg.", MessageType.Error);
                        if (GUILayout.Button("Download and install ffmpeg"))
                        {
                            LocalFfmpegInstallationManager.DownloadFfmpeg();
                            LocalFfmpegInstallationManager.InstallFfmpeg("ffmpeg",false);
                            ffmpegInstalledByTool = true;
                            settings.localFfmpegInstallationFound = LocalFfmpegInstallationManager.IsLocalFfmpegInstalled();
                        }
                    }
                    //but we find a local one
                    else
                    {
                        EditorGUILayout.HelpBox("A global ffmpeg installation was found, consider using it instead to save some disk space.", MessageType.Info);
                        EditorGUILayout.HelpBox("No local ffmpeg installation found, please download and install ffmpeg.", MessageType.Error);
                        if (GUILayout.Button("Download and install ffmpeg"))
                        {
                            LocalFfmpegInstallationManager.DownloadFfmpeg();
                            LocalFfmpegInstallationManager.InstallFfmpeg("ffmpeg",false);
                            ffmpegInstalledByTool = true;
                            settings.localFfmpegInstallationFound = LocalFfmpegInstallationManager.IsLocalFfmpegInstalled();
                        }
                    }
                }
            }
        }
    }

    private void StartRecording()
    {
        currentOutputPath = Path.Combine(settings.outputPath, DateTime.Now.ToString("MM-dd-yyyy@HH-mm-ss"));
        if (!Directory.Exists(currentOutputPath))
            Directory.CreateDirectory(currentOutputPath);
        captureFilePath = Path.Combine(currentOutputPath, settings.outputFileName);
        currentFrame = 0;
        recordingState = RecordingState.Recording;
    }

    void SpliceRecording(string path)
    {
        // Start the ffmpeg process
        ffmpegProcess = new Process();
        ffmpegProcess.StartInfo.FileName = path;
        ffmpegProcess.StartInfo.Arguments = "-y -r " + settings.frameRate + " -i " + Path.Combine(captureFilePath + "%d.png") + " -vcodec libx264 -crf 0 " + Path.Combine(currentOutputPath, settings.outputVideoName);
        ffmpegProcess.Start();
        ffmpegProcess.WaitForExit();
    }

    void StopRecording()
    {
        // Close the ffmpeg process and set the recording state to stopped
        if (ffmpegProcess != null)
        {
            ffmpegProcess.Close();
            ffmpegProcess = null;
        }
        recordingState = RecordingState.Stopped;
        currentFrame = 0;
    }

    void CreateAsset()
    {
        settings = CreateInstance<LosslessRecorderSettings>();
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


public class LocalFfmpegInstallationManager : EditorWindow
{
    [MenuItem("Tools/FFMPEG Install Check")]
    public static void ShowWindow()
    {
        var window = GetWindow<LocalFfmpegInstallationManager>();
        window.titleContent = new GUIContent("FFMPEG Install Check");
    }
#region Static Bools
    public static bool IsGlobalFfmpegInstalled()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
    }
    public static bool IsLocalFfmpegInstalled()
    {
        return File.Exists("localffmpeg/ffmpeg-master-latest-win64-gpl/bin/ffmpeg.exe");
    }
#endregion

    private void OnGUI()
    {
        if (GUILayout.Button("Check if global FFMPEG is installed"))
        {
            Debug.Log(IsGlobalFfmpegInstalled());
        }
        if (GUILayout.Button("Check if local FFMPEG is installed"))
        {
            Debug.Log(IsLocalFfmpegInstalled());
        }
    }
    public static void DownloadFfmpeg()
    {
        using (var client = new WebClient())
        {
            client.DownloadFile("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip", "ffmpeg-latest.zip");
        }
        if (!Directory.Exists("ffmpeg"))
        {
            Directory.CreateDirectory("ffmpeg");
        }
    }

    public static void InstallFfmpeg(string path, bool addToPath)
    {
        ZipFile.ExtractToDirectory("ffmpeg-latest.zip", path);
        if (addToPath)
            AddFfmpegToPath();
    }
    public static void AddFfmpegToPath()
    {
        var environmentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        var ffmpegPath = "C:/ffmpeg-master-latest-win64-gpl/bin/";
        if (!environmentPath.Contains(ffmpegPath))
        {
            environmentPath = ffmpegPath + ";" + environmentPath;
            Environment.SetEnvironmentVariable("PATH", environmentPath, EnvironmentVariableTarget.User);
        }
    }
}
