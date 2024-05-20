using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Not the most performance-optimised perlin function, simplex is quicker
public static class Noise {

    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        return Mathf.PerlinNoise((position.x + 0.1f) / VoxelData.chunkWidth * scale + offset,
                                 (position.y + 0.1f) / VoxelData.chunkWidth * scale + offset);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold) {
        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float XY = Mathf.PerlinNoise(x, y);
        float XZ = Mathf.PerlinNoise(x, z);
        float YX = Mathf.PerlinNoise(y, x);
        float YZ = Mathf.PerlinNoise(y, z);
        float ZX = Mathf.PerlinNoise(z, x);
        float ZY = Mathf.PerlinNoise(z, y);

        return (XY + XZ + YX + YZ + ZX + ZY) / 6f > threshold;
    }
}
