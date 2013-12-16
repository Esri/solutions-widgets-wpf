#Find Closest Facility Map Tool

Find the closest facility map tool identifies closest facilities from given incidents and returns the best routes to identified facilities.  This addin is built as a Map Tool with a Toolbar for the [Operations Dashboard for ArcGIS](http://resources.arcgis.com/en/operations-dashboard/).  The addin can be added as a Map Tool on a Map Widget.  No data is required to run the tool.

![Image of Operations Dashboard]( ScreenShot.png "solutions-widgets-wpf")

## Features

* Finds closest facilities from given incidents. To locate closest facilities:
1. Draw incident points on the map
2. Select facilities from a given layer 
3. Draw or select barriers on the map
4. Closest facilities are identified and the best routes are returned   

## Instructions
* How to set up the map tool: 
In the Configure Find Closest Facility Map Tool dialog set the following parameters:
1. Resource Layer (Facilities) - Specify resources layer. This layer contains facility locations that are searched for when finding the closest location. These locations are referred to as facilities or resources.
2. Select a field name for the layer. Facilities or Resource locations will be selected on the map based on attribute criteria. In the map tool, you will specifiy facilities layer and from that layer you will select given facilities (hospital, police stations etc).
3. Select barriers layers: Use this parameter to specify a layer that contains temporary restrictions or represent additional time or distance that may be required to travel on the underlying streets. 

### General Help

* [New to Github? Get started here.](http://htmlpreview.github.com/?https://github.com/Esri/esri.github.com/blob/master/help/esri-getting-to-know-github.html)

### Getting Started with this addin
* Open, build, and add the addin to the Operations Dashboard
* Add as a Map tool to a Map Widget
* Specify your parameters and run the tool.

## Requirements

* Visual Studio 2012
* ArcGIS Runtime SDK for WPF 10.2, included in the SDK is a copy of the Operations Dashboard
* Requires ArcGIS Closest Facility Services 
http://route.arcgis.com/arcgis/rest/services/World/ClosestFacility/NAServer/ClosestFacility_World/solveClosestFacility
* Download and reference Extended WPF Toolkit 1.9.0 
http://wpftoolkit.codeplex.com/releases/view/96972
 
### Services

* If you are using your own portal behind the firewall and cannot reach ArcGIS Online you will need to modify the service url to point to your own service.
* Requires ArcGIS Closest Facility Services 
http://route.arcgis.com/arcgis/rest/services/World/ClosestFacility/NAServer/ClosestFacility_World/solveClosestFacility

## Resources

* Learn more about the [Operations Dashboard for ArcGIS](http://resources.arcgis.com/en/operations-dashboard/)
* These widgets use [Esri's ArcGIS Runtime SDK for WPF](http://resources.arcgis.com/en/communities/runtime-wpf/);
see the site for concepts, samples, and references for using the API to create mapping applications.


## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Esri welcomes contributions from anyone and everyone. Please see our [guidelines for contributing](https://github.com/esri/contributing).

## Licensing

Copyright 2012 Esri

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

Note: Portions of this code use Extended WPF Toolkit whose use is governed by the Microsoft License (Ms-PL). For more details, see http://wpftoolkit.codeplex.com/license.


[](Esri Tags: ArcGIS Defense and Intelligence Situational Awareness ArcGIS Runtime WPF 10.2)
[](Esri Language: C#)