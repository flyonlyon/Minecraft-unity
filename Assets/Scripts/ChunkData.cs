using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ChunkData {

    public ChunkCoord chunkCoord;
    public GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    // List is almost definately not the most efficient way to do this
    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];
    public WorldData worldData;

    private bool _isActive;
    public bool isVoxelMapPopulated = false;


    // ChunkData constructor
    public ChunkData(ChunkCoord _chunkCoord, WorldData _worldData, bool generateOnLoad) {
        chunkCoord = _chunkCoord;
        worldData = _worldData;
        _isActive = true;

        if (generateOnLoad) Init();
    }

    public void Init() {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.name = "Chunk (" + chunkCoord.x + ", " + chunkCoord.z + ")";
        chunkObject.transform.SetParent(worldData.transform);
        chunkObject.transform.position = new Vector3(chunkCoord.x * VoxelData.chunkWidth, 0f,
                                                     chunkCoord.z * VoxelData.chunkWidth);
        meshRenderer.material = worldData.material;

        PopulateVoxelMap();
        UpdateChunkMesh();
    }

    // PopulateVoxelMap() fills the chunk's voxel map with data
    private void PopulateVoxelMap() {
        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    voxelMap[x, y, z] = worldData.GetVoxel(chunkPosition + new Vector3(x, y, z));
                } 
            }
        }
        isVoxelMapPopulated = true;
    }

    // CreateMeshData() adds all necessary faces to the chunk's mesh
    private void UpdateChunkMesh() {
        ClearMeshData();

        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    if (!worldData.blockTypes[voxelMap[x, y, z]].isSolid) continue;
                    UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }

        CreateMesh();
    }

    private void ClearMeshData() {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    public bool IsActive {
        get { return _isActive; }
        set {
            _isActive = value;
            if (chunkObject != null) chunkObject.SetActive(value);
        }
    }

    public Vector3 chunkPosition {
        get { return chunkObject.transform.position; }
    }

    // IsVoxelInChunk() returns whether the voxel is on the inside of a chunk
    private bool IsVoxelInChunk(int x, int y, int z) {
        return !(x < 0 || x > VoxelData.chunkWidth - 1 ||
                 y < 0 || y > VoxelData.chunkHeight - 1 ||
                 z < 0 || z > VoxelData.chunkWidth - 1);
    }


    public void EditVoxel(Vector3 position, byte newID) {
        int xBlock = Mathf.FloorToInt(position.x);
        int yBlock = Mathf.FloorToInt(position.y);
        int zBlock = Mathf.FloorToInt(position.z);

        xBlock -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zBlock -= Mathf.FloorToInt(chunkObject.transform.position.z);

        voxelMap[xBlock, yBlock, zBlock] = newID;
        UpdateSurroundingVoxels(new Vector3(xBlock, yBlock, zBlock));
        UpdateChunkMesh();
    }

    private void UpdateSurroundingVoxels(Vector3 position) {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        for (int face = 0; face < 6; ++face) {
            Vector3 voxel = position + VoxelData.faceChecks[face];
            if (!IsVoxelInChunk((int)voxel.x, (int)voxel.y, (int)voxel.z)) worldData.GetChunkFromVector3(voxel + chunkPosition).UpdateChunkMesh();
        }
    }

    // CheckVoxel() returns true if there is a solid block at the specified position
    private bool CheckVoxel(Vector3 position) {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!IsVoxelInChunk(x, y, z))
            return worldData.CheckForVoxel(position + chunkPosition);

        return worldData.blockTypes[voxelMap[x, y, z]].isSolid;
    }


    public byte GetVoxelFromGlobalVector3(Vector3 position) {
        int xBlock = Mathf.FloorToInt(position.x);
        int yBlock = Mathf.FloorToInt(position.y);
        int zBlock = Mathf.FloorToInt(position.z);

        xBlock -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zBlock -= Mathf.FloorToInt(chunkObject.transform.position.z);

        return voxelMap[xBlock, yBlock, zBlock];
    }

    // AddVoxelDataToChunk() adds the specified voxel's data to the chunk's mesh
    private void UpdateMeshData(Vector3 position) {

        for (int face = 0; face < 6; ++face) {

            // If there is a block next to this one, don't draw the face
            if (CheckVoxel(position + VoxelData.faceChecks[face])) continue;

            byte blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];

            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 0]]);
            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 1]]);
            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 2]]);
            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 3]]);

            AddTexture(worldData.blockTypes[blockID].GetTextureID(face));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);
            vertexIndex += 4;
        }
    }

    // CreateMesh() creates a mesh based on the chunk's vertices, triagles, and uvs
    private void CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    // AddTexture() adds the texture with the specified ID to the chunk's uvs
    private void AddTexture(int textureID) {

        float y = textureID / VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);

        y *= VoxelData.normalizedBlockTextureSize;
        x *= VoxelData.normalizedBlockTextureSize;

        y = 1f - y - VoxelData.normalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.normalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize,
                            y + VoxelData.normalizedBlockTextureSize));
    }
}

public class ChunkCoord {
    public int x;
    public int z;

    public ChunkCoord(Vector3 position) {
        x = Mathf.FloorToInt(position.x) / VoxelData.chunkWidth;
        z = Mathf.FloorToInt(position.z) / VoxelData.chunkWidth;
    }

    public ChunkCoord(int _x = 0, int _z = 0) {
        x = _x;
        z = _z;
    }

    public bool Equals(ChunkCoord other) {
        if (other == null) return false;
        if (other.x == x && other.z == z) return true;
        return false;
    }
}