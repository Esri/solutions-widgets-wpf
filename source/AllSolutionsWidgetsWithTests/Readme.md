#  solutions-widgets-wpf test projects

This solution tests the solutions-widgets-wpf projects.

## Features

* Test availability of dependent services
* Unit and mock/fake testing of the assemblies

## Requirements

* Visual Studio 2012 - Update 2 or later
    * IMPORTANT: Visual Studio 2012 Update 2 is required to run the Unit Tests provided with the repository (pre-Update 2 Visual Studio was missing required testing assemblies except in the Ultimate edition)
* ArcGIS Runtime SDK for WPF 10.2

## Getting Started with the test projects

* Open and run the test solution at source\AllSolutionsWidgetsWithTests with the Visual Studio Test Explorer
* If using the command line, you may also use VSTest.Console.exe or MSTest.exe to run the test projects' dlls. See [the MSDN page for more information](http://msdn.microsoft.com/en-us/library/vstudio/jj155796.aspx)
 
### Services

* See [TestDependentServices\TestDependentServices.cs](TestDependentServices\\TestDependentServices.cs) for a list of services tested

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Esri welcomes contributions from anyone and everyone. Please see our [guidelines for contributing](https://github.com/esri/contributing).

## Licensing

Copyright 2013 Esri

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

[](Esri Tags: ArcGIS Defense and Intelligence Situational Awareness ArcGIS Runtime WPF)
[](Esri Language: C#)