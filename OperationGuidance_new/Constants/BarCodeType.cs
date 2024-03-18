namespace OperationGuidance_new.Constants {
    public static class BarCodeTypes {
        public static List<BarCodeType> Elements = new();
        private static BarCodeType AddNew(int id, string name) {
            BarCodeType type = new() {
                Id = id,
                Name = name,
            };
            Elements.Add(type);
            return type;
        }

        public static BarCodeType PRODUCT { get; } = AddNew(1, "总成码/追溯码");
        public static BarCodeType PARTS { get; } = AddNew(2, "物料码");

        public static BarCodeType GetById(int id) {
            foreach (BarCodeType type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of bar code by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (BarCodeType type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of bar code by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (BarCodeType type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of bar code by type_name = {name}");
        }
    }

    public class BarCodeType {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
