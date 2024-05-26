using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WorldData : MonoBehaviour {

    public int seed;
    public BiomeData biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    private ChunkData[,] chunks = new ChunkData[VoxelData.worldSizeInChunks,
                                               VoxelData.worldSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();             // REMOVE DESPAWNED CHUNKS FROM ACTIVECHUNKS !!!!!
    public ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;

    private List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    private bool isCreatingChunks = false;

    public GameObject debugScreen;


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
        if (!playerChunkCoord.Equals(playerLastChunkCoord)) CheckViewDistance();
        if (chunksToCreate.Count > 0 && !isCreatingChunks) StartCoroutine(CreateChunks());

        if (Input.GetKeyDown(KeyCode.F3)) debugScreen.SetActive(!debugScreen.activeSelf);
    }


    // GenerateWorld() creates all world chunks
    private void GenerateWorld() {
        for (int x = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistance;
                      x < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistance; ++x) {
            for (int z = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistance;
                          z < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistance; ++z){
                chunks[x, z] = new ChunkData(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        player.position = spawnPosition;
    }

    IEnumerator CreateChunks() {
        isCreatingChunks = true;

        while(chunksToCreate.Count > 0) {
            chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;
        }

        isCreatingChunks = false;
    }

    // GetChunkCoordFromVector3() gets the coordinate of the chunk the given position is in
    private ChunkCoord GetChunkCoordFromVector3(Vector3 position) {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return new ChunkCoord(x, z);
    }

    public ChunkData GetChunkFromVector3(Vector3 position) {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return chunks[x, z];
    }

    // CheckViewDistance() creates chunks within the view distance that haven't been created
    public void CheckViewDistance() {
        ChunkCoord chunkCoord = GetChunkCoordFromVector3(player.position);
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = chunkCoord.x - VoxelData.viewDistance; x < chunkCoord.x + VoxelData.viewDistance; ++x) {
            for (int z = chunkCoord.z - VoxelData.viewDistance; z < chunkCoord.z + VoxelData.viewDistance; ++z) {

                if (!IsChunkInWorld(new ChunkCoord(x, z))) continue;

                if (chunks[x, z] == null) {
                    chunks[x, z] = new ChunkData(new ChunkCoord(x, z), this, false);
                    chunksToCreate.Add(new ChunkCoord(x, z));
                } else if (!chunks[x, z].IsActive) chunks[x, z].IsActive = true;

                activeChunks.Add(new ChunkCoord(x, z));

                for (int idx = 0; idx < previouslyActiveChunks.Count; ++idx)
                    if (previouslyActiveChunks[idx].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(idx);
            }
        }

        foreach (ChunkCoord deadChunk in previouslyActiveChunks)
            chunks[deadChunk.x, deadChunk.z].IsActive = false;

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private int GetTerrainHeight(float x, float z) {
        return Mathf.FloorToInt(biome.solidGroundHeight + biome.terrainHeight * Noise.Get2DPerlin(new Vector2(x, z), 0, biome.terrainScale));
    }

    // CheckForVoxel returns whether the voxel at the given global coordinates is solid
    public bool CheckForVoxel(Vector3 position) {
        ChunkCoord thisChunk = new ChunkCoord(position);

        // This line could maybe replace "if (!IsChunkInWorld..."
        // if (!IsVoxelInWorld(position)) return false;

        if (!IsChunkInWorld(thisChunk) || position.y < 0 || position.y > VoxelData.chunkHeight) return false;
        if (chunks[thisChunk.x, thisChunk.z] != null &&
            chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(position)].isSolid;
        return blockTypes[GetVoxel(position)].isSolid;
    }

    public bool CheckIfTransparent(Vector3 position) {
        ChunkCoord thisChunk = new ChunkCoord(position);

        // This line could maybe replace "if (!IsChunkInWorld..."
        // if (!IsVoxelInWorld(position)) return false;

        if (!IsChunkInWorld(thisChunk) || position.y < 0 || position.y > VoxelData.chunkHeight) return false;
        if (chunks[thisChunk.x, thisChunk.z] != null &&
            chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(position)].isTransparent;
        return blockTypes[GetVoxel(position)].isTransparent;
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
    public Sprite blockIcon;

    public bool isSolid;
    public bool isTransparent;
    

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
