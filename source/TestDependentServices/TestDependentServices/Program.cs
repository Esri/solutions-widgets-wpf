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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace TestDependentServices
{
    class Program
    {
        static void Main(string[] args)
        {
            //Checking service availability
            bool agiServices = false;
            bool otherServices = false;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Checking availability of dependent services...");
            if (!ServiceAvailable("http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/AircraftCommunicationCoverage/GPServer/AircraftCommunicationCoverage"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Aircraft Communication Coverage Addin: Aircraft Communication Coverage Service is NOT available.");
                agiServices = true;
            }
            if (!ServiceAvailable("http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/AircraftRouteGenerationToLine/GPServer/Aircraft%20Route%20Generation%20To%20Line"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Aircraft Route Generation Line Addin: Aircraft Route Generation to Line Service is NOT available.");
                agiServices = true;
            }
            if (!ServiceAvailable("http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/GroundCommunicationCoverage_Power/GPServer/GroundCommunicationCoverage"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gound Communication Coverage Addin: Ground Communication Coverage (Power) is NOT available.");
                agiServices = true;
            }
            if (!ServiceAvailable("http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/SatelliteEphemerisGeneration/GPServer/Satellite%20Ephemeris%20Generation"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Satellite Ephemeris Generation Addin: Satellite Ephemeris Generation is NOT available.");
                agiServices = true;
            }
            if (agiServices)
                Console.WriteLine("Please contact Todd Smith from AGI at tsmith@agi.com for the above services that are not running.");

            if (!ServiceAvailable("http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Bomb Threat Addin: You are unable to access the Geometry service from ArcGIS Online. You will need to update the service to point to your own geometry service.");
                otherServices = true;
            }
            if (!ServiceAvailable("https://afmcomstaging.esri.com/arcgis/rest/services/Tasks/FarthestOnCircle/GPServer/Farthest%20On%20Circle"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Farthest On Circle Addin: You are unable to access the geoprocessing service on afcomstaging.esri.com server");
                otherServices = true;
            }

            if (otherServices || agiServices)
                Console.WriteLine("The above addins will not function unless the above services can be accessed.");
            else
                Console.WriteLine("No errors reported.  All services can be accessed.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press any key to close...");
            Console.ReadKey(true);

           
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
