using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OOB
{
    public class OOBCache
    {
        public OOBCache() { }
        private Int32 r = 0;
        private Dictionary<String, Dictionary<String, Dictionary<String, object>>> _items = new Dictionary<String, Dictionary<String, Dictionary<String, object>>>();
        //private SymbolCatalog _symcat = new SymbolCatalog();
        public Boolean IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
            }
        }
        private Boolean _isDirty = false;
        public void AddFeatuereContainer(String key)
        {
            Dictionary<String, Dictionary<String, object>> fc = new Dictionary<String, Dictionary<String, object>>();
            _items[key] = fc;
            Dictionary<String, Dictionary<String, object>> ufc = new Dictionary<String, Dictionary<String, object>>();
            String updates_key = key + "_UPDATE";
            _items[updates_key] = ufc;
        }
        public void AddFeature(String key, client.Graphic feature, String baseDesc, Dictionary<String, String> fields)
        {
            try
            {
                Dictionary<String, Dictionary<String, object>> fList = _items[key];
                Dictionary<String, object> attributes = new Dictionary<String, object>();

                object uid = null, hf = null, label = null, description = null;
                uid = feature.Attributes[fields["UID"]];
                hf = feature.Attributes[fields["HF"]];
                label = parseLabel(feature, fields["LABELS"]);
                String descFlds = fields["DESCFLDS"];
                String df = fields["DESCFIELD"];
                description = createDescriptionString(feature, baseDesc, df, descFlds);
                client.FeatureService.Symbols.PictureMarkerSymbol sym = feature.Symbol as client.FeatureService.Symbols.PictureMarkerSymbol;
                ImageSource isrc = null;
                if (uid != null)
                {
                    if (!fList.ContainsKey(uid.ToString()))
                    {
                        if (hf != null)
                        {
                            attributes["HF"] = hf.ToString();
                        }
                        else
                        {
                            attributes["HF"] = null;
                        }
                        if (label != null)
                        {
                            attributes["LABEL"] = label;
                        }
                        else
                        {
                            attributes["LABEL"] = null;
                        }
                        if (description != null)
                        {
                            attributes["DESCRIPTION"] = description;
                        }
                        else
                        {
                            attributes["DESCRIPTION"] = null;
                        }
                        if (sym != null)
                        {
                            
                            attributes["ICON"] = sym.Source;
                        }
                        else
                        {
                            attributes["ICON"] = isrc;
                        }
                        fList.Add(uid.ToString(), attributes);
                        _isDirty = true;
                    }
                    else
                        r++;
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }

        private String parseLabel(client.Graphic f, String label)
        {
            if (label == null)
                return null;
            if (label.Equals(""))
                return null;
            String[] labelflds = label.Split(',');
            String l = "";
            
            for (Int32 i = 0; i < labelflds.Length; ++i)
            {
                if(i != 0)
                {
                    l += " ";
                }
                String fld = labelflds[i];
                l += f.Attributes[fld];
            }
            return l;
        }
        private String createDescriptionString(client.Graphic f, String basedesc, String descfield, String descFlds)
        {
            String d = null;
            String desc = null;
            if (!String.IsNullOrEmpty(descfield))
            {
                if (f.Attributes[descfield] == null)
                {
                    d = null;
                    return d;
                }
                
                desc = f.Attributes[descfield].ToString();
                if (!String.IsNullOrEmpty(basedesc))
                {
                    if (String.IsNullOrEmpty(descFlds))
                    {
                        d = basedesc;
                    }
                    else
                    {
                        String[] flds = descFlds.Split(',');
                        foreach (String df in flds)
                        {
                            if (f.Attributes[df] != null)
                            {
                                d = basedesc.Replace("{" + df + "}", f.Attributes[df].ToString());
                            }
                            else
                            {
                                d = basedesc.Replace("{" + df + "}", "");
                            }
                        }
                    }
                }
                else
                {
                    d = desc;
                }
            }
            else
            {
                d = null;
            }
            if (String.IsNullOrEmpty(d))
            {
                return null;
            }
            return d;
        }
        public Boolean UpdateFeature(String key, String id, String baseDesc, client.Graphic feature, Dictionary<String, String> fields)
        {
            Boolean featureUpdated = false;
            Dictionary<String, Dictionary<String, object>> fList = _items[key];
            Dictionary<String, object> attributes = new Dictionary<String, object>();
            Dictionary<String, object> f = fList[id];
            object hf = feature.Attributes[fields["HF"]];
            object currenthf = f["HF"];
            object labelobj = parseLabel(feature, fields["LABELS"]);
            String df = fields["DESCFIELD"];
            String descFlds = fields["DESCFLDS"];
            object descriptionobj = createDescriptionString(feature, baseDesc, df, descFlds);
            object currentLabel = f["LABEL"];
            object currentDescription = f["DESCRIPTION"];
            //object nameobj = feature.Attributes[fields["NAME"]];
            //object currentName = f["NAME"];
            
            client.FeatureService.Symbols.PictureMarkerSymbol sym = feature.Symbol as client.FeatureService.Symbols.PictureMarkerSymbol;
            object currentsym = f["ICON"];

            if (hf != null)
            {
                String hfLabel = hf.ToString();
                if (currenthf != null)
                {
                    String currentHfLabel = currenthf.ToString();
                    if (!hfLabel.Equals(currentHfLabel))
                    {
                        f["HF"] = hfLabel;
                        featureUpdated = true;
                    }
                }
                else
                {
                    f["HF"] = hfLabel;
                    featureUpdated = true;
                }

            }
            else
            {
                if (currenthf != null)
                {
                    f["HF"] = null;
                }
            }

            if (labelobj != null)
            {
                String label = labelobj.ToString();
                if (currentLabel != null)
                {
                    String currentNameLabel = currentLabel.ToString();
                    if (!label.Equals(currentNameLabel))
                    {
                        f["LABEL"] = label;
                        featureUpdated = true;
                    }
                }
                else
                {
                    f["LABEL"] = label;
                    featureUpdated = true;
                }

            }
            else
            {
                if (currentLabel != null)
                {
                    f["LABEL"] = null;
                }
            }

            if (descriptionobj != null)
            {
                String descLabel = descriptionobj.ToString();
                if (currentDescription != null)
                {
                    String currentNameLabel = currentDescription.ToString();
                    if (!descLabel.Equals(currentNameLabel))
                    {
                        f["DESCRIPTION"] = descLabel;
                        featureUpdated = true;
                    }
                }
                else
                {
                    f["DESCRIPTION"] = descLabel;
                    featureUpdated = true;
                }

            }
            else
            {
                if (currentDescription != null)
                {
                    f["DESCRIPTION"] = null;
                }
            }

            if (sym != null)
            {
                if (currentsym == null)
                {
                    f["ICON"] = sym.Source;
                    featureUpdated = true;
                }
            }
            else
            {
                //_symcat.Search();

            }
            if (featureUpdated)
            {
                String updates_key = key + "_UPDATE";
                _items[updates_key].Add(id, f);
            }
            return featureUpdated;

        }

        public Dictionary<String, Dictionary<String, object>> RetrieveFeatureCache(String key)
        {
            return _items[key];
        }
    }
}
