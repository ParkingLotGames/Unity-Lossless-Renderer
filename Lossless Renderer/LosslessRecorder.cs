using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.IO.Compression;
using System.Net;
using Debug = UnityEngine.Debug;

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
    #region Variables
    #region Private Variables
    LosslessRecorderSettings settings;
    RecordingState recordingState;

    int currentFrame = 0;
    float frameTime;
    float elapsed;
#if UNITY_2018_2_OR_NEWER
    bool ffmpegInstalledByTool;
#endif
    Process ffmpegProcess;
    #endregion

    #region Public Variables
    public string currentOutputPath;
    public string captureFilePath;
    #endregion
    #endregion

    #region Unity Callbacks
    [MenuItem("Tools/Lossless Recorder")]
    static void ShowWindow()
    {
        var window = GetWindow<EditorRecorder>();
        window.titleContent = new GUIContent("Lossless Recorder");
        window.Show();
    }

    void OnEnable()
    {
        if (File.Exists("Assets/LosslessRecorderSettings.asset"))
        {
            settings = AssetDatabase.LoadAssetAtPath<LosslessRecorderSettings>("Assets/LosslessRecorderSettings.asset");
            RecheckInstalls();
        }
    }

    void OnGUI()
    {
        if (settings)
            if (GUILayout.Button("Recheck Installs", GUILayout.Width(112)))
            {
                RecheckInstalls();
            }
        settings = (LosslessRecorderSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(LosslessRecorderSettings), false);
        if (!settings)
        {
            EditorGUILayout.HelpBox("No Settings asset found. Please select or create one.", MessageType.Warning);
            if (GUILayout.Button("Create Asset"))
            {
                CreateSettingsAsset();
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
                    ShowRegularUI(settings.ffmpegInstallToUse);
                //and we don't find a global install
                else
                    ShowInstallNotFoundArea(settings.ffmpegInstallToUse);
            }
            // if we're using local
            else
            {
                //and we find an install
                if (settings.localFfmpegInstallationFound)
                    ShowRegularUI(settings.ffmpegInstallToUse);
                //and we don't find a local install
                else
                    ShowInstallNotFoundArea(settings.ffmpegInstallToUse);
            }
        }
    }

    void Update()
    {
        if (recordingState == RecordingState.Recording)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= frameTime)
            {
#if UNITY_2017_4_OR_NEWER
                ScreenCapture.CaptureScreenshot(captureFilePath + currentFrame + ".png");
#else
                Application.CaptureScreenshot(captureFilePath + currentFrame + ".png");
#endif
                currentFrame += 1;
                elapsed = 0;
            }
        }
    }
    #endregion

    #region Private Methods
    void CreateSettingsAsset()
    {
        settings = CreateInstance<LosslessRecorderSettings>();
        AssetDatabase.CreateAsset(settings, "Assets/LosslessRecorderSettings.asset");
        AssetDatabase.SaveAssets();
    }

    void RecheckInstalls()
    {
        settings.globalFfmpegInstallationFound = FfmpegInstallationManager.IsGlobalFfmpegInstalled();
        settings.localFfmpegInstallationFound = FfmpegInstallationManager.IsLocalFfmpegInstalled();
    }

    void ShowSettingsArea()
    {
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
    }

    void ShowStartStopRecordingControls()
    {
        switch (recordingState)
        {
            case RecordingState.Stopped:
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play and Record"))
                {
                    TogglePlayMode();
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
                        TogglePlayMode();
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

    private static void TogglePlayMode()
    {
#if UNITY_2019_1_OR_NEWER
                                EditorApplication.EnterPlaymode();
#else
        EditorApplication.ExecuteMenuItem("Edit/Play");
#endif
    }

    void ShowRegularUI(FfmpegInstallToUse selectedInstall)
    {
        if (selectedInstall == FfmpegInstallToUse.global)
        {
            if (settings.localFfmpegInstallationFound)
                ShowInstallFoundInfoBox(selectedInstall);
        }
        else
        {
            if (settings.globalFfmpegInstallationFound)
                ShowInstallFoundInfoBox(selectedInstall);
        }
#if UNITY_2018_2_OR_NEWER
        if (ffmpegInstalledByTool)
            ShowInstallFirstTimeInfoBox(settings.ffmpegInstallToUse);
#endif
        ShowSettingsArea();
        ShowStartStopRecordingControls();
    }

    void ShowInstallFirstTimeInfoBox(FfmpegInstallToUse selectedInstall)
    {
        string installTypeString;
        if (selectedInstall == FfmpegInstallToUse.global)
            installTypeString = "global";
        else
            installTypeString = "local";
        EditorGUILayout.HelpBox(installTypeString + " ffmpeg installation created at 'Project/ffmpeg/'.", MessageType.Info);
    }

    void ShowInstallFoundInfoBox(FfmpegInstallToUse selectedInstall)
    {
        string oppositeKeyword;
        if (selectedInstall == FfmpegInstallToUse.global)
            oppositeKeyword = "local";
        else
            oppositeKeyword = "global";
        EditorGUILayout.HelpBox("A " + oppositeKeyword + " ffmpeg installation was found, consider using it instead to save some disk space.", MessageType.Info);
    }

    void ShowInstallMissingMessage(FfmpegInstallToUse selectedInstall)
    {
        string installTypeString;
        if (selectedInstall == FfmpegInstallToUse.global)
            installTypeString = "global";
        else
            installTypeString = "local";
        EditorGUILayout.HelpBox("No " + installTypeString + " ffmpeg installation found, please download and install ffmpeg.", MessageType.Error);
    }

    void ShowInstallNotFoundArea(FfmpegInstallToUse selectedInstall)
    {
        //show if we find an opposite installation
        if (selectedInstall == FfmpegInstallToUse.global)
        {
            if (settings.localFfmpegInstallationFound)
                ShowInstallFoundInfoBox(selectedInstall);
        }
        else
        {
            if (settings.globalFfmpegInstallationFound)
                ShowInstallFoundInfoBox(selectedInstall);
        }
        ShowInstallMissingMessage(selectedInstall);
#if UNITY_2018_2_OR_NEWER
        ShowDownloadFfmpegButton(selectedInstall);
#endif
    }

#if UNITY_2018_2_OR_NEWER
    void ShowDownloadFfmpegButton(FfmpegInstallToUse selectedInstall)
    {
        if (GUILayout.Button("Download and install ffmpeg"))
        {
#if UNITY_EDITOR_WIN
            FfmpegInstallationManager.DownloadFfmpeg();
            if (selectedInstall == FfmpegInstallToUse.global)
            {
                FfmpegInstallationManager.InstallFfmpeg("C:/", false);
                settings.globalFfmpegInstallationFound = FfmpegInstallationManager.IsGlobalFfmpegInstalled();
            }
            else
            {
                FfmpegInstallationManager.InstallFfmpeg("localffmpeg", false);
                settings.localFfmpegInstallationFound = FfmpegInstallationManager.IsLocalFfmpegInstalled();
            }
#elif UNITY_EDITOR_LINUX || UNITY_EDITOR_MACOS
            FfmpegInstallationManager.OpenTerminalWithCommand(FfmpegInstallationManager.CheckPackageManager());
            settings.globalFfmpegInstallationFound = FfmpegInstallationManager.IsGlobalFfmpegInstalled();
#endif
            ffmpegInstalledByTool = true;
        }
    }
#endif

    void StartRecording()
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
        ffmpegProcess.StartInfo.Arguments = "-y -r " + settings.frameRate + " -i " + captureFilePath + "%d.png" + " -vcodec libx264 -crf 0 " + Path.Combine(currentOutputPath, settings.outputVideoName);
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
    #endregion
}

public class FfmpegInstallationManager : EditorWindow
{
    #region Unity Callbacks
    [MenuItem("Tools/FFMPEG Install Check")]
    public static void ShowWindow()
    {
        var window = GetWindow<FfmpegInstallationManager>();
        window.titleContent = new GUIContent("FFMPEG Install Check");
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Check global install"))
        {
            Debug.Log(IsGlobalFfmpegInstalled());
        }
        if (GUILayout.Button("Check local install"))
        {
            Debug.Log(IsLocalFfmpegInstalled());
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region Static Methods
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

#if UNITY_EDITOR_LINUX || UNITY_EDITOR_MACOS
    public static int CheckPackageManager()
    {
        if (File.Exists("/usr/bin/pacman")) return 0;
        else if (File.Exists("/usr/bin/dnf")) return 1;
        else if (File.Exists("/usr/bin/apt")) return 2;
        else if (File.Exists("/usr/bin/portage")) return 3;
        else if (File.Exists("/usr/local/bin/brew")) return 4;
        else return -1;
    }
    public static void OpenTerminalWithCommand(int packageManager)
    {
        string command = "";
        if (packageManager == 0) command = "pacman -S ffmpeg";
        else if (packageManager == 1) command = "dnf install ffmpeg";
        else if (packageManager == 2) command = "apt install ffmpeg";
        else if (packageManager == 3) command = "emerge ffmpeg";
        else if (packageManager == 4) command = "brew install ffmpeg";
        else
        {
            Debug.Log("No supported package manager found.");
            return;
        }
        var terminalProcess = new Process
        {
#if UNITY_EDITOR_LINUX
            StartInfo = new ProcessStartInfo
            {
                FileName = "x-terminal-emulator",
                Arguments = "-a Terminal",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
#elif UNITY_EDITOR_MACOS
            StartInfo = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = "-a Terminal",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
#endif
        };
        terminalProcess.Start();
        terminalProcess.StandardInput.WriteLine(command);
        terminalProcess.StandardInput.Flush();
        terminalProcess.StandardInput.Close();
    } 
#endif


#if UNITY_2018_2_OR_NEWER
    public static void DownloadFfmpeg()
    {
        using (var client = new WebClient())
        {
            client.DownloadFile("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip", "ffmpeg-latest.zip");
        }
        if (!Directory.Exists("localffmpeg"))
        {
            Directory.CreateDirectory("localffmpeg");
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
#endif
    #endregion
}
