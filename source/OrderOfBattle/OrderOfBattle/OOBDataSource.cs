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
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OOB
{
    public enum DescriptionType
    {
        None,
        SingleField,
        Custom
    }
    public class OOBDataSource
    {
        private DataSource _ds;
        public DataSource DataSource
        {
            get
            {
                return _ds;
            }
        }
        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }
        }
        private String _id;
        public String ID
        {
            get
            {
                return _id;
            }
        }
        private String _uidfld;
        public String UIDField
        {
            get
            {
                return _uidfld;
            }
            set
            {
                _uidfld = value;
            }
        }
        private String _hffld;
        public String HFField
        {
            get
            {
                return _hffld;
            }
            set
            {
                _hffld = value;
            }
        }
        private List<String> _lblflds = new List<String>();
        public List<String> LabelFields
        {
            get
            {
                return _lblflds;
            }
        }
        private List<String> _descflds = new List<String>();
        public List<String> DescriptionFields
        {
            get
            {
                return _descflds;
            }
        }
        private DescriptionType _dType = DescriptionType.None;
        public DescriptionType DescType
        {
            get { return _dType; }
            set { _dType = value; }
        }
        private String _baseDesc = "";
        public String BaseDescription
        {
            get { return _baseDesc; }
            set { _baseDesc = value; }
        }
        private String _baseLabel = "";
        public String BaseLabel
        {
            get { return _baseLabel; }
            set { _baseLabel = value; }
        }
        private String _descField;
        public String DescField
        {
            get
            {
                return _descField;
            }
            set
            {
                _descField = value;
            }
        }
        private Boolean _useIcon;
        public Boolean UseIcon
        {
            get
            {
                return _useIcon;
            }
            set
            {
                _useIcon = value;
            }
        }

        private String _key;
        public String Key
        {
            get
            {
                return _key;
            }
        }
        private Boolean _isCacheCreated = false;
        public Boolean IsCacheCreated
        {
            get
            {
                return _isCacheCreated;
            }
            set
            {
                _isCacheCreated = value;
            }
        }
        private Boolean _isCacheUpdated = false;
        public Boolean IsCacheUpdated
        {
            get
            {
                return _isCacheUpdated;
            }
            set
            {
                _isCacheUpdated = value;
            }
        }

        private Dictionary<String, String> _tokens = new Dictionary<String, String>();
        public Dictionary<String, String> Tokens
        {
            get { return _tokens; }
        }

        public OOBDataSource(DataSource ds, String key)
        {
            _tokens.Add("@sc", ";");
            _tokens.Add("@cln", ":");
            _tokens.Add("@l", "|");
            _tokens.Add("@c", ",");
            _ds = ds;
            _name = ds.Name;
            _id = ds.Id;
            _key = key; 
        }

        public String Serialize()
        {
            String val = "";
            val += "KEY:" + _key + ",";
            val += "NAME:" + _name + ",";
            val += "ID:" + _id + ",";
            val += "USEICON:" + _useIcon.ToString() + ",";
            val += "UIDFLD:" + _uidfld + ",";
            val += "HFFLD:" + _hffld + ",";
            val += "LBLFLDS:";


            Boolean first = true;
            foreach (String l in _lblflds)
            {
                if (!first)
                {
                    val += "|";
                }
                val += l;
                first = false;
            }
            val += ",";
            val += "DESCFLD:" + _descField + ",";
            String baseDescription = _baseDesc;
            foreach (KeyValuePair<String, String>p in _tokens)
            {
                baseDescription = baseDescription.Replace(p.Value, p.Key);
            }
            val += "BASEDESC:" + baseDescription + ",";
            String baseLabel = _baseLabel;
            foreach (KeyValuePair<String, String> p in _tokens)
            {
                baseLabel = baseLabel.Replace(p.Value, p.Key);
            }
            val += "BASELABEL:" + baseLabel + ",";
            val += "DESCTYPE:" + _dType + ",";
            val += "DESCFLDS:";
            first = true;
            foreach (String l in _descflds)
            {
                if (!first)
                {
                    val += "|";
                }
                val += l;
                first = false;
            }

            return val;
        }
        public static OOBDataSource marshalODS(DataSource ds, String odsString)
        {
            String[] odsFields = odsString.Split(',');
            String key = null;
            String name = null;
            String id = null;
            String useiconstring = null;
            Boolean useicon = false;
            String uid = null;
            String hf = null;
            String df = null;
            String baseDesc = null;
            String baseLabel = null;
            String dtstring = null;
            String[] lflds = null;
            String[] dflds = null;
            //List<String> labels = new List<String>();
            //List<String> descriptions = new List<String>();
            foreach (String f in odsFields)
            {
                String[] vals = f.Split(':');
                if (vals[0].Equals("KEY"))
                {
                    key = vals[1];
                }
                if (vals[0].Equals("NAME"))
                {
                    name = vals[1];
                }
                if (vals[0].Equals("ID"))
                {
                    id = vals[1];
                }
                if (vals[0].Equals("USEICON"))
                {
                    useiconstring = vals[1];
                    if (useiconstring.ToLower().Equals("true"))
                    {
                        useicon = true;
                    }
                }
                if (vals[0].Equals("UIDFLD"))
                {
                    uid = vals[1];
                }
                if (vals[0].Equals("HFFLD"))
                {
                    hf = vals[1];
                }
                if (vals[0].Equals("LBLFLDS"))
                {
                    lflds = vals[1].Split('|');
                }
                if (vals[0].Equals("DESCFLD"))
                {
                    df = vals[1];
                }
                if (vals[0].Equals("BASEDESC"))
                {
                    baseDesc = vals[1];
                }
                if (vals[0].Equals("BASELABEL"))
                {
                    baseLabel = vals[1];
                }
                if (vals[0].Equals("DESCTYPE"))
                {
                    dtstring = vals[1];
                }
                if (vals[0].Equals("DESCFLDS"))
                {
                    dflds = vals[1].Split('|');
                }

            }
            OOBDataSource ods = new OOBDataSource(ds, key);
            ods.UIDField = uid;
            ods.HFField = hf;
            ods.DescField = df;
            ods.UseIcon = useicon;
            foreach (KeyValuePair<String, String> p in ods.Tokens)
            {
                baseDesc = baseDesc.Replace(p.Key, p.Value);
            }
            ods.BaseDescription = baseDesc;
            foreach (KeyValuePair<String, String> p in ods.Tokens)
            {
                baseLabel = baseLabel.Replace(p.Key, p.Value);
            }
            ods.BaseLabel = baseLabel;
            DescriptionType dtype = DescriptionType.None;
            if (dtstring.Equals("None"))
            {
                dtype = DescriptionType.None;
            }
            else if (dtstring.Equals("SingleField"))
            {
                dtype = DescriptionType.SingleField;
            }
            else if (dtstring.Equals("Custom"))
            {
                dtype = DescriptionType.Custom;
            }
            ods.DescType = dtype;
            if (lflds != null)
            {
                foreach (String l in lflds)
                {
                    ods.LabelFields.Add(l);
                }
            }
            if (dflds != null)
            {
                foreach (String l in dflds)
                {
                    ods.DescriptionFields.Add(l);
                }
            }

            return ods;
        }
    }
}
