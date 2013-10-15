# solutions-widgets-wpf

The Solutions Widgets (WPF) are examples of how to extend the [Operations Dashboard for ArcGIS](http://resources.arcgis.com/en/operations-dashboard/).  There are multiple Addins available to use as is or to take and modify to meet your needs.

![Image of Operations Dashboard]( ScreenShot.PNG "solutions-widgets-wpf")

## Features

* [Aircraft Communication Coverage Addin](source/AircraftCommunicationCoverageAddin/Readme.md)
* [Aircraft Route Generation Line Addin](source/AirCraftRouteGenerationLineAddin/Readme.md)
* [Bomb Threat Addin](source/BombThreatAddin/Readme.md)
* [Farthest On Circle Addin](source/FarthestOnCircleAddin/Readme.md)
* [Ground Communication Coverage Addin](source/GroundCommunicationCoverageAddin/Readme.md)
* [Order of Battle Addin](source/OrderOfBattle/Readme.md)
* [Range Fan Addin](source/RangeFanAddin/Readme.md)
* [Satellite Ephemeris Generation Addin](source/SatelliteEphemerisGenerationAddin/Readme.md)

## Sections

* [Requirements](#requirements)
* [Instructions](#instructions)
* [Resources](#resources)
* [Issues](#issues)
* [Contributing](#contributing)
* [Licensing](#licensing)

## Requirements

* Visual Studio 2012
* ArcGIS Runtime SDK for WPF 10.2
    * included in the SDK is a copy of the Operations Dashboard

## Instructions

### General Help

* [New to Github? Get started here.](http://htmlpreview.github.com/?https://github.com/Esri/esri.github.com/blob/master/help/esri-getting-to-know-github.html)

### Getting Started with the Solution Widgets (WPF)

* Building
    * To Build Using Visual Studio
        * Open, build, and add one of the addins to the Operations Dashboard
    * To use MSBuild to build the solution
        * Open a Visual Studio Command Prompt: Start Menu | Microsoft Visual Studio 2012 | Visual Studio Tools | Developer Command Prompt for VS 2012
        * `cd solutions-widgets-wpf\source\AllSolutionsWidgets`
        * `msbuild AllSolutionsWidgets.sln /property:Configuration=Release`
* Running Units Test to Verify Your Solution
    * Important Note: Visual Studio 2012 Update 2 is required to run the Unit Tests provided with the repository
    * Open and run the test solution at source\AllSolutionsWidgetsWithTests with the Visual Studio Test Explorer
    * See the Readme in the [Unit Test Solution](source/AllSolutionsWidgetsWithTests/Readme.md) for more information
* Running
    * Check the readme for each addin for more details about what each one does.
    * To run from Visual Studio:
        * Update Project Debug properties to correctly locate add-in build path in /addinpath command line argument. 
        * E.g. Command Line Arguments: /addinpath:"{FULLY QUALIFIED PATH TO}\solutions-widgets-wpf\applications"
    * To run from a command prompt:
        * > cd solutions-widgets-wpf\applications
        * > "C:\Program Files (x86)\ArcGIS SDKs\WPF10.2\sdk\OperationsDashboard\OperationsDashboard.exe" /addinpath:"{LOCAL PATH TO}\solutions-widgets-wpf\applications"
    * When Operations Dashboard application starts, edit an Operation View settings to choose one of the addins
    * When ready to test the deployment to ArcGIS Online
        * Upload one or more of the .opdashboardaddin files from the solutions-widgets-wpf\applications directory to ArcGIS Online, and then download using Manage Add-Ins in Operations Dashboard

### Services

* There are several services that the addins depend on to function.  You can open, build, and run the test project [TestDependentServices](source/AllSolutionsWidgetsWithTests/TestDependentServices/TestDependentServices.cs) to check which services may not be available.

## Resources

* Learn more about the [Operations Dashboard for ArcGIS](http://resources.arcgis.com/en/operations-dashboard/)
* Learn more about Esri's [ArcGIS for the Military](http://solutions.arcgis.com/military/).
* These widgets use [Esri's ArcGIS Runtime SDK for WPF](http://resources.arcgis.com/en/communities/runtime-wpf/);
see the site for concepts, samples, and references for using the API to create mapping applications.

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Esri welcomes contributions from anyone and everyone. Please see our [guidelines for contributing](https://github.com/esri/contributing).

## Licensing

Copyright 2012-2013 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's
[license.txt](license.txt) file.

Note: Portions of this code use Json.NET whose use is governed by the MIT License. For more details, see [http://json.codeplex.com/license](http://json.codeplex.com/license).

[](Esri Tags: ArcGIS Defense and Intelligence Situational Awareness ArcGIS Runtime WPF Military)
[](Esri Language: C#)
