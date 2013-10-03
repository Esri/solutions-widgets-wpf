using System;
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

namespace OrderOfBattle102UnitTests
{
    /// <summary>
    /// Summary description for OOBNodeTest
    /// </summary>
    [TestClass]
    public class OOBNodeTest
    {
        private OOBDataSource ods;
        private fake.ShimDataSource fds;
        public OOBNodeTest()
        {
            using (ShimsContext.Create())
            {
                fds = new fake.ShimDataSource();
                ods = new OOBDataSource(fds, "fake");
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
        public void IsRootTestTrue()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "test0");
            Assert.IsTrue(n0.IsRoot);
        }
        [TestMethod]
        public void IsRootTestFalse()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "test0");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "test1");
            n0.addChild(n1);
            Assert.IsFalse(n1.IsRoot);
        }
        [TestMethod]
        public void ValidateParentName()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "test0");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "test1");
            n0.addChild(n1);
            Assert.AreEqual(n1.ParentName, n0.Name);
        }
        [TestMethod]
        public void ValidateNodeTypeUnits()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "UNITS");
            
            Assert.AreEqual(n0.NType, "UNITS");
        }
        [TestMethod]
        public void ValidateNodeTypeRoot()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "TREEROOT");
            Assert.AreEqual(n0.NType, "TREEROOT");
        }
        [TestMethod]
        public void ValidateNodeTypeNotUnitsAndNotRoot()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "test0");
            Boolean units = n0.NType.Equals("UNITS");
            Boolean root = n0.NType.Equals("TREEROOT");
            Assert.IsTrue((!units && !root));
        }
        [TestMethod]
        public void ValidateNodeCTypeNone()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "UNITS");
            Assert.IsTrue(n1.CType.Equals("NONE"));
        }
        [TestMethod]
        public void ValidateNodeCTypeUnits()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "UNITS");
            n0.addChild(n1);
            Assert.IsTrue(n0.CType.Equals("UNITS"));
        }
        [TestMethod]
        public void ValidateNodeCTypeDependants()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "EQUIPMENT");
            n0.addChild(n1);
            Assert.IsTrue(n0.CType.Equals("DEPENDANTS"));
        }
        [TestMethod]
        public void ValidateNodeCTypeBoth()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n2 = new OOBNode("test2", "test2", ods, "EQUIPMENT");
            n0.addChild(n1);
            n1.addChild(n2);
            Assert.IsTrue(n0.CType.Equals("BOTH"));
        }
        [TestMethod]
        public void ValidateAllParentNodeCTypeBoth()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n2 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n3 = new OOBNode("test3", "test3", ods, "EQUIPMENT");
            n0.addChild(n1);
            n1.addChild(n2);
            n2.addChild(n3);
            Assert.IsTrue(n3.Parent.Parent.CType.Equals("BOTH") && n3.Parent.Parent.Parent.CType.Equals("BOTH"));
        }
        [TestMethod]
        public void ValidateParentNodeCTypeBothDependantToUnit()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "UNITS");
            OOBNode n10 = new OOBNode("test3", "test3", ods, "UNITS");
            OOBNode n11 = new OOBNode("test4", "test4", ods, "EQUIPMENT");
            n00.addChild(n01);
            n01.addChild(n02);
            n10.addChild(n11);
            n01.addChild(n10);

            Assert.IsTrue(n00.CType.Equals("BOTH"));
        }
        [TestMethod]
        public void ValidateParentNodeCTypeBothUnitToDependant()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "UNITS");
            
            OOBNode n10 = new OOBNode("test3", "test3", ods, "UNITS");
            OOBNode n11 = new OOBNode("test4", "test4", ods, "EQUIPMENT");
            
            n00.addChild(n01);
            n01.addChild(n02);
            n10.addChild(n11);
            n10.addChild(n00);

            Assert.IsTrue(n10.CType.Equals("BOTH"));
        }

        [TestMethod]
        public void ValidateParentNodeCTypeBothAlreadyBoth()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "EQUIPMENT");

            OOBNode n10 = new OOBNode("test3", "test3", ods, "UNITS");
            OOBNode n11 = new OOBNode("test4", "test4", ods, "UNITS");
            OOBNode n12 = new OOBNode("test5", "test5", ods, "EQUIPMENT");

            n00.addChild(n01);
            n01.addChild(n02);

            n10.addChild(n11);
            n11.addChild(n12);

            n00.addChild(n10);

            Assert.IsTrue(n00.CType.Equals("BOTH"));
        }
        [TestMethod]
        public void ValidateParentNodeCTypeBothAlreadyBothAddUnit()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "EQUIPMENT");

            OOBNode n10 = new OOBNode("test3", "test3", ods, "UNITS");
            OOBNode n11 = new OOBNode("test4", "test4", ods, "UNITS");
      

            n00.addChild(n01);
            n01.addChild(n02);
            n10.addChild(n11);
            n00.addChild(n10);

            Assert.IsTrue(n00.CType.Equals("BOTH"));
        }

        [TestMethod]
        public void ValidateParentNodeCTypeBothAlreadyBothAddDependent()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "EQUIPMENT");

            OOBNode n10 = new OOBNode("test3", "test3", ods, "UNITS");
            OOBNode n11 = new OOBNode("test4", "test4", ods, "EQUIPMENT");


            n00.addChild(n01);
            n01.addChild(n02);
            n10.addChild(n11);
            n00.addChild(n10);

            Assert.IsTrue(n00.CType.Equals("BOTH"));
        }

        [TestMethod]
        public void AddChildIncreaseNumChildren()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            int numChildren = n00.Children.Count;
            n00.addChild(n01);

            Assert.IsTrue(numChildren < n00.Children.Count);
        }

        [TestMethod]
        public void RemoveChildDecreaseNumChildren()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            
            n00.addChild(n01);
            int numChildren = n00.Children.Count;
            n00.removeChild("test1");

            Assert.IsTrue(numChildren > n00.Children.Count);
        }

        [TestMethod]
        public void GetDescendantExists()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "UNITS");
            
            n00.addChild(n01);
            n01.addChild(n02);
            OOBNode n = n00.GetDescendant("test2");
            n00.removeChild("test1");

            Assert.AreEqual(n02, n);
        }
        [TestMethod]
        public void GetDescendantDoesNotExist()
        {
            OOBNode n00 = new OOBNode("test0", "test0", ods, "UNITS");
            OOBNode n01 = new OOBNode("test1", "test1", ods, "UNITS");
            OOBNode n02 = new OOBNode("test2", "test2", ods, "UNITS");

            n00.addChild(n01);
            n01.addChild(n02);
            OOBNode n = n00.GetDescendant("test3");
            n00.removeChild("test1");

            Assert.AreNotEqual(n02, n);
        }

        [TestMethod]
        public void IsLeafTestTrue()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "test0");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "test1");
            Assert.IsTrue(n1.IsLeaf);
        }
        [TestMethod]
        public void IsLeafTestFalse()
        {
            OOBNode n0 = new OOBNode("test0", "test0", ods, "test0");
            OOBNode n1 = new OOBNode("test1", "test1", ods, "test1");
            n0.addChild(n1);
            Assert.IsFalse(n0.IsLeaf);
        }
    }
}
