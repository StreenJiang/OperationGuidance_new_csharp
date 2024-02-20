using OperationGuidance_service.Attributes;
using System.Drawing.Imaging;
using System.Reflection;

namespace OperationGuidance_service.Utils {
    public static class CommonUtils {
        public static Point PointStringToPoint(string? pointString) {
            if (pointString != null) {
                string[] pointStringArr = pointString.Replace("{X=", "").Replace("Y=", "").Replace("}", "").Split(",");
                return new Point(int.Parse(pointStringArr[0]), int.Parse(pointStringArr[1]));
            }
            return new(0, 0);
        }

        public static string ImageToBase64(Image image) {
            if (image == null) {
                return string.Empty;
            }
            using (MemoryStream meoryStream = new()) {
                image.Save(meoryStream, ImageFormat.Png);
                return Convert.ToBase64String(meoryStream.ToArray());
            }
        }

        public static Image? ImageBase64ToImage(string? imageBase64) {
            if (imageBase64 == null || string.IsNullOrEmpty(imageBase64)) {
                return null;
            }
            byte[] bytes = Convert.FromBase64String(imageBase64);
            using (MemoryStream meoryStream = new(bytes, 0, bytes.Length)) {
                meoryStream.Write(bytes, 0, bytes.Length);
                return Image.FromStream(meoryStream);
            }
        }

        public static string ListToString(List<int> list) {
            return list.Count <= 0 ? "" : string.Join("|", list);
        }

        public static List<int> StringToList(string str) {
            string[] strings = str.Split("|");
            List<int> list = new();
            foreach (string item in strings) {
                list.Add(int.Parse(item));
            }
            return list;
        }

        /// <summary>
        /// 根据对象对应的属性名将值赋予新对象，参数用 object 是想同时支持List和object
        /// </summary>
        /// <param name="objFrom">base对象（使用object就可以传入List对象）</param>
        /// <param name="objTo">新对象（同上）</param>
        public static void ObjectConverter<FROM, TO>(object objFrom, object? objTo) where TO : new() {
            if (objTo == null) {
                objTo = new();
            }
            if (objFrom is ICollection<FROM>) {
                if (objTo is ICollection<TO>) {
                    ICollection<FROM> froms = (ICollection<FROM>) objFrom;
                    ICollection<TO> tos = (ICollection<TO>) objTo;
                    foreach (FROM from in froms) {
                       if (from != null) {
                            TO to = new();
                            ObjectConverter<FROM, TO>(from, to);
                            tos.Add(to);
                        }
                    }
                } else {
                    throw new ArgumentException("If objFrom is List or some ICollection, objTo should be the same type to objFrom. " +
                        "Now objFrom is <" + objFrom.GetType() + "> while objTo is <" + objTo.GetType() + ">");
                }
            } else {
                Type type1 = objFrom.GetType();
                Type type2 = objTo.GetType();
                PropertyInfo[] props1 = type1.GetProperties();
                List<PropertyInfo> props2 = type2.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                Dictionary<string, PropertyInfo> p1Dict = props1.ToDictionary(p => p.Name);
                foreach (PropertyInfo p2 in props2) {
                    if (p1Dict.ContainsKey(p2.Name) && p2.GetType() == p1Dict[p2.Name].GetType()) {
                        p2.SetValue(objTo, p1Dict[p2.Name].GetValue(objFrom));
                    } else {
                        IEnumerable<Attribute> enumerable = p2.GetCustomAttributes();
                        foreach (Attribute a in enumerable) {
                            if (a is ConvertPropertyAttribute) {
                                ConvertPropertyAttribute attr = (ConvertPropertyAttribute) a;
                                if (attr.SourceName != null && p1Dict.ContainsKey(attr.SourceName)) {
                                    p2.SetValue(objTo, p1Dict[attr.SourceName].GetValue(objFrom));
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static T CannotBeNull<T>(T? obj) {
            if (obj == null) {
                throw new NullReferenceException("This obj can not be null, please check out the root cause.");
            }
            return obj;
        }
    }
}
