# Use an ONNX file exported from the Custom Vision Service with Windows ML

This example demonstrates how to take an ONNX file that was exported from the Cognitive Services Custom Vision service, and use it in a UWP application. Specifically, this example demonstrates how to use the camera on a Windows 10 device as the image source for the model.

__IMPORTANT__: This example requires Windows 10 build 17738 or higher, along with the matching Windows 10 SDK.


## Features

* Displays a preview of what the camera sees.
* Displays the tags and score returned from the model.

## Getting Started

### Prerequisites

- Windows 10 device with a camera and Windows 10 build 17738 or higher
- [Windows 10 SDK](https://developer.microsoft.com/windows/downloads/windows-10-sdk) (Build 17738 or higher)
- [Visual Studio](https://developer.microsoft.com/windows/downloads)

### Quickstart
(Add steps to get up and running quickly)

1. `git clone https://github.com/Azure-Samples/Custom-Vision-ONNX-UWP`
2. Open the `VisionApp.sln` file using Visual Studio.
3. Use F5 to build and run the application.
4. Point the camera at a dog or cat (or a picture of one). The score below the preview shows whether the model thinks that a dog or cat is in the scene. A label of "Negative" indicates that no dog or cat is in the image. 

## See also

[https://github.com/Azure-Samples/cognitive-services-onnx12-customvision-sample/](
https://github.com/Azure-Samples/cognitive-services-onnx12-customvision-sample/)