---
services: cognitive-services,custom-vision
platforms: c#, Windows ML, XAML
author: kukramer
---

# Sample application for ONNX models exported from Custom Vision Service
This sample application demonstrates how to take a model exported from the [Custom Vision Service](https://www.customvision.ai) in the ONNX format and add it to an application for real-time image classification. 

## Getting Started

### Prerequisites
- [Windows SDK - Build 17110+] (latest)](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewSDK)
- [Visual Studio 17](https://www.visualstudio.com/vs/preview/)
- [Windows 10 Insider Preview](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewiso)
- An account at [Custom Vision Service](https://www.customvision.ai) 
### Quickstart

1. clone the repository and open the project in Visual Studio
2. Build and run the sample Application
3. Application comes with two models already included along with sample images to test.
### Adding your own sample model of your own classifier.
The models provided with the sample recognizes some foods(Cheesecake, Donuts, Fries) and teh other recognizes some plankton images. To add  your own model exported from the [Custom Vision Service](https://www.customvision.ai) do the following, and then build and launch the application:
  1. [Create and train](https://docs.microsoft.com/en-us/azure/cognitive-services/custom-vision-service/getting-started-build-a-classifier) a classifer with the Custom Vision Service. You must choose a "compact" domain such as **General (compact)** to be able to export your classifier. If you have an existing classifier you want to export instead, convert the domain in "settings" by clicking on the gear icon at the top right. In setting, choose a "compact" model, Save, and Train your project.  
  2. Export your model by going to the Performance tab. Select an iteration trained with a compact domain, an "Export" button will appear. Click on *Export* then *ONNX* then *Export.* Click the *Download* button when it appears. A *.onnx file will download.
  3. Drop your *model.onnx file into your project's Assets folder. 
  4. Under Solutions Explorer/ Assets Folder add model file to project by selecting Add Existing Item.
  5. Change properties of model just added: "Build Action" -> "Content"  and  "Copy to Output Directory" -> "Copy if newer"
  6. In the MainPage.xaml.cs file set the "_ourOnnxFileName" constant to the name of model just added and set the "_numLabels" constant to the number of labels that the model contains.
  7. Build and run.
  8. Click button to select image to evaluate.

### Things to note.
- Image preprocessing is performed by binding method call. Look at method "EvaluateAsync" of class "OnnxModel". Note that it binds a VideoFrame instance to "data"; this binding call will perform cropping and scaling such that the image fits the define size of model (227 x 227 with included models). It will also reformat the image data into a tensor that the model expects(Channel(BGR), Rows, Cols).
- Dictionary(map) based output bindings must pre-allocate number of entries for number of labels model was trained for. Ex: The included Plankton model contains 4 classes(labels) and for this reason the output variable for “loss” has 4 entries added to it.  The name of the labels do not need to match the actual name of the classes in the model.
- mlgen.exe - This tool generates API code in c# or c++ for a specified ONNX mode.  See [Windows ML overview](https://docs.microsoft.com/en-us/windows/uwp/machine-learning/overview) for description of utility. If you are working with a ONNX model and are unsure what variable types to utilize this utility will generate the correct types.

## Resources
- Link to [ONNX](https://onnx.ai/)
- Link to [ONNX on GitHub](https://github.com/onnx/onnx)
- Link to [Get started with Windows Machine Learning](https://docs.microsoft.com/en-us/windows/uwp/machine-learning/get-started)
- Link to [Custom Vision Service Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/custom-vision-service/home)
