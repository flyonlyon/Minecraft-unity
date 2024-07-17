using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData {

    public const int chunkSize = 32;

    public const int worldSizeInChunks = 4;
    public const int worldHeightInChunks = 2;
    public const int worldSizeInVoxels = worldSizeInChunks * chunkSize;
    public const int worldHeightInVoxels = worldHeightInChunks * chunkSize;

    public const int worldCenterInChunks = worldSizeInChunks / 2;
    public const int worldCenterInVoxels = worldSizeInVoxels / 2;

    public const int textureAtlasSizeInBlocks = 16;
    public const float normalizedBlockTextureSize = 1.0f / textureAtlasSizeInBlocks;


    public static readonly Vector3Int[] voxelVertices = new Vector3Int[8]{
        // Right/Left, Top/Bottom, Front/Back
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1),
    };

    public static readonly int[,] voxelTriangles = new int[,] {
        // Order of vertices: 0 1 2 2 1 3
        {3, 7, 2, 6}, // Top
        {5, 6, 4, 7}, // Front
        {1, 2, 5, 6}, // Right
        {0, 3, 1, 2}, // Back
        {4, 7, 0, 3}, // Left
        {1, 5, 0, 4}, // Bottom
    };

    public static readonly Vector2Int[] voxelUVs = new Vector2Int[4] {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
    };

    public static readonly Vector3Int[] faceChecks = new Vector3Int[6]{
        new Vector3Int(0, 1, 0), // Top
        new Vector3Int(0, 0, 1), // Front
        new Vector3Int(1, 0, 0), // Right
        new Vector3Int(0, 0, -1), // Back
        new Vector3Int(-1, 0, 0), // Left
        new Vector3Int(0, -1, 0) // Bottom
    };
}
