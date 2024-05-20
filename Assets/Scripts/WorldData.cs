using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WorldData : MonoBehaviour {

    public int seed;
    public BiomeData biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public BlockType[] blockTypes;

    private ChunkData[,] chunks = new ChunkData[VoxelData.worldSizeInChunks,
                                               VoxelData.worldSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();             // REMOVE DESPAWNED CHUNKS FROM ACTIVECHUNKS !!!!!
    private ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;


    private void Start() {
        Random.InitState(seed);

        spawnPosition = new Vector3(VoxelData.worldSizeInChunks * VoxelData.chunkWidth / 2f,
                                    GetTerrainHeight(VoxelData.worldSizeInChunks * VoxelData.chunkWidth / 2f,
                                                     VoxelData.worldSizeInChunks * VoxelData.chunkWidth / 2f) + 2.5f,
                                    VoxelData.worldSizeInChunks * VoxelData.chunkWidth / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private void Update() {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        if (!playerChunkCoord.Equals(playerLastChunkCoord)) {
            playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
            CheckViewDistance();
        }
    }


    // GenerateWorld() creates all world chunks
    private void GenerateWorld() {
        for (int x = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistance;
                      x < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistance; ++x) {
            for (int z = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistance;
                          z < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistance; ++z){
                CreateNewChunk(x, z);
            }
        }

        player.position = spawnPosition;
    }

    // GetChunkCoordFromVector3() gets the coordinate of the chunk the given position is in
    private ChunkCoord GetChunkCoordFromVector3(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return new ChunkCoord(x, z);
    }

    // CheckViewDistance() creates chunks within the view distance that haven't been created
    public void CheckViewDistance() {
        ChunkCoord chunkCoord = GetChunkCoordFromVector3(player.position);
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = chunkCoord.x - VoxelData.viewDistance; x < chunkCoord.x + VoxelData.viewDistance; ++x) {
            for (int z = chunkCoord.z - VoxelData.viewDistance; z < chunkCoord.z + VoxelData.viewDistance; ++z) {

                if (!IsChunkInWorld(new ChunkCoord(x, z))) continue;

                if (chunks[x, z] == null) CreateNewChunk(x, z);
                else if (!chunks[x, z].IsActive) {
                    chunks[x, z].IsActive = true;
                    activeChunks.Add(new ChunkCoord(x, z));
                } 

                for (int idx = 0; idx < previouslyActiveChunks.Count; ++idx) {
                    if (previouslyActiveChunks[idx].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(idx);
                }
            }
        }

        foreach (ChunkCoord deadChunk in previouslyActiveChunks) {
            chunks[deadChunk.x, deadChunk.z].IsActive = false;
        }
    }

    private int GetTerrainHeight(float x, float z) {
        return Mathf.FloorToInt(biome.solidGroundHeight + biome.terrainHeight * Noise.Get2DPerlin(new Vector2(x, z), 0, biome.terrainScale));
    }

    // GetVoxel() returns the block ID based on its position in the world
    public byte GetVoxel(Vector3 position) {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);
        byte voxelValue = 0;

        // Immutable pass
        if (!IsVoxelInWorld(position)) return 0; // Air - If out of bounds, 
        if (y == 0) return 1; // Bedrock - If bottom row

        // Basic terrain pass
        int terrainHeight = GetTerrainHeight(x, z);

        if (y < terrainHeight - 4) voxelValue = 4; // Stone
        else if (y < terrainHeight) return 3; // Dirt
        else if (y == terrainHeight) return 2; // Grass

        // Second terrain pass
        if (voxelValue == 4)
            foreach (Lode lode in biome.lodes)
                if (lode.minHeight < y && y < lode.maxHeight)
                    if (Noise.Get3DPerlin(new Vector3(x, y, z), lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;

        return voxelValue;
    }

    // CreateNewChunk() creates a new chunk with the speficied coordinates
    private void CreateNewChunk(int x, int z) {
        chunks[x, z] = new ChunkData(new ChunkCoord(x, z), this);
        activeChunks.Add(new ChunkCoord(x, z));
    }

    // Might need to change based on what "in world" means (not on border vs existing)
    // Not On Border:
    // return (0 < chunkCoord.x && chunkCoord.x < VoxelData.worldSizeInChunks - 1 &&
    //         0 < chunkCoord.z && chunkCoord.z < VoxelData.worldSizeInChunks - 1);
    // Existing
    // return (0 <= chunkCoord.x && chunkCoord.x < VoxelData.worldSizeInChunks &&
    //         0 <= chunkCoord.z && chunkCoord.z < VoxelData.worldSizeInChunks);
    public bool IsChunkInWorld(ChunkCoord chunkCoord) {
        return (0 < chunkCoord.x && chunkCoord.x < VoxelData.worldSizeInChunks - 1 &&
                0 < chunkCoord.z && chunkCoord.z < VoxelData.worldSizeInChunks - 1);
    }
     
    public bool IsVoxelInWorld(Vector3 position)
    {
        return (0 <= position.x && position.x < VoxelData.worldSizeInVoxels &&
                0 <= position.y && position.y < VoxelData.chunkHeight &&
                0 <= position.z && position.z < VoxelData.worldSizeInVoxels);
    }

}

[System.Serializable]
public class BlockType {

    public string blockName;
    public bool isSolid;

    public int topFaceTexture;
    public int frontFaceTexture;
    public int rightFaceTexture;
    public int backFaceTexture;
    public int leftFaceTexture;
    public int bottomFaceTexture;

    public int GetTextureID (int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return topFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return rightFaceTexture;
            case 3:
                return backFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return bottomFaceTexture;
            default:
                Debug.Log("Error in GetTextureID: invalid faceIndex");
                return 0;
        }
    }
}
