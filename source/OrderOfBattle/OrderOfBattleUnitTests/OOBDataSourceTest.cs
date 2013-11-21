﻿/* Copyright 2013 Esri
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using fake = ESRI.ArcGIS.OperationsDashboard.Fakes;

using OOB;

namespace OrderOfBattleUnitTest
{
    /// <summary>
    /// Summary description for OOBDataSourceTests
    /// </summary>
    [TestClass]
    public class OOBDataSourceTests
    {
        private OOBDataSource ods;
        String serialized;
        private fake.ShimDataSource fds;
        private String odsString = "";
        public OOBDataSourceTests()
        {
            using (ShimsContext.Create())
            {
                fds = new fake.ShimDataSource();
                fds.NameGet = () => { return "Friendly Situation - Friendly Equipment"; };
                fds.IdGet = () => { return "b8725bd2-1aa2-4a06-895b-d87a8028d75a"; };
                using (FileStream fs = File.OpenRead("C:/Dev/VS/DefenseSolution/OrderOfBattle/OrderOfBattleUnitTests/resources/oobdatasources.txt")) 
                {
                    byte[] b = new byte[1024];
                    UTF8Encoding temp = new UTF8Encoding(true);
                    while (fs.Read(b,0,b.Length) > 0) 
                    {
                        odsString += (temp.GetString(b));
                    }
                }
                serialized = odsString.Split(';')[1];
                ods = OOBDataSource.marshalODS(fds, serialized);
            }
            
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestSerialize()
        {
            String s = ods.Serialize();
            
            Assert.AreEqual(serialized, s);
        }
    }
}
