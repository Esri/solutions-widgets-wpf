using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OOB
{
    public class OOBTree
    {
        public OOBTree()
        { }
        public OOBTree(OOBNode node)
        {
            _root.addChild(node);
        }

        private OOBNode _root = new OOBNode("ROOT", "Root", null, "TREEROOT");
        public OOBNode Root { get { return _root; } }

        private Dictionary<String, String> _keys = new Dictionary<String, String>();
        public Dictionary<String, String> Keys
        {
            get
            {
                return _keys;
            }
        }
        private Dictionary<String, DataSource> _datasources = new Dictionary<String, DataSource>();
        public Dictionary<String, DataSource> DataSources
        {
            get
            {
                return _datasources;
            }
        }
        public void AddNode(OOBNode n)
        {
            _root.addChild(n);
        }
        public void AddDataSource(String dsid, DataSource ds, String type)
        {
            _keys.Add(dsid, type);
            _datasources.Add(type, ds);
        }
    }
}
