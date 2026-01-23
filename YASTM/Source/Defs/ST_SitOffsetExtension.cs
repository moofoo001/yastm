using UnityEngine;
using Verse;

namespace YASTM
{
    public class ST_SitOffsetExtension : DefModExtension
    {
        public Vector3 offsetNorth = Vector3.zero;
        public Vector3 offsetSouth = Vector3.zero;
        public Vector3 offsetEast = Vector3.zero;
        public Vector3 offsetWest = Vector3.zero;
        
        // NEU: Statt nur Ja/Nein, geben wir die exakte Tiefe an!
        // Standard -0.05f reicht meistens, um hinter dem Objekt zu sein.
        public float northLayerAdjust = 0f; 
    }
}