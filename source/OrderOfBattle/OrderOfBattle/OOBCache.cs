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
using System.Windows.Media;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OOB
{
    public class OOBCache
    {
        public OOBCache() { }
        private client.Geometry.PointCollection geoCollection = new client.Geometry.PointCollection();
        private client.Geometry.Envelope _cacheExtent = null;
        public client.Geometry.Envelope CacheExtent
        {
            get
            {
                client.Geometry.MultiPoint mp = new client.Geometry.MultiPoint(geoCollection);
                return mp.Extent;
            }
        }
        private Int32 r = 0;
        private Dictionary<String, Dictionary<String, Dictionary<String, object>>> _items = new Dictionary<String, Dictionary<String, Dictionary<String, object>>>();
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
        private Boolean _refreshSymbols = false;
        public Boolean RefreshSymbols
        {
            get { return _refreshSymbols; }
            set { _refreshSymbols = value; }
        }
        
        public void AddFeatuereContainer(String key)
        {
            Dictionary<String, Dictionary<String, object>> fc = new Dictionary<String, Dictionary<String, object>>();
            _items[key] = fc;
            Dictionary<String, Dictionary<String, object>> ufc = new Dictionary<String, Dictionary<String, object>>();
            String updates_key = key + "_UPDATE";
            _items[updates_key] = ufc;
        }
        public void AddFeature(String key, client.Graphic feature, String baseDesc, String baseLabel, Dictionary<String, String> fields, client.UniqueValueRenderer ur)
        {
            try
            {
                Dictionary<String, Dictionary<String, object>> fList = _items[key];
                Dictionary<String, object> attributes = new Dictionary<String, object>();

                object uid = null, hf = null, label = null, description = null;
                client.Geometry.MapPoint pt = feature.Geometry as client.Geometry.MapPoint;

                uid = feature.Attributes[fields["UID"]];
                hf = feature.Attributes[fields["HF"]];
                label = parseLabel(feature, fields["LABELS"], baseLabel);
                String descFlds = fields["DESCFLDS"];
                String df = fields["DESCFIELD"];
                description = createDescriptionString(feature, baseDesc, df, descFlds);
                //client.FeatureService.Symbols.PictureMarkerSymbol sym = feature.Symbol as client.FeatureService.Symbols.PictureMarkerSymbol;
                client.FeatureService.Symbols.PictureMarkerSymbol sym = ur.GetSymbol(feature) as client.FeatureService.Symbols.PictureMarkerSymbol;
                ImageSource isrc = null;
                
                if (uid != null)
                {
                    if (!fList.ContainsKey(uid.ToString()))
                    {
                        if (pt != null)
                        {
                            String coords = pt.X.ToString() + "," + pt.Y.ToString();
                            attributes["COORDS"] = coords;
                            attributes["POINT"] = pt;
                            geoCollection.Add(pt);
                        }
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
                        _refreshSymbols = true;
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

        private String parseLabel(client.Graphic f, String label, String baseLabel)
        {
            if (String.IsNullOrEmpty(label))
                return null;
            if (String.IsNullOrEmpty(baseLabel))
                return null;
            String[] labelflds = label.Split(',');
            String l = baseLabel;

            for (Int32 i = 0; i < labelflds.Length; ++i)
            {
                
                String fld = labelflds[i];
                String lstring = "{" + fld + "}";
                if (f.Attributes[fld] != null)
                {
                    l = l.Replace(lstring, f.Attributes[fld].ToString());
                }
                else
                {
                    l = l.Replace(lstring, "");
                }
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
        public Boolean UpdateFeature(String key, String id, String baseDesc, String baseLabel, client.Graphic feature, Dictionary<String, String> fields)
        {
            Boolean featureUpdated = false;
            Dictionary<String, Dictionary<String, object>> fList = _items[key];
            
            Dictionary<String, object> f = fList[id];
            client.Geometry.MapPoint pt = feature.Geometry as client.Geometry.MapPoint;

            object hf = feature.Attributes[fields["HF"]];
            object currenthf = f["HF"];
            object labelobj = parseLabel(feature, fields["LABELS"], baseLabel);
            String df = fields["DESCFIELD"];
            String descFlds = fields["DESCFLDS"];
            object descriptionobj = createDescriptionString(feature, baseDesc, df, descFlds);
            object currentLabel = f["LABEL"];
            object currentDescription = f["DESCRIPTION"];
            object currentCoords = f["COORDS"];

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

            if (pt != null)
            {
                String coords = pt.X.ToString() + "," + pt.Y.ToString();
                if (currentCoords != null)
                {
                    if (!coords.Equals(currentCoords.ToString()))
                    {
                        f["COORDS"] = coords;
                        client.Geometry.MapPoint cPoint = f["POINT"] as client.Geometry.MapPoint;
                        geoCollection.Remove(cPoint);
                        geoCollection.Add(pt);
                        f["POINT"] = pt;
                        featureUpdated = true;
                    }
                }
                else
                {
                    f["COORDS"] = coords;
                    f["POINT"] = pt;
                    geoCollection.Add(pt);
                    featureUpdated = true;
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
                _refreshSymbols = true;
            }
            if (featureUpdated)
            {
                String updates_key = key + "_UPDATE";
                if (!_items[updates_key].ContainsKey(id))
                {
                    _items[updates_key].Add(id, f);
                }
                else
                {
                    _items[updates_key][id] =  f;
                }
                if (!_items[key].ContainsKey(id))
                {
                    _items[key].Add(id, f);
                }
                else
                {
                    _items[key][id] = f;
                }
            }
            return featureUpdated;

        }

        public Dictionary<String, Dictionary<String, object>> RetrieveFeatureCache(String key)
        {
            return _items[key];
        }
    }
}
