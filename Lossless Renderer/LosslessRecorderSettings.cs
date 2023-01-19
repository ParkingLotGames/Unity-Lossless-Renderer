using UnityEngine;

public class LosslessRecorderSettings : ScriptableObject
{ 
    public string outputFileName = "frame_";
    public string outputVideoName = "sequence_.mkv";
    public int frameRate = 30;
    /// <summary>
    /// Size multiplier that the image sequence will be captured at.
    /// </summary>
    [Range(1,4)]public int captureSizeMultiplier = 1;
    /// <summary>
    /// The path where both image sequences and videos are recorded
    /// </summary>
    public string outputPath;   
    /// <summary>
    /// Wheter or not there is a properly configured installation of ffmpeg present in the system
    /// </summary>
    public bool globalFfmpegInstallationFound;
    /// <summary>
    /// Wheter to use a local installation inside the Project/Application folder
    /// </summary>
    public bool localFfmpegInstallationFound;
    public FfmpegInstallToUse ffmpegInstallToUse = FfmpegInstallToUse.global;
}
