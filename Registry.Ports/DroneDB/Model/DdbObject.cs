﻿using System.Collections.Generic;
using GeoJSON.Net;
using Newtonsoft.Json.Linq;

namespace Registry.Ports.DroneDB.Model
{
    public class DdbObject
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public int CreationDate { get; set; }
        public int ModifiedTime { get; set; }
        public string Hash { get; set; }
        public int Depth { get; set; }
        public int Size { get; set; }
        public int Type { get; set; }
        public JObject Meta { get; set; }
        public GeoJSONObject PointGeometry { get; set; }
        public GeoJSONObject PoligonGeometry { get; set; }
    }

}