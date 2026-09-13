using System;

namespace PQS.Mods
{
    public enum NoiseType
    {
        OpenSimplex2, OpenSimplex2S, Cellular, Perlin, ValueCubic, Value
    }

    public enum FractalType
    {
        None, Fbm, Ridged, PingPong
    }

    public enum CellularDistanceFunction
    {
        Euclidean, EuclideanSq, Manhattan, Hybrid
    }

    public enum CellularReturnType
    {
        CellValue, Distance, Distance2, Distance2Add, Distance2Sub, Distance2Mul, Distance2Div
    }
    
    [Serializable]
    public struct NoiseMod
    {
        public float Height;

        public NoiseType NoiseType;
        public int NoiseSeed;
        public float NoiseFrequency;

        public FractalType FractalType;
        public int FractalOctaves;
        public float FractalLacunarity;
        public float FractalGain;
        public float FractalWeightedStrength;
        public float FractalPingPongStrength;        
        
        public CellularDistanceFunction CellularDistanceFunction;
        public CellularReturnType CellularReturnType;
        public float CellularJitter;
    }
}