using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class World : MonoBehaviour {

    public BlockType[] blockTypes;
    public Material worldMaterial;

    List<Vector3Int> activeChunks = new List<Vector3Int>();
    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    public GameObject player;
    public Vector3Int playerChunkCoord;
    public Vector3Int playerLastChunkCoord;


    // ===================================================================== //
    //                         UNITY EVENT FUNCTIONS                         //
    // ===================================================================== //

    void Awake() {
        blockTypes = Loader.LoadBlockTypes();
    }

    void Start() {
        LoadWorld();
        SpawnPlayer();
        CheckViewDistance();
    }

    void Update() {
        CheckViewDistance();
    }

    // ===================================================================== //
    //                           PRIMARY GAME LOOP                           //
    // ===================================================================== //

    void LoadWorld() {
        for (int x = 0; x < VoxelData.worldSizeInChunks; ++x) {
            for (int z = 0; z < VoxelData.worldSizeInChunks; ++z) {
                for (int y = 0; y < VoxelData.worldHeightInChunks; ++y) {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    chunks[chunkCoord] = new Chunk(chunkCoord, this);
                }
            }
        }
    }

    void SpawnPlayer() {
        player.transform.position = new Vector3(VoxelData.worldCenterInVoxels, VoxelData.worldHeightInVoxels + 2, VoxelData.worldCenterInVoxels);
    }

    void CheckViewDistance() {

        playerChunkCoord = GetChunkCoordFromPosition(player.transform.position);
        if (playerChunkCoord == playerLastChunkCoord) return;

        List<Vector3Int> prevActiveChunks = new List<Vector3Int>(activeChunks);
        activeChunks.Clear();

        for (int x = Mathf.Max(playerChunkCoord.x - VoxelData.viewDistance, 0); x < Mathf.Min(playerChunkCoord.x + VoxelData.viewDistance, VoxelData.worldSizeInChunks); ++x) {
            for (int z = Mathf.Max(playerChunkCoord.z - VoxelData.viewDistance, 0); z < Mathf.Min(playerChunkCoord.z + VoxelData.viewDistance, VoxelData.worldSizeInChunks); ++z) {
                for (int y = Mathf.Max(playerChunkCoord.y - VoxelData.viewDistance, 0); y < Mathf.Min(playerChunkCoord.y + VoxelData.viewDistance, VoxelData.worldHeightInChunks); ++y) {

                    Vector3Int chunkCoord = new Vector3Int(x, y, z);

                    activeChunks.Add(chunkCoord);
                    if (prevActiveChunks.Contains(chunkCoord)) prevActiveChunks.Remove(chunkCoord);
                    else chunks[chunkCoord].DrawChunk();

                }
            }
        }

        foreach (Vector3Int chunkCoord in prevActiveChunks)
            chunks[chunkCoord].UndrawChunk();

        playerLastChunkCoord = playerChunkCoord;
    }

    // ===================================================================== //
    //                           UTILITY FUNCTIONS                           //
    // ===================================================================== //

    public ushort GetBlockType(Vector3Int voxelPos) {
        if (VoxelNotInWorld(voxelPos)) return 0;
        Vector3Int chunkCoord = GetChunkCoordFromPosition(voxelPos);
        return chunks[chunkCoord].GetBlockType(voxelPos - (chunkCoord * VoxelData.chunkSize));
    }

    Vector3Int GetChunkCoordFromPosition(Vector3 position) {
        return Vector3Int.FloorToInt(new Vector3(position.x / 32, position.y / 32, position.z / 32));
    }

    bool VoxelNotInWorld(Vector3Int voxelPos) {
        return voxelPos.x < 0 || VoxelData.worldSizeInVoxels - 1 < voxelPos.x ||
               voxelPos.y < 0 || VoxelData.worldHeightInVoxels - 1 < voxelPos.y ||
               voxelPos.z < 0 || VoxelData.worldSizeInVoxels - 1 < voxelPos.z;
    }
}
