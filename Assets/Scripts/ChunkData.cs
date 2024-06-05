using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkData {

    public ChunkCoord chunkCoord;
    public GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2]; 
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 chunkPosition;

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    public WorldData worldData;

    private bool _isActive;
    private bool isVoxelMapPopulated = false;


    // ChunkData constructor
    public ChunkData(ChunkCoord _chunkCoord, WorldData _worldData) {
        chunkCoord = _chunkCoord;
        worldData = _worldData;
    }

    public void Init() {

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.name = "Chunk (" + chunkCoord.x + ", " + chunkCoord.z + ")";
        chunkObject.transform.SetParent(worldData.transform);
        chunkObject.transform.position = new Vector3(chunkCoord.x * VoxelData.chunkWidth, 0f, chunkCoord.z * VoxelData.chunkWidth);
        chunkPosition = chunkObject.transform.position;

        materials[0] = worldData.material;
        materials[1] = worldData.transparentMaterial;
        meshRenderer.materials = materials;

        PopulateVoxelMap();

    }

    // PopulateVoxelMap() fills the chunk's voxel map with data
    private void PopulateVoxelMap() {

        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    voxelMap[x, y, z] = new VoxelState(worldData.GetVoxel(chunkPosition + new Vector3(x, y, z)));

                } 
            }
        }

        isVoxelMapPopulated = true;
        lock(worldData.chunkUpdateThreadLock)
            worldData.chunksToUpdate.Add(this);
    }

    // CreateMeshData() adds all necessary faces to the chunk's mesh
    public void UpdateChunkMesh() {

        while (modifications.Count > 0) {
            VoxelMod currMod = modifications.Dequeue();
            Vector3 pos = currMod.position -= chunkPosition;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = currMod.id;
        }

        ClearMeshData();
        CalculateLight();

        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    if (!worldData.blockTypes[voxelMap[x, y, z].id].isSolid) continue;
                    UpdateMeshData(new Vector3(x, y, z));

                }
            }
        }

        worldData.chunksToDraw.Enqueue(this);

    }

    private void CalculateLight() {

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.chunkWidth; ++x) {
            for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                float lightRay = 1f;

                for (int y = VoxelData.chunkHeight - 1; y >= 0; --y) {

                    VoxelState thisVoxel = voxelMap[x, y, z];
                    if (thisVoxel.id > 0 && worldData.blockTypes[thisVoxel.id].transparency < lightRay)
                        lightRay = worldData.blockTypes[thisVoxel.id].transparency;
                    thisVoxel.globalLightPercent = lightRay;

                    voxelMap[x, y, z] = thisVoxel;

                    if (lightRay > VoxelData.lightFalloff) litVoxels.Enqueue(new Vector3Int(x, y, z));

                }
            }
        }

        while (litVoxels.Count > 0) {

            Vector3Int litVoxel = litVoxels.Dequeue();

            for (int face = 0; face < 6; ++face) {
                Vector3 currentVoxel = litVoxel + VoxelData.faceChecks[face];
                Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (!IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z)) continue;

                if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < voxelMap[litVoxel.x, litVoxel.y, litVoxel.z].globalLightPercent - VoxelData.lightFalloff) {

                    voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent =
                        voxelMap[litVoxel.x, litVoxel.y, litVoxel.z].globalLightPercent - VoxelData.lightFalloff;

                    if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                        litVoxels.Enqueue(neighbor);
                }

            }
        }

    }

    private void ClearMeshData() {

        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();

    }

    public bool IsActive {

        get { return _isActive; }
        set {
            _isActive = value;
            if (chunkObject != null) chunkObject.SetActive(value);
        }

    }

    public bool isEditable {
        get {
            if (!isVoxelMapPopulated) return false;
            else return true;
        }
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

        voxelMap[xBlock, yBlock, zBlock].id = newID;

        lock (worldData.chunkUpdateThreadLock) {
            worldData.chunksToUpdate.Insert(0, this);
            UpdateSurroundingVoxels(new Vector3(xBlock, yBlock, zBlock));
        }

    }

    private void UpdateSurroundingVoxels(Vector3 position) {

        for (int face = 0; face < 6; ++face) {
            Vector3 voxel = position + VoxelData.faceChecks[face];
            if (!IsVoxelInChunk((int)voxel.x, (int)voxel.y, (int)voxel.z))
                worldData.chunksToUpdate.Insert(0, worldData.GetChunkFromVector3(voxel + chunkPosition));
        }

    }

    // CheckVoxel() returns true if there is a solid block at the specified position
    private VoxelState CheckVoxel(Vector3 position) {

        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if (!IsVoxelInChunk(x, y, z))
            return worldData.GetVoxelState(position + chunkPosition);

        return voxelMap[x, y, z];

    }


    public VoxelState GetVoxelFromGlobalVector3(Vector3 position) {

        int xBlock = Mathf.FloorToInt(position.x);
        int yBlock = Mathf.FloorToInt(position.y);
        int zBlock = Mathf.FloorToInt(position.z);

        xBlock -= Mathf.FloorToInt(chunkPosition.x);
        zBlock -= Mathf.FloorToInt(chunkPosition.z);

        return voxelMap[xBlock, yBlock, zBlock];
    }

    // AddVoxelDataToChunk() adds the specified voxel's data to the chunk's mesh
    private void UpdateMeshData(Vector3 position) {

        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        byte blockID = voxelMap[x, y, z].id;
        // bool isTransparent = worldData.blockTypes[blockID].renderNeighbors;

        for (int face = 0; face < 6; ++face) {

            VoxelState neighbor = CheckVoxel(position + VoxelData.faceChecks[face]);

            if (neighbor == null || !worldData.blockTypes[neighbor.id].renderNeighbors) continue;

            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 0]]);
            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 1]]);
            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 2]]);
            vertices.Add(position + VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, 3]]);

            for (int idx = 0; idx < 4; ++idx)
                normals.Add(VoxelData.faceChecks[face]);

            AddTexture(worldData.blockTypes[blockID].GetTextureID(face));

            float lightLevel = neighbor.globalLightPercent;

            colors.Add(new Color(0, 0, 0, lightLevel));
            colors.Add(new Color(0, 0, 0, lightLevel));
            colors.Add(new Color(0, 0, 0, lightLevel));
            colors.Add(new Color(0, 0, 0, lightLevel));

            if (!worldData.blockTypes[neighbor.id].renderNeighbors) {
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
            } else {
                transparentTriangles.Add(vertexIndex);
                transparentTriangles.Add(vertexIndex + 1);
                transparentTriangles.Add(vertexIndex + 2);
                transparentTriangles.Add(vertexIndex + 2);
                transparentTriangles.Add(vertexIndex + 1);
                transparentTriangles.Add(vertexIndex + 3);
            }
            vertexIndex += 4;

        }
    }

    // CreateMesh() creates a mesh based on the chunk's vertices, triagles, and uvs
    public void CreateMesh() {

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        // mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

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
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y + VoxelData.normalizedBlockTextureSize));

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

public class VoxelState {
    public byte id;
    public float globalLightPercent;

    public VoxelState() {
        id = 0;
        globalLightPercent = 0f;
    }

    public VoxelState(byte _id) {
        id = _id;
        globalLightPercent = 0f;
    }
}