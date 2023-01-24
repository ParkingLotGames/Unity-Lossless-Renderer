using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.IO.Compression;
using System.Net;
using System.Collections;

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

public class LosslessRecorder
{
    public static void StartRecording(string currentOutputPath, LosslessRecorderSettings settings, string captureFilePath, int currentFrame, RecordingState recordingState)
    {
        currentOutputPath = Path.Combine(settings.outputPath, DateTime.Now.ToString("MM-dd-yyyy@HH-mm-ss"));
        if (!Directory.Exists(currentOutputPath))
            Directory.CreateDirectory(currentOutputPath);
        captureFilePath = Path.Combine(currentOutputPath, settings.outputFileName);
        currentFrame = 0;
        recordingState = RecordingState.Recording;
    }

    public static void StopRecording(Process ffmpegProcess, RecordingState recordingState, int currentFrame)
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

    public static void SpliceRecording(Process ffmpegProcess, LosslessRecorderSettings settings, string currentOutputPath)
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

    public static void CreateSettingsAsset(LosslessRecorderSettings settings)
    {
        settings = ScriptableObject.CreateInstance<LosslessRecorderSettings>();
        AssetDatabase.CreateAsset(settings, "Assets/LosslessRecorderSettings.asset");
        AssetDatabase.SaveAssets();
    }

    public static void RecheckInstalls(LosslessRecorderSettings settings)
    {
        settings.globalFfmpegInstallationFound = FfmpegInstallationManager.IsGlobalFfmpegInstalled();
        settings.localFfmpegInstallationFound = FfmpegInstallationManager.IsLocalFfmpegInstalled();
    }

    public static void TogglePlayMode()
    {
#if UNITY_2019_1_OR_NEWER
        EditorApplication.EnterPlaymode();
#else
        EditorApplication.ExecuteMenuItem("Edit/Play");
#endif
    }

    public static void InitializeRecorder(float frameTime, LosslessRecorderSettings settings, string currentOutputPath, string captureFilePath, RecordingState recordingState, bool startRecording)
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
        if (startRecording)
            recordingState = RecordingState.Recording;
        else
            recordingState = RecordingState.Stopped;
    }

    public static IEnumerator CaptureScreenshot(string captureFilePath, int currentFrame, float elapsed)
    {
        yield return new WaitForEndOfFrame();

#if UNITY_2017_4_OR_NEWER
        ScreenCapture.CaptureScreenshot(captureFilePath + currentFrame + ".png");
#else
        Application.CaptureScreenshot(captureFilePath + currentFrame + ".png");
#endif
        currentFrame += 1;
        elapsed = 0;
    }
}

public class FfmpegInstallationManager
{
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

    public static void InstallFfmpeg(FfmpegInstallToUse ffmpegInstallToUse , LosslessRecorderSettings settings)
    {

        string installationPath = ffmpegInstallToUse == FfmpegInstallToUse.global ? "C:/" : "localffmpeg";
        ZipFile.ExtractToDirectory("ffmpeg-latest.zip", installationPath);
        if (ffmpegInstallToUse == FfmpegInstallToUse.global)
            AddFfmpegToPath();
        settings.localFfmpegInstallationFound = IsLocalFfmpegInstalled();
        settings.globalFfmpegInstallationFound = IsGlobalFfmpegInstalled();
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
