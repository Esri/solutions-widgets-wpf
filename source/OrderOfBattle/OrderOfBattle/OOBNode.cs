using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OOB
{
    [Serializable]
    public enum updatestate {New = 0, Update = 1, Clean= 2 }
    class OOBNode: SortedDictionary<String, OOBNode>
    {
        public OOBNode(String uid, String name, OOBDataSource ods, String ntype)
        {
            _children = new SortedDictionary<String, OOBNode>();
            _ctype = childtype.NONE;
            setNtype(ntype);
             _key = uid;
            _name = name;
            _parent = null;
            _desc = null;
            ds = ods;
            
        }
        public OOBNode(String uid, String name, String desc, OOBDataSource ods, String ntype)
        {
            _children = new SortedDictionary<String, OOBNode>();
            _ctype = childtype.NONE;
            setNtype(ntype);
            _key = uid;
            _name = name;
            _parent = null;
            ds = ods;
            _desc = desc;
            
        }
        public OOBNode(String uid, String name, String desc, OOBDataSource ods, String ntype,  ImageSource imgsrc)
        {
            _children = new SortedDictionary<String, OOBNode>();
            _ctype = childtype.NONE;
            setNtype(ntype);
            _key = uid;
            _name = name;
            _parent = null;
            _is = imgsrc;
            ds = ods;
            _desc = desc;
            
        }
        /*public OOBNode(String uid, String name, OOBDataSource ods, String ntype, ImageSource imgsrc, OOBNode p)
        {
            _children = new SortedDictionary<String, OOBNode>();
            _ctype = childtype.NONE;
            setNtype(ntype);
            _key = uid;
            _name = name;
            _parent = p;
            _is = imgsrc;
            ds = ods;
        }*/
        private enum childtype { NONE = 0, UNITS = 1, DEPENDANTS = 2, BOTH = 3 };
        private childtype _ctype;
        private enum nodetype { UNITS=0, OTHER=1, TREEROOT=3 };
        private nodetype _ntype;
        private updatestate _state;
        
        private ImageSource _is;
        public ImageSource Icon
        {
            get
            {
                return _is;
            }
        }
        public String NType
        {
            get
            {
                String ntype = null;
                {
                    if (_ntype == nodetype.UNITS)
                    {
                        ntype= "UNITS";
                    }
                    else if (_ntype == nodetype.TREEROOT)
                    {
                        ntype = "TREEROOT";
                    }
                    else
                    {
                        ntype = ds.Key;
                    }
                    return ntype;
                }
            }
        }
        private void setNtype(String ntype)
        {
            if (ntype.Equals("UNITS"))
            {
                _ntype = nodetype.UNITS;
            }
            else if(ntype.Equals("TREEROOT"))
            {
                _ntype = nodetype.TREEROOT;
            }
            else
            {
                _ntype = nodetype.OTHER;
            }
        }
        public String CType
        {
            get
            {
                String ctype = "NONE";
                switch (_ctype)
                {
                    case childtype.UNITS:
                        ctype = "UNITS";
                        break;
                    case childtype.DEPENDANTS:
                        ctype = "DEPENDANTS";
                        break;
                    case childtype.BOTH:
                        ctype = "BOTH";
                        break;
                }
                return ctype;
            }
        }
        private String _desc;
        public String Description
        {
            get
            {
                return _desc;
            }
            set
            {
                _desc = value;
            }
        }
        private OOBDataSource ds;
        public OOBDataSource NodeDataSource
        {
            get
            {
                return ds;
            }
        }
        private OOBNode _parent;
        public OOBNode Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                _parent = value;
            }
        }
        private String _parentName;
        public String ParentName
        {
            get
            {
                return _parentName;
            }
            set
            {
                _parentName = value;
            }
        }
        private SortedDictionary<String, OOBNode> _children;
        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }
        }
        private String _uuid;
        public String UUID
        {
            get
            {
                return _uuid;
            }
            set
            {
                _uuid = value;
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
        public Boolean IsLeaf
        {
            get
            {
                if (this.numChildren > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public SortedDictionary<String, OOBNode> Children
        {
            get
            {
                return _children;
            }
        }
        public int numChildren 
        { 
            get 
            {
                return Children.Count;
            } 
        }
        public Boolean IsRoot
        { 
            get
            {
                if (this.Parent == null)
                    return true;
                else
                    return false;
            }
        }

        public void addChild(OOBNode n)
        {

            if (!this.Children.ContainsKey(n.Key))
            {
                this.Children.Add(n.Key, n);
                updatectype(n);
            }
            
        }

        private void updatectype(OOBNode n)
        {
            if (n.Parent != null)
            {
                if (n.numChildren > 0) //node not leaf -- must be ntype->UNIT
                {
                    //node is UNIT parent previously only had equipment etc.
                    if (n.Parent.CType.Equals("DEPENDANTS")) 
                    {
                        n.Parent._ctype = childtype.BOTH;
                        propagateCtype(n.Parent);
                    }
                    //parent's children already units and other
                    else if (n.Parent.CType.Equals("BOTH"))
                    {
                        propagateCtype(n.Parent);
                    }
                    else if(n.Parent.CType.Equals("UNITS"))
                    {
                        if (n.CType.Equals("DEPENDANTS"))
                        {
                            n.Parent._ctype = childtype.BOTH;
                            propagateCtype(n.Parent);
                        }
                        else
                        {
                            updatectype(n.Parent);
                        }
                    }

                }
                else //node is leaf ctype stays none...
                {
                    //Update parent's ctype

                    //node is unit
                    if (n.NType.Equals("UNITS"))
                    {
                        //now parent has both kinds of children
                        if (n.Parent.CType.Equals("DEPENDANTS"))
                        {
                            n.Parent._ctype = childtype.BOTH;
                            propagateCtype(n.Parent);
                        }
                        //parent now has childtype of added node - UNITS
                        else if (n.Parent.CType.Equals("NONE"))
                        {
                            n.Parent._ctype = childtype.UNITS;
                            updatectype(n.Parent);
                        }
                        //parent type already both everything above it is both
                        else if (n.Parent.CType.Equals("BOTH"))
                        {
                            propagateCtype(n.Parent);
                        }
                        else
                            updatectype(n.Parent);

                    }
                    else
                    {
                        //now parent has both kinds of children
                        if (n.Parent.CType.Equals("UNITS"))
                        {
                            n.Parent._ctype = childtype.BOTH;
                            propagateCtype(n.Parent);
                        }
                        //parent now has childtype of added node - DEPENDANTS
                        else if (n.Parent.CType.Equals("NONE"))
                        {
                            n.Parent._ctype = childtype.DEPENDANTS;
                            updatectype(n.Parent);
                        }
                        //parent type already both everything above it is both
                        else if (n.Parent.CType.Equals("BOTH"))
                        {
                            propagateCtype(n.Parent);
                        }
                        else
                            updatectype(n.Parent);
                    }
                }
            }
        }
        private void propagateCtype(OOBNode n)
        {
            if (n.Parent != null)
            {
                n.Parent._ctype = childtype.BOTH;
                propagateCtype(n.Parent);
            }
        }

        public void removeChild(String uid)
        {
            _children.Remove(uid);
        }

        public OOBNode GetDescendant(String uid)
        {
            OOBNode foundNode = null;
            foreach (KeyValuePair<String, OOBNode> pair in this.Children)
            {
                if (foundNode != null)
                {
                    return foundNode;
                }
                if (pair.Key.Equals(uid))
                    foundNode = pair.Value;
                else
                {
                    foundNode = pair.Value.GetDescendant(uid);
                }
            }
            return foundNode;
        }
    }
}
