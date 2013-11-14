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
