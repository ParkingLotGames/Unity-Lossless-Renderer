
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class EditorRecorderWindow : EditorWindow
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
        var window = GetWindow<EditorRecorderWindow>();
        window.titleContent = new GUIContent("Lossless Recorder");
        window.Show();
    }

    void OnEnable()
    {
        LosslessRecorder.CreateSettingsAsset(settings);
    }

    void OnGUI()
    {
        if (settings)
            if (GUILayout.Button("Recheck Installs", GUILayout.Width(112)))
            {
                LosslessRecorder.RecheckInstalls(settings);
            }
        settings = (LosslessRecorderSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(LosslessRecorderSettings), false);
        if (!settings)
        {
            EditorGUILayout.HelpBox("No Settings asset found. Please select or create one.", MessageType.Warning);
            if (GUILayout.Button("Create Asset"))
            {
                LosslessRecorder.CreateSettingsAsset(settings);
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

    private void Update()
    {
        if (recordingState == RecordingState.Recording)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= frameTime)
            {
                EditorCoroutineUtility.StartCoroutine(LosslessRecorder.CaptureScreenshot(captureFilePath, currentFrame, elapsed), this);
            }
        }
    }
    #endregion

    #region Private UI Methods
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
                    LosslessRecorder.TogglePlayMode();
                    LosslessRecorder.StartRecording(currentOutputPath, settings, captureFilePath, currentFrame, recordingState);
                }
                if (GUILayout.Button("Start Recording"))
                {
                    LosslessRecorder.StartRecording(currentOutputPath, settings, captureFilePath, currentFrame, recordingState);
                }
                if (GUILayout.Button("Splice Image Sequence"))
                {
                    LosslessRecorder.SpliceRecording(ffmpegProcess, settings, currentOutputPath);
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
                        LosslessRecorder.StopRecording(ffmpegProcess, recordingState, currentFrame);
                    }
                    if (GUILayout.Button("Stop Playing"))
                    {
                        recordingState = RecordingState.Stopping;
                        LosslessRecorder.TogglePlayMode();
                        LosslessRecorder.StopRecording(ffmpegProcess, recordingState, currentFrame);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button("Stop Recording"))
                    {
                        recordingState = RecordingState.Stopping;
                        LosslessRecorder.StopRecording(ffmpegProcess, recordingState, currentFrame);
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
                FfmpegInstallationManager.InstallFfmpeg(selectedInstall,settings);
#elif UNITY_EDITOR_LINUX || UNITY_EDITOR_MACOS
            FfmpegInstallationManager.OpenTerminalWithCommand(FfmpegInstallationManager.CheckPackageManager());
            settings.globalFfmpegInstallationFound = FfmpegInstallationManager.IsGlobalFfmpegInstalled();
#endif
            ffmpegInstalledByTool = true;
        }
    }
#endif
    #endregion
}

public class FfmpegInstallationManagerWindow : EditorWindow
{
    #region Unity Callbacks
    [MenuItem("Tools/FFMPEG Install Check")]
    public static void ShowWindow()
    {
        var window = GetWindow<FfmpegInstallationManagerWindow>();
        window.titleContent = new GUIContent("FFMPEG Install Check");
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Check global install"))
        {
            Debug.Log(FfmpegInstallationManager.IsGlobalFfmpegInstalled());
        }
        if (GUILayout.Button("Check local install"))
        {
            Debug.Log(FfmpegInstallationManager.IsLocalFfmpegInstalled());
        }
        GUILayout.EndHorizontal();
    }
    #endregion
}
