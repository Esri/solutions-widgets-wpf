/* Copyright 2013 Esri
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestDependentServices
{
    [TestClass]
    public class TestDependentServices
    {
        Dictionary<string, string> PublicServicesUrlToName = new Dictionary<string, string>()
        {
            // BombThreatToolbar, Buffer Map Tool --> ArcGIS Online:
            { "http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer", 
                "Bomb Threat Addin - Geometry Service" },
            // Try It Live Staging Portal:
            { "http://afmcloud.esri.com/arcgis/rest/services/Tasks/FarthestOnCircle/GPServer/Farthest%20On%20Circle", 
                "Farthest On Circle Addin - geoprocessing service" },
            // ERG Chemical Addin 1-3
            { "http://arcgis-emergencymanagement-2057568539.us-east-1.elb.amazonaws.com/arcgis/rest/services/COP/ERGByChemical/GPServer/ERG%20By%20Chemical", 
                "ERG Chemical Addin - geoprocessing service 1" },
            { "http://arcgis-emergencymanagement-2057568539.us-east-1.elb.amazonaws.com/arcgis/rest/services/COP/ERGByPlacard/GPServer/ERG%20By%20Placard",
                "ERG Chemical Addin - geoprocessing service 2" },
            { "http://arcgis-emergencymanagement-2057568539.us-east-1.elb.amazonaws.com/arcgis/rest/services/COP/FindNearestWS/GPServer/FindNearestWeatherStation",
                "ERG Chemical Addin - geoprocessing service 3" }

            // TODO: Removed until this link can be resolved: 
            // Find Closest Resource
            // { "http://route.arcgis.com/arcgis/rest/services/World/ClosestFacility/NAServer/ClosestFacility_World/solveClosestFacility", 
            //    "Find Closest Resource - routing service" }
 
        };

        // TODO: Need to get services hosted publicly - Issue: https://github.com/ArcGIS/solutions-widgets-wpf/issues/10
        Dictionary<string, string> PrivateServicesUrlToName = new Dictionary<string, string>()
        {
            // AGI:
            //{ "TODO ADD URL TO AGI'S AIRCRAFT COMMUNICATION COVERAGE GEOPROCESSING SERVICE, THIS IS REQUIRED FOR THE THIS ADDIN TO FUNCTION", 
            //    "Aircraft Communication Coverage Addin: Aircraft Communication Coverage Service" },
            //{ "TODO ADD URL TO AGI'S AIRCRAFT ROUTE GENERATION TO LINE GEOPROCESSING SERVICE, THIS IS REQUIRED FOR THE THIS ADDIN TO FUNCTION", 
            //    "Aircraft Route Generation Line Addin: Aircraft Route Generation Service" },
            //{ "TODO ADD URL TO AGI'S GROUND COMMUNICATION COVERAGE GEOPROCESSING SERVICE, THIS IS REQUIRED FOR THE THIS ADDIN TO FUNCTION",
            //    "Ground Communication Coverage Addin: Ground Communication Coverage (Power)" },
            //{ "TODO ADD URL TO AGI'S SATELLITE EPHEMERIS GENERATION GEOPROCESSING SERVICE, THIS IS REQUIRED FOR THE THIS ADDIN TO FUNCTION",
            //    "Satellite Ephemeris Generation Addin: Satellite Ephemeris Generation" }
        };

        [TestMethod]
        public void TestPrivateServices()
        {
            foreach (string serviceUrl in PrivateServicesUrlToName.Keys)
            {
                string name = PrivateServicesUrlToName[serviceUrl];
                Console.WriteLine("Testing Service: " + name);

                bool available = ServiceAvailable(serviceUrl);
                if (!available)
                {
                    Console.WriteLine("SERVICE NOT AVAILABLE: " + name);
                    Console.WriteLine("Failing URL: " + serviceUrl);
                }

                Assert.IsTrue(available);
            }
        }

        [TestMethod]
        public void TestPublicServices()
        {
            foreach (string serviceUrl in PublicServicesUrlToName.Keys)
            {
                string name = PublicServicesUrlToName[serviceUrl];
                Console.WriteLine("Testing Service: " + name);

                bool available = ServiceAvailable(serviceUrl);
                if (!available)
                {
                    Console.WriteLine("SERVICE NOT AVAILABLE: " + name);
                    Console.WriteLine("Failing URL: " + serviceUrl);
                }

                Assert.IsTrue(available);
            }
        }

        static bool ServiceAvailable(string strService)
        {
            try
            {
                HttpWebRequest reqFP = (HttpWebRequest)HttpWebRequest.Create(strService);
                HttpWebResponse rspFP = (HttpWebResponse)reqFP.GetResponse();
                if (HttpStatusCode.OK == rspFP.StatusCode)
                {
                    // HTTP = 200 - Internet connection available, server online
                    rspFP.Close();
                    return true;
                }
                else
                {
                    // Other status - Server or connection not available
                    rspFP.Close();
                    return false;
                }
            }
            catch (WebException)
            {
                // Exception - connection not available
                return false;
            }
        }
    }

}
