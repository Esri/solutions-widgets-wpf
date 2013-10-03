using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Collections.Generic;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using opsfake = ESRI.ArcGIS.OperationsDashboard.Fakes;
using clientfake = ESRI.ArcGIS.Client.Fakes;
using System.Windows.Media.Imaging;
using OOB;


namespace OrderOfBattle102UnitTests
{
    /// <summary>
    /// Summary description for OOBCacheTest
    /// </summary>
    [TestClass]
    public class OOBCacheTest
    {
        private OOBCache cache;
        public OOBCacheTest()
        {
            cache = new OOBCache();
            cache.AddFeatuereContainer("UNITS");
            cache.AddFeatuereContainer("EQUIPMENT");
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
        public void TestCacheAddFeature()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();

                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "1-1";
                attributes["higherformation"] = "1";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "higherformation";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = null;
                fields["DESCFIELD"] = null;
                int count = cache.RetrieveFeatureCache("UNITS").Count;
                cache.AddFeature("UNITS", g, "", "{uniquedesignation}", fields);
                Assert.IsTrue(cache.RetrieveFeatureCache("UNITS").Count > count);
            }
            
            
        }
        [TestMethod]
        public void TestCacheUpdateFeature()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();

                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "1-1";
                attributes["higherformation"] = "1";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "higherformation";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = null;
                fields["DESCFIELD"] = null;
                int count = cache.RetrieveFeatureCache("UNITS").Count;
                cache.AddFeature("UNITS", g, "", "{uniquedesignation}", fields);
                String coords = cache.RetrieveFeatureCache("UNITS")["1-1"]["COORDS"].ToString();
                client.Geometry.MapPoint p1 = new client.Geometry.MapPoint(1.0, 0.0);
                g.GeometryGet = () => { return p1; };
                cache.UpdateFeature("UNITS", "1-1", "", "{uniquedesignation}", g, fields);
                String newcoords = cache.RetrieveFeatureCache("UNITS")["1-1"]["COORDS"].ToString();
                Assert.AreNotEqual(newcoords, coords);
            }
        }
        [TestMethod]
        public void TestCacheAddFeatureWithDesc()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();

                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "Wolfpack 1 C";
                attributes["owningunit"] = "2nd Stryker Brigade";
                attributes["type"] = "M1128";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "owningunit";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = "type";
                fields["DESCFIELD"] = null;
                int count = cache.RetrieveFeatureCache("EQUIPMENT").Count;
                cache.AddFeature("EQUIPMENT", g, "Type: {type}", "{uniquedesignation}", fields);
                Assert.IsTrue(cache.RetrieveFeatureCache("EQUIPMENT").Count > count);
            }


        }
        [TestMethod]
        public void TestCacheUpdateFeatureWithDesc()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();

                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "Wolfpack 1 C";
                attributes["owningunit"] = "2nd Stryker Brigade";
                attributes["type"] = "M1128";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "owningunit";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = "type";
                fields["DESCFIELD"] = "type";
                int count = cache.RetrieveFeatureCache("EQUIPMENT").Count;
                cache.AddFeature("EQUIPMENT", g, "Type: {type}", "{uniquedesignation} owned by {owningunit}", fields);
                cache.UpdateFeature("EQUIPMENT", "Wolfpack 1 C", "{type}", "{uniquedesignation}", g, fields);
                Dictionary<String, Dictionary<String, Object>> fcache = cache.RetrieveFeatureCache("EQUIPMENT");
                Dictionary<String, Object> f = fcache["Wolfpack 1 C"];
                String desc = f["DESCRIPTION"].ToString();
                Assert.AreEqual(desc, "M1128");
            }
        }
        [TestMethod]
        public void TestCacheUpdateFeatureLabel()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();

                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "Wolfpack 1 C";
                attributes["owningunit"] = "2nd Stryker Brigade";
                attributes["type"] = "M1128";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "owningunit";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = "type";
                fields["DESCFIELD"] = "type";
                int count = cache.RetrieveFeatureCache("EQUIPMENT").Count;
                cache.AddFeature("EQUIPMENT", g, "Type: {type}", "{uniquedesignation}", fields);
                fields["LABELS"] = "uniquedesignation,owningunit";
                cache.UpdateFeature("EQUIPMENT", "Wolfpack 1 C", "Type: {type}", "{uniquedesignation} owned by {owningunit}", g, fields);
                Dictionary<String, Dictionary<String, Object>> fcache = cache.RetrieveFeatureCache("EQUIPMENT");
                Dictionary<String, Object> f = fcache["Wolfpack 1 C"];
                String label = f["LABEL"].ToString();
                Assert.AreEqual(label, "Wolfpack 1 C owned by 2nd Stryker Brigade");
            }
        }

        [TestMethod]
        public void TestCacheIsDirty()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();

                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "Wolfpack 1 C";
                attributes["owningunit"] = "2nd Stryker Brigade";
                attributes["type"] = "M1128";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "owningunit";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = "type";
                fields["DESCFIELD"] = "type";
                int count = cache.RetrieveFeatureCache("EQUIPMENT").Count;
                cache.AddFeature("EQUIPMENT", g, "Type: {type}", "{uniquedesignation}", fields);
                fields["LABELS"] = "uniquedesignation,owningunit";
                cache.UpdateFeature("EQUIPMENT", "Wolfpack 1 C", "Type: {type}", "{uniquedesignation} owned by {owningunit}", g, fields);
                Dictionary<String, Dictionary<String, Object>> fcache = cache.RetrieveFeatureCache("EQUIPMENT");
                Dictionary<String, Object> f = fcache["Wolfpack 1 C"];
                String label = f["LABEL"].ToString();
                Assert.IsTrue(cache.IsDirty);
            }
        }
        [TestMethod]
        public void TestCacheModifySymbol()
        {
            using (ShimsContext.Create())
            {
                client.Geometry.MapPoint p = new client.Geometry.MapPoint(0.0, 0.0);
                clientfake.ShimGraphic g = new clientfake.ShimGraphic();
                client.FeatureService.Symbols.PictureMarkerSymbol pms = new client.FeatureService.Symbols.PictureMarkerSymbol();
                Uri myUri = new Uri("../../resources/zoom_in_tool_1.bmp", UriKind.RelativeOrAbsolute);
                
                BitmapDecoder decoder = new BmpBitmapDecoder(myUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bitmapSource = decoder.Frames[0];
                pms.Source = bitmapSource;
                
                Dictionary<String, Object> attributes = new Dictionary<String, Object>();
                attributes["uniquedesignation"] = "Wolfpack 1 C";
                attributes["owningunit"] = "2nd Stryker Brigade";
                attributes["type"] = "M1128";
                g.AttributesGet = () => { return attributes; };
                g.GeometryGet = () => { return p; };
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = "uniquedesignation";
                fields["HF"] = "owningunit";
                fields["LABELS"] = "uniquedesignation";
                fields["DESCFLDS"] = "type";
                fields["DESCFIELD"] = "type";
                int count = cache.RetrieveFeatureCache("EQUIPMENT").Count;
                cache.AddFeature("EQUIPMENT", g, "Type: {type}", "{uniquedesignation}", fields);
                fields["LABELS"] = "uniquedesignation,owningunit";
                g.SymbolGet = () => {return pms;};
                cache.UpdateFeature("EQUIPMENT", "Wolfpack 1 C", "Type: {type}", "{uniquedesignation} owned by {owningunit}", g, fields);
                Dictionary<String, Dictionary<String, Object>> fcache = cache.RetrieveFeatureCache("EQUIPMENT");
                Dictionary<String, Object> f = fcache["Wolfpack 1 C"];
                ImageSource imsrc = f["ICON"] as ImageSource;
                Assert.AreEqual(pms.Source, imsrc);
            }
        }
    }
}
