# Unity Lossless Renderer

![Unity Supported Version](https://img.shields.io/badge/Unity-2018.2%2B-blue?style=plastic) [Unity Minimum Version](https://img.shields.io/badge/MinUnity-5.6%2B-yellowgreen?style=plastic) ![License](https://img.shields.io/github/license/ParkingLotGames/Unity-Lossless-Renderer?style=plastic) ![Size](https://img.shields.io/github/repo-size/ParkingLotGames/Unity-Lossless-Renderer?style=plastic) ![package.json version (branch)](https://img.shields.io/github/package-json/v/ParkingLotGames/Unity-Lossless-Renderer/main?style=plastic) ![Last commit](https://img.shields.io/github/last-commit/ParkingLotGames/Unity-Lossless-Renderer?style=plastic)

![package.json dynamic](https://img.shields.io/github/package-json/keywords/ParkingLotGames/Unity-Lossless-Renderer?style=plastic)

![Issues](https://img.shields.io/github/issues-raw/ParkingLotGames/Unity-Lossless-Renderer?style=plastic) ![Pull requests](https://img.shields.io/github/issues-pr-raw/ParkingLotGames/Unity-Lossless-Renderer?style=plastic)

### An editor and runtime renderer that outputs frame captures and uses ffmpeg to join them in a video. It can also download and install ffmpeg globally or locally.

Local path for ffmpeg is Project/localffmpeg/ffmpeg-master-latest-win64-gpl/bin/ffmpeg.exe

For prior versions, download the [2018_OR_LOWER](https://github.com/ParkingLotGames/Unity-Lossless-Renderer/tree/2018_OR_LOWER) branch but be aware that those versions are not able to download ffmpeg if it's not installed.

# //TODO:
#### Add Linux support.
#### Delete the downloaded ffmpeg zip after confirming the install was correctly finished.
#### Allow user to select a folder to splice without recording previously.
#### Consider adding an option for the user to select the name of the subfolder where screen captures are saved.
#### Check how to fix the issue where EditorUpdate is so fast that the screenshots have skipped numbers in between them, breaking ffmpeg's ability to splice them.
#### Port over missing features to MonoBehaviour version from the Editor Window.
#### In both Editor Window and MB, show controls for all options saved in the Settings scriptable object.
#### Port feasible features to 2018 branch.
#### When everything has been implemented, refactor code to avoid duplication, both when the program checks for ffmpeg installs and between EW and MB versions.
#### Create documentation, summaries, comments and examples.
