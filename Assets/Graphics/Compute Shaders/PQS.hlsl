#include "Assets/Plugins/FastNoiseLite.hlsl"

struct NoiseMod
{
    float Height;

    fnl_noise_type NoiseType;
    int NoiseSeed;
    float NoiseFrequency;

    fnl_fractal_type FractalType;
    int FractalOctaves;
    float FractalLacunarity;
    float FractalGain;
    float FractalWeightedStrength;
    float FractalPingPongStrength;

    fnl_cellular_distance_func CellularDistanceFunction;
    fnl_cellular_return_type CellularReturnType;
    float CellularJitter;

    float GetElevation(float3 sphere)
    {
        fnl_state noise = fnlCreateState();

        noise.noise_type = NoiseType;
        noise.seed = NoiseSeed;
        noise.frequency = NoiseFrequency;

        noise.fractal_type = FractalType;
        noise.octaves = FractalOctaves;
        noise.lacunarity = FractalLacunarity;
        noise.gain = FractalGain;
        noise.weighted_strength = FractalWeightedStrength;
        noise.ping_pong_strength = FractalPingPongStrength;

        noise.cellular_distance_func = CellularDistanceFunction;
        noise.cellular_return_type = CellularReturnType;
        noise.cellular_jitter_mod = CellularJitter;

        float elevation = fnlGetNoise3D(noise, sphere.x, sphere.y, sphere.z) * Height;
        return elevation;
    }
};