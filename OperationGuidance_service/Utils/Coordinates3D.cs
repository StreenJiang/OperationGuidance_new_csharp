using Newtonsoft.Json;

namespace OperationGuidance_service.Utils {
        public class Coordinates3D {
            public int X = 0;
            public int Y = 0;
            public int Z = 0;

            public new string ToString() {
                return JsonConvert.SerializeObject(this);
            }

            public static Coordinates3D FromString(string? coordinateStr) {
                Coordinates3D self;
                if (coordinateStr == null) {
                    self = new Coordinates3D();
                } else {
                    Coordinates3D? selfTemp = JsonConvert.DeserializeObject<Coordinates3D>(coordinateStr);
                    if (selfTemp == null) {
                        self = new();
                    } else {
                        self = selfTemp;
                    }
                }
                return self;
            }

            public override bool Equals(object? obj) {
                if (obj != null && obj is Coordinates3D nObj) {
                    if (nObj.X == this.X && nObj.Y == this.Y && nObj.Z == this.Z) {
                        return true;
                    }
                }
                return base.Equals(obj);
            }

            public override int GetHashCode() {
                return base.GetHashCode();
            }
        }
    
}
