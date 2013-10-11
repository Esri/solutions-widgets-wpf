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
            { "http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer", 
                "Bomb Threat Addin - Geometry Service" },
            // TODO: Not sure if this next one is public or private - todo - test 
            { "https://afmcomstaging.esri.com/arcgis/rest/services/Tasks/FarthestOnCircle/GPServer/Farthest%20On%20Circle", 
                "Farthest On Circle Addin - geoprocessing service" }            
        };

        Dictionary<string, string> PrivateServicesUrlToName = new Dictionary<string, string>()
        {
            { "http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/AircraftCommunicationCoverage/GPServer/AircraftCommunicationCoverage", 
                "Aircraft Communication Coverage Addin: Aircraft Communication Coverage Service" },
            { "http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/AircraftRouteGenerationToLine/GPServer/Aircraft%20Route%20Generation%20To%20Line", 
                "Aircraft Route Generation Line Addin: Aircraft Route Generation Service" },
            { "http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/GroundCommunicationCoverage_Power/GPServer/GroundCommunicationCoverage",
                "Ground Communication Coverage Addin: Ground Communication Coverage (Power)" },
            { "http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/SatelliteEphemerisGeneration/GPServer/Satellite%20Ephemeris%20Generation",
                "Satellite Ephemeris Generation Addin: Satellite Ephemeris Generation" }
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
