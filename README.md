# Unity Lossless Renderer

![Unity Supported Version](https://img.shields.io/badge/Unity-2018.2%2B-blue?style=plastic) ![Unity Minimum Version](https://img.shields.io/badge/Unity-Min5.6%2B-yellowgreen?style=plastic) ![License](https://img.shields.io/github/license/ParkingLotGames/Unity-Lossless-Renderer?style=plastic) ![Size](https://img.shields.io/github/repo-size/ParkingLotGames/Unity-Lossless-Renderer?style=plastic) ![package.json version (branch)](https://img.shields.io/github/package-json/v/ParkingLotGames/Unity-Lossless-Renderer/main?style=plastic) ![Last commit](https://img.shields.io/github/last-commit/ParkingLotGames/Unity-Lossless-Renderer?style=plastic)

![Unity Version Limitation](https://img.shields.io/badge/Unity%202018.2‑3‑4-Check%20limitations-red?style=plastic) ![package.json dynamic](https://img.shields.io/github/package-json/keywords/ParkingLotGames/Unity-Lossless-Renderer?style=plastic)

![Issues](https://img.shields.io/github/issues-raw/ParkingLotGames/Unity-Lossless-Renderer?style=plastic) ![Pull requests](https://img.shields.io/github/issues-pr-raw/ParkingLotGames/Unity-Lossless-Renderer?style=plastic)

#### Editor and runtime renderer that outputs frame captures and uses ffmpeg to join them in a video
##### Can download and install ffmpeg globally or locally.

> Local path for ffmpeg is Project/localffmpeg/ffmpeg-master-latest-win64-gpl/bin/ffmpeg.exe

## Limitations
Unity 2018.2-2018.4 versions need to add [this](https://gist.github.com/ParkingLotGames/0f8b4bdfa298266cba093c69241e9b43) as a [msc(.2) or csc.rsp(.3+) file](https://forum.unity.com/threads/c-compression-zip-missing.577492/#post-3849472) to your Assets folder in order to be able to unzip the downloaded ffmpeg file.
Download support is not available on 2018.1 or lower versions and no testing was made on versions prior to 5.6.0f3

## //TODO:
- Test Linux support.

- Delete the downloaded ffmpeg zip after confirming the install was correctly finished.

- Allow user to select a folder to splice without recording previously.
 
- Check how to fix the issue where EditorUpdate is so fast that the screenshots have skipped numbers in between them, breaking ffmpeg's ability to splice them.

- Port over missing features to MonoBehaviour version from the Editor Window.

- In both Editor Window and MB, show controls for all options saved in the Settings scriptable object.

- Add option to define how many frames to capture in advance
 
- Create documentation, summaries, comments and examples.
- - Consider adding an option for the user to select the name of the subfolder where screen captures are saved.
