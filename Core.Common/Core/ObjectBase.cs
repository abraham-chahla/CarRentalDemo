﻿using Core.Common.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Core.Common.Extensions;

namespace Core.Common.Core
{
    public class ObjectBase : INotifyPropertyChanged
    {
        private event PropertyChangedEventHandler _PropertyChanged;
        List<PropertyChangedEventHandler> _PropertyChangedSubscribers = new List<PropertyChangedEventHandler>();

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (!_PropertyChangedSubscribers.Contains(value))
                {
                    _PropertyChanged += value;
                    _PropertyChangedSubscribers.Add(value);
                }
            }

            remove
            {
                _PropertyChanged -= value;
                _PropertyChangedSubscribers.Remove(value);
            }
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            string propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
            OnPropertyChanged(propertyName);
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName, true);
        }
        protected virtual void OnPropertyChanged(string propertyName, bool makeDirty)
        {
            if (_PropertyChanged != null)
            {
                _PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            if (makeDirty)
            {
                _IsDirty = true;
            }
        }

        bool _IsDirty;
        [NotNavigable]
        public bool IsDirty
        {
            get
            {
                return _IsDirty;
            }
            set
            {
                _IsDirty = value;
            }
        }

        public List<ObjectBase> GetDirtyObjects()
        {
            List<ObjectBase> dirtyObjects = new List<ObjectBase>();
            WalObjectGraph(o =>
            {
                if (o.IsDirty)
                {
                    dirtyObjects.Add(o);
                }
                return false;
            }, coll => { });

            return dirtyObjects;
        }


        public void CleanAll()
        {

            WalObjectGraph(o =>
            {
                if (o.IsDirty)
                {
                    o.IsDirty = false;
                }
                return false;
            }, coll => { });
        }

        public virtual bool IsAnythingDirty()
        {
            bool IsDirty = false;

            WalObjectGraph(o =>
            {
                if (o.IsDirty)
                {
                    IsDirty = true;
                    return true;
                }
                else
                {
                    return false;

                }
            }, coll => { });
            return IsDirty;
        }



        protected void WalObjectGraph(Func<ObjectBase, bool> snippetForObject, Action<IList> snippetForCollection, params string[] exemptProperties)
        {
            List<ObjectBase> visited = new List<ObjectBase>();
            Action<ObjectBase> walk = null;
            List<string> exemptions = new List<string>();
            if (exemptProperties != null)
            {
                exemptions = exemptProperties.ToList();
            }

            walk = (o) =>
            {
                if (o != null && !visited.Contains(o))
                {
                    visited.Add(o);

                    bool exitWalk = snippetForObject.Invoke(o);
                    if (!exitWalk)
                    {
                        PropertyInfo[] properties = o.GetBrowsableProperties();
                        foreach (var property in properties)
                        {
                            if (!exemptions.Contains(property.Name))
                            {
                                if (property.PropertyType.IsSubclassOf(typeof(ObjectBase)))
                                {
                                    ObjectBase obj = (ObjectBase)(property.GetValue(o, null));
                                    walk(obj);
                                }
                                else
                                {
                                    IList coll = property.GetValue(o, null) as IList;
                                    if (coll != null)
                                    {
                                        snippetForCollection.Invoke(coll);
                                        foreach (var item in coll)
                                        {
                                            if (item is ObjectBase)
                                            {
                                                walk((ObjectBase)item);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            };
            walk(this);

        }

    }
}