# Unity Lossless Renderer

#### Only 2019.1+ versions are officially supported

### An editor and runtime renderer that outputs frame captures and uses ffmpeg to join them in a video. It can also download and install ffmpeg globally or locally.

Local path for ffmpeg is Project/localffmpeg/ffmpeg-master-latest-win64-gpl/bin/ffmpeg.exe

For prior versions, download the [2018_OR_LOWER](https://github.com/ParkingLotGames/Unity-Lossless-Renderer/tree/2018_OR_LOWER) branch but be aware that those versions are not able to download ffmpeg if it's not installed.

# //TODO:
#### Delete the downloaded ffmpeg zip after confirming the install was correctly finished.
#### Allow user to select a folder to splice without recording previously.
#### Consider adding an option for the user to select the name of the subfolder where screen captures are saved.
#### Check how to fix the issue where EditorUpdate is so fast that the screenshots have skipped numbers in between them, breaking ffmpeg's ability to splice them.
#### Port over missing features to MonoBehaviour version from the Editor Window.
#### In both Editor Window and MB, show controls for all options saved in the Settings scriptable object.
#### Port feasible features to 2018 branch.
