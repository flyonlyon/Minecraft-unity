using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class World : MonoBehaviour {

    public BlockType[] blockTypes;
    public Material worldMaterial;

    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();


    private void Awake() {
        blockTypes = Loader.LoadBlockTypes();
    }

    private void Start() {
        LoadWorld();
        DrawWorld();
        //SpawnPlayer();
    }

    private void LoadWorld() {
        for (int x = 0; x < VoxelData.worldSizeInChunks; ++x) {
            for (int z = 0; z < VoxelData.worldSizeInChunks; ++z) {
                for (int y = 0; y < VoxelData.worldHeightInChunks; ++y) {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    chunks[chunkCoord] = new Chunk(chunkCoord, this);
                }
            }
        }
    }

    private void DrawWorld() {
        for (int x = 0; x < VoxelData.worldSizeInChunks; ++x) {
            for (int z = 0; z < VoxelData.worldSizeInChunks; ++z) {
                for (int y = 0; y < VoxelData.worldHeightInChunks; ++y) {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    chunks[chunkCoord].DrawChunk();
                }
            }
        }
    }

    public ushort GetBlockType(Vector3Int voxelPos) {
        if (VoxelNotInWorld(voxelPos)) return 0;
        Vector3Int chunkCoord = GetChunkCoordFromVoxelPos(voxelPos);
        return chunks[chunkCoord].GetBlockType(voxelPos - (chunkCoord * VoxelData.chunkSize));
    }

    Vector3Int GetChunkCoordFromVoxelPos(Vector3Int voxelPos) {
        return new Vector3Int(voxelPos.x / 32, voxelPos.y / 32, voxelPos.z / 32);
    }

    bool VoxelNotInWorld(Vector3Int voxelPos) {
        return voxelPos.x < 0 || VoxelData.worldSizeInVoxels - 1 < voxelPos.x ||
               voxelPos.y < 0 || VoxelData.worldHeightInVoxels - 1 < voxelPos.y ||
               voxelPos.z < 0 || VoxelData.worldSizeInVoxels - 1 < voxelPos.z;
    }

    private void SpawnPlayer() {
        // player.transform.position = new Vector3(VoxelData.worldSizeInVoxels / 2, VoxelData.worldHeightInVoxels, VoxelData.worldSizeInVoxels);
    }
}
