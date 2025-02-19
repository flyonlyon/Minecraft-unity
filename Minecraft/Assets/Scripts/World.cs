using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public class World : MonoBehaviour {

    public static World instance;

    [Header("World Parameters")]
    public BlockType[] blockTypes;
    public Material worldMaterial;

    [Header("Multithreading")]
    bool multithreading = true;
    Thread chunkLoadingThread;
    Thread chunkUpdatingThread;
    Thread chunkRenderingThread;
    ConcurrentQueue<Vector3Int> chunksToLoad = new ConcurrentQueue<Vector3Int>();
    ConcurrentQueue<Vector3Int> chunksToUpdate = new ConcurrentQueue<Vector3Int>();
    ConcurrentQueue<Vector3Int> chunksToRender = new ConcurrentQueue<Vector3Int>();
    ConcurrentQueue<Vector3Int> chunksToDraw = new ConcurrentQueue<Vector3Int>();

    [Header("Chunk Data")]
    List<Vector3Int> loadedChunks = new List<Vector3Int>();
    List<Vector3Int> renderedChunks = new List<Vector3Int>();
    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    [Header("Player")]
    public GameObject player;
    public Vector3Int playerChunkCoord;
    public Vector3Int playerLastChunkCoord;


    // ===================================================================== //
    //                             START + STOP                              //
    // ===================================================================== //

    void Awake() { blockTypes = Loader.LoadBlockTypes(); }

    void Start() {

        // Singleton check
        if (instance != null) {
            Destroy(this);
            return;
        }
        instance = this;

        Random.InitState(VoxelData.seed);

        // If multithreading, start all threads
        if (multithreading) {
            chunkLoadingThread = new Thread(new ThreadStart(ChunkLoadingThreadUpdate));
            chunkUpdatingThread = new Thread(new ThreadStart(ChunkUpdatingThreadUpdate));
            chunkRenderingThread = new Thread(new ThreadStart(ChunkRenderingThreadUpdate));

            chunkLoadingThread.Start();
            chunkUpdatingThread.Start();
            chunkRenderingThread.Start();
        }

        SpawnPlayer();
        CheckViewDistance();
    }

    private void OnDisable() {
        chunkLoadingThread.Abort();
        chunkUpdatingThread.Abort();
        chunkRenderingThread.Abort();
    }

    // ===================================================================== //
    //                                UPDATES                                //
    // ===================================================================== //

    void Update(){
        CheckViewDistance();

        // If not multithreading, run an update for all threaded actions
        if (!multithreading) {
            LoadingUpdate();
            UpdatingUpdate();
            RenderingUpdate();
        }

        DrawingUpdate();
    }

    void ChunkLoadingThreadUpdate() { while (true) LoadingUpdate(); }
    void ChunkUpdatingThreadUpdate() { while (true) UpdatingUpdate(); }
    void ChunkRenderingThreadUpdate() { while (true) RenderingUpdate(); }

    // LoadingUpdate() pulls the next chunk from chunksToRender and loads it
    void LoadingUpdate() {
        if (chunksToLoad.Count == 0) return;
        chunksToLoad.TryDequeue(out Vector3Int chunkCoord);
        Chunk chunk = chunks[chunkCoord];

        chunk.PopulateVoxelData();
        
        if (renderedChunks.Contains(chunkCoord))
            chunksToRender.Enqueue(chunkCoord);
    }

    // UpdatingUpdate() pulls the next chunk from chunksToRender and updates it
    void UpdatingUpdate() {
        if (chunksToUpdate.Count == 0) return;
        chunksToUpdate.TryDequeue(out Vector3Int chunkCoord);
        Chunk chunk = chunks[chunkCoord];

        chunk.UpdateChunk();

        if (renderedChunks.Contains(chunkCoord))
            chunksToRender.Enqueue(chunkCoord);
    }

    // RenderingUpdate() pulls the next chunk from chunksToRender and renders it
    void RenderingUpdate() {
        if (chunksToRender.Count == 0) return;

        // Find the next chunk that can be rendered
        chunksToRender.TryDequeue(out Vector3Int chunkCoord);
        while (!chunks[chunkCoord].canRender) {
            chunksToRender.Enqueue(chunkCoord);
            chunksToRender.TryDequeue(out chunkCoord);
        }

        Chunk chunk = chunks[chunkCoord];

        chunk.GenerateChunkMesh();

        chunksToDraw.Enqueue(chunkCoord);
    }

    // DrawingUpdate() pulls the next chunk from chunksToDraw and draws it
    void DrawingUpdate() {
        if (chunksToDraw.Count == 0) return;
        chunksToDraw.TryDequeue(out Vector3Int chunkCoord);
        Chunk chunk = chunks[chunkCoord];

        chunk.DrawChunk();
    }

    // ===================================================================== //
    //                            PRIMARY UTILITY                            //
    // ===================================================================== //

    // SpawnPlayer() sets the player's location
    void SpawnPlayer() {
        player.transform.position = new Vector3(VoxelData.worldCenterInVoxels, Noise.GetTerrainHeight(VoxelData.worldCenterInVoxels, VoxelData.worldCenterInVoxels), VoxelData.worldCenterInVoxels);
    }

    // CheckViewDistance() manages loading and rendering chunks based on their distance from the player
    void CheckViewDistance() {

        playerChunkCoord = GetChunkCoordFromPosition(player.transform.position);
        if (playerChunkCoord == playerLastChunkCoord) return;

        // Load and unload chunks
        List<Vector3Int> prevLoadedChunks = new List<Vector3Int>(loadedChunks);
        loadedChunks.Clear();

        for (int x = playerChunkCoord.x - VoxelData.loadDistance; x < playerChunkCoord.x + VoxelData.loadDistance; ++x) {
            for (int z = playerChunkCoord.z - VoxelData.loadDistance; z < playerChunkCoord.z + VoxelData.loadDistance; ++z) {
                for (int y = playerChunkCoord.y - VoxelData.loadDistance; y < playerChunkCoord.y + VoxelData.loadDistance; ++y) {

                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    if (ChunkNotInWorld(chunkCoord)) continue;
                    loadedChunks.Add(chunkCoord);

                    if (prevLoadedChunks.Contains(chunkCoord)) {
                        prevLoadedChunks.Remove(chunkCoord);
                    } else {
                        CreateChunkGameObject(chunkCoord);
                        chunksToLoad.Enqueue(chunkCoord);
                    }
                }
            }
        }

        foreach (Vector3Int chunkCoord in prevLoadedChunks) {
            chunks[chunkCoord].DeleteChunk();
            chunks.Remove(chunkCoord);
        }

        // Render and unrender chunks
        List<Vector3Int> prevRenderedChunks = new List<Vector3Int>(renderedChunks);
        renderedChunks.Clear();

        for (int x = playerChunkCoord.x - VoxelData.viewDistance; x < playerChunkCoord.x + VoxelData.viewDistance; ++x) {
            for (int z = playerChunkCoord.z - VoxelData.viewDistance; z < playerChunkCoord.z + VoxelData.viewDistance; ++z) {
                for (int y = playerChunkCoord.y - VoxelData.viewDistance; y < playerChunkCoord.y + VoxelData.viewDistance; ++y) {

                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    if (ChunkNotInWorld(chunkCoord)) continue;
                    renderedChunks.Add(chunkCoord);

                    if (prevRenderedChunks.Contains(chunkCoord)) prevRenderedChunks.Remove(chunkCoord);
                    else if (chunks.ContainsKey(chunkCoord)) chunksToRender.Enqueue(chunkCoord);
                }
            }
        }

        foreach (Vector3Int chunkCoord in prevRenderedChunks)
            chunks[chunkCoord].UndrawChunk();

        playerLastChunkCoord = playerChunkCoord;
    }

    // ===================================================================== //
    //                           UTILITY FUNCTIONS                           //
    // ===================================================================== //

    // CreateChunkGameObject() creates the gameObject needed for a Chunk instance
    void CreateChunkGameObject(Vector3Int chunkCoord) {
        Chunk chunk = new Chunk(chunkCoord);
    }

    // AddChunkToChunks() Adds the given chunkCoord and chunk to the chunks dict
    public void AddChunkToChunks(Vector3Int chunkCoord, Chunk chunk) {
        chunks[chunkCoord] = chunk;
    }

    // GetBlockType() returns the block type at the given world position
    public ushort GetBlockType(Vector3Int voxelPos) {
        if (VoxelNotInWorld(voxelPos)) return 0;
        Vector3Int chunkCoord = GetChunkCoordFromPosition(voxelPos);
        return chunks[chunkCoord].GetBlockType(voxelPos - (chunkCoord * VoxelData.chunkSize));
    }

    // GetChunkCoordFromPosition() returns the chunk coord at the given world position
    Vector3Int GetChunkCoordFromPosition(Vector3 position) {
        return Vector3Int.FloorToInt(new Vector3(position.x / 32, position.y / 32, position.z / 32));
    }

    // ChunkNotInWorld() returns whether the given voxel is not in the world
    bool ChunkNotInWorld(Vector3Int chunkPos) {
        return chunkPos.x < 0 || VoxelData.worldSizeInChunks - 1 < chunkPos.x ||
               chunkPos.y < 0 || VoxelData.worldHeightInChunks - 1 < chunkPos.y ||
               chunkPos.z < 0 || VoxelData.worldSizeInChunks - 1 < chunkPos.z;
    }

    // VoxelNotInWorld() returns whether the given voxel is not in the world
    bool VoxelNotInWorld(Vector3Int voxelPos) {
        return voxelPos.x < 0 || VoxelData.worldSizeInVoxels - 1 < voxelPos.x ||
               voxelPos.y < 0 || VoxelData.worldHeightInVoxels - 1 < voxelPos.y ||
               voxelPos.z < 0 || VoxelData.worldSizeInVoxels - 1 < voxelPos.z;
    }

    // UpdateCanRender() updates whether the given chunks (and neighbors) can render
    public void UpdateCanRender(Vector3Int chunkCoord, bool origin = true) {
        if (ChunkNotInWorld(chunkCoord)) return;

        // If all neighboring chunks have been loaded, set canRender to true
        if (ChunkIsLoaded(chunkCoord + VoxelData.faceChecks[0]) &&
            ChunkIsLoaded(chunkCoord + VoxelData.faceChecks[1]) &&
            ChunkIsLoaded(chunkCoord + VoxelData.faceChecks[2]) &&
            ChunkIsLoaded(chunkCoord + VoxelData.faceChecks[3]) &&
            ChunkIsLoaded(chunkCoord + VoxelData.faceChecks[4]) &&
            ChunkIsLoaded(chunkCoord + VoxelData.faceChecks[5]))
            chunks[chunkCoord].canRender = true;

        // Update canRender for surrounding chunks
        if (!origin) return;
        UpdateCanRender(chunkCoord + VoxelData.faceChecks[0], false);
        UpdateCanRender(chunkCoord + VoxelData.faceChecks[1], false);
        UpdateCanRender(chunkCoord + VoxelData.faceChecks[2], false);
        UpdateCanRender(chunkCoord + VoxelData.faceChecks[3], false);
        UpdateCanRender(chunkCoord + VoxelData.faceChecks[4], false);
        UpdateCanRender(chunkCoord + VoxelData.faceChecks[5], false);
    }

    // ChunkIsLoaded() returns if the given chunk has loaded all its data
    public bool ChunkIsLoaded(Vector3Int chunkCoord) {
        if (ChunkNotInWorld(chunkCoord)) return true;
        return chunks.ContainsKey(chunkCoord);
    }
}
