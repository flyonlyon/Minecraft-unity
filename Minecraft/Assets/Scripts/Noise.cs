using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise {

    public static float Get2DPerlin(float x, float z, float offset, float scale) {
        x += offset + 0.1f;
        z += offset + 0.1f;
        return Mathf.PerlinNoise(x / VoxelData.chunkSize * scale, z / VoxelData.chunkSize * scale);
    }

    public static int GetTerrainHeight(int x, int z) {
        return 400 + (int)(Get2DPerlin(x, z, 1000, 0.5f) * 100);
    }
}
