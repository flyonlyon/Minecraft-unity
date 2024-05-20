using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class VoxelData {

    public static readonly int chunkWidth = 16;
    public static readonly int chunkHeight = 128;

    public static readonly int worldSizeInChunks = 16;
    public static int worldSizeInVoxels { get { return worldSizeInChunks * chunkWidth; } }

    public static readonly int viewDistance = 8;

    public static readonly int textureAtlasSizeInBlocks = 16;
    public static float normalizedBlockTextureSize { get { return 1f / (float)textureAtlasSizeInBlocks; } }


    public static readonly Vector3[] VoxelVertices = new Vector3[8] {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f)
    };

    public static readonly Vector3[] faceChecks = new Vector3[6] {
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
    };

    public static readonly int[,] VoxelTriangles = new int[6, 4] {
        {3, 7, 2, 6}, // Top Face
        {5, 6, 4, 7}, // Front Face
        {1, 2, 5, 6}, // Right Face
        {0, 3, 1, 2}, // Back Face
        {4, 7, 0, 3}, // Left Face
        {1, 5, 0, 4}, // Bottom Face
    };

    public static readonly Vector2[] voxelUVs = new Vector2[4] {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f),
    };
}
