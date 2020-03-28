
using System;
using System.Collections.Generic;
using System.Reflection;
using Contensive.BaseClasses;

namespace aoEmailBounceProcess {
    namespace Models.Domain {
        public abstract class BaseDomainModel {
            // 
            // ====================================================================================================
            /// <summary>
            /// open an existing object
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="cp"></param>
            /// <param name="cs"></param>
            /// <returns></returns>
            private static T loadRecord<T>(CPBaseClass cp, CPCSBaseClass cs) where T : BaseDomainModel {
                T instance = null;
                try {
                    if (cs.OK()) {
                        Type instanceType = typeof(T);
                        instance = (T)Activator.CreateInstance(instanceType);
                        foreach (PropertyInfo resultProperty in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                            switch (resultProperty.Name.ToLower()) {
                                case "specialcasefield": {
                                        break;
                                    }

                                case "sortorder": {
                                        // 
                                        // -- customization for pc, could have been in default property, db default, etc.
                                        string sortOrder = cs.GetText(resultProperty.Name);
                                        if ((string.IsNullOrEmpty(sortOrder)))
                                            sortOrder = "9999";
                                        resultProperty.SetValue(instance, sortOrder, null);
                                        break;
                                    }

                                default: {
                                        switch (resultProperty.PropertyType.Name) {
                                            case "Int32": {
                                                    resultProperty.SetValue(instance, cs.GetInteger(resultProperty.Name), null);
                                                    break;
                                                }

                                            case "Boolean": {
                                                    resultProperty.SetValue(instance, cs.GetBoolean(resultProperty.Name), null);
                                                    break;
                                                }

                                            case "DateTime": {
                                                    resultProperty.SetValue(instance, cs.GetDate(resultProperty.Name), null);
                                                    break;
                                                }

                                            case "Double": {
                                                    resultProperty.SetValue(instance, cs.GetNumber(resultProperty.Name), null);
                                                    break;
                                                }

                                            default: {
                                                    resultProperty.SetValue(instance, cs.GetText(resultProperty.Name), null);
                                                    break;
                                                }
                                        }

                                        break;
                                    }
                            }
                        }
                    }
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex);
                    throw;
                }
                return instance;
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// pattern get a list of objects from this model
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="cp"></param>
            /// <param name="sql"></param>
            /// <param name="pageSize"></param>
            /// <param name="pageNumber"></param>
            /// <returns></returns>
            protected static List<T> createListFromSql<T>(CPBaseClass cp, string sql, int pageSize, int pageNumber) where T : BaseDomainModel {
                try {
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        List<T> result = new List<T>();
                        if ((cs.OpenSQL(sql, "", pageSize, pageNumber))) {
                            do {
                                T instance = loadRecord<T>(cp, cs);
                                if ((instance != null)) { result.Add(instance); }
                                cs.GoNext();
                            } while (cs.OK());
                        }
                        cs.Close();
                        return result;
                    }
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex);
                    throw;
                }
            }
        }
    }
}
