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
    private List<ChunkData> chunksToUpdate = new List<ChunkData>();

    bool applyingModifications = false;
    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

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

        if (modifications.Count > 0 && !applyingModifications) StartCoroutine(ApplyModifications());
        if (chunksToCreate.Count > 0) CreateChunk();
        if (chunksToUpdate.Count > 0) UpdateChunks();

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

        while (modifications.Count > 0) {
            VoxelMod currMod = modifications.Dequeue();
            ChunkCoord currCoord = GetChunkCoordFromVector3(currMod.position);

            if (chunks[currCoord.x, currCoord.z] == null) {
                chunks[currCoord.x, currCoord.z] = new ChunkData(currCoord, this, true);
                activeChunks.Add(currCoord);
            }

            chunks[currCoord.x, currCoord.z].modifications.Enqueue(currMod);
            if (!chunksToUpdate.Contains(chunks[currCoord.x, currCoord.z]))
                chunksToUpdate.Add(chunks[currCoord.x, currCoord.z]);
        }

        for (int i = 0; i < chunksToUpdate.Count; ++i) {
            chunksToUpdate[0].UpdateChunkMesh();
            chunksToUpdate.RemoveAt(0);
        }

        player.position = spawnPosition;
    }

    void CreateChunk() {
        ChunkCoord coord = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        activeChunks.Add(coord);
        chunks[coord.x, coord.z].Init();
    }

    void UpdateChunks() {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToCreate.Count - 1) {
            if (chunksToUpdate[index].isVoxelMapPopulated) {
                chunksToUpdate[index].UpdateChunkMesh();
                chunksToUpdate.RemoveAt(index);
                updated = true;
            }

            ++index;
        }
    }

    IEnumerator ApplyModifications() {
        applyingModifications = true;
        int count = 0;

        while (modifications.Count > 0) {
            VoxelMod currMod = modifications.Dequeue();
            ChunkCoord currCoord = GetChunkCoordFromVector3(currMod.position);

            if (chunks[currCoord.x, currCoord.z] == null) {
                chunks[currCoord.x, currCoord.z] = new ChunkData(currCoord, this, true);
                activeChunks.Add(currCoord);
            }

            chunks[currCoord.x, currCoord.z].modifications.Enqueue(currMod);

            if (!chunksToUpdate.Contains(chunks[currCoord.x, currCoord.z]))
                chunksToUpdate.Add(chunks[currCoord.x, currCoord.z]);

            ++count;
            if (count > 300) {
                count = 0;
                yield return null;
            }
        }

        applyingModifications = false;
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
        else if (y < terrainHeight) voxelValue = 3; // Dirt
        else if (y == terrainHeight) voxelValue = 2; // Grass

        // Second terrain pass
        if (voxelValue == 4)
            foreach (Lode lode in biome.lodes)
                if (lode.minHeight < y && y < lode.maxHeight)
                    if (Noise.Get3DPerlin(new Vector3(x, y, z), lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;

        // Tree pass
        if (y == terrainHeight)
            if (Noise.Get2DPerlin(new Vector2(x, z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
                if (Noise.Get2DPerlin(new Vector2(x, z), 100, biome.treePlacementScale) > biome.treePlacementThreshold)
                    Structure.MakeTree(position, modifications, biome.minTreeSize, biome.maxTreeSize);
            
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

public class VoxelMod {

    public Vector3 position;
    public byte id;

    public VoxelMod (Vector3 _position, byte _id) {
        position = _position;
        id = _id;
    }

}