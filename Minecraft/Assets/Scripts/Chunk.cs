using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    static ushort blockTypeBitShift = 6;
    static ushort blockStateBitMask = 63;

    ushort[,,] voxelData = new ushort[VoxelData.chunkSize, VoxelData.chunkSize, VoxelData.chunkSize];

    GameObject chunkObject;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    int currentVertex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    Vector3Int position;


    public Chunk(Vector3Int chunkCoord) {
        chunkObject = new GameObject();
        chunkObject.name = "Chunk " + chunkCoord.x + " " + chunkCoord.y + " " + chunkCoord.z;
        chunkObject.transform.SetParent(World.instance.gameObject.transform);
        chunkObject.transform.localPosition = chunkCoord * VoxelData.chunkSize;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = World.instance.worldMaterial;

        position = Vector3Int.FloorToInt(chunkObject.transform.position);

        PopulateVoxelData();
    }

    // ===================================================================== //
    //                           PRIMARY GAME LOOP                           //
    // ===================================================================== //

    // PopulateVoxelData() fills the voxelData array
    public void PopulateVoxelData() {
        for (byte x = 0; x < VoxelData.chunkSize; ++x)
            for (byte y = 0; y < VoxelData.chunkSize; ++y)
                for (byte z = 0; z < VoxelData.chunkSize; ++z)
                    voxelData[x, y, z] = SetVoxelData(1, 0);   
    }

    // UpdateChunk() updates the chunk
    public void UpdateChunk() { }

    // GenerateChunkMesh() builds the entire chunk's mesh
    public void GenerateChunkMesh() {
        for (byte x = 0; x < VoxelData.chunkSize; ++x)
            for (byte y = 0; y < VoxelData.chunkSize; ++y)
                for (byte z = 0; z < VoxelData.chunkSize; ++z)
                    AddVoxelMeshToChunkMesh(new Vector3Int(x, y, z));
    }

    // AddVoxelMeshToChunkMesh() adds the given voxel's mesh to the chunk mesh
    void AddVoxelMeshToChunkMesh(Vector3Int voxelPos) {
        ushort thisBlockType = GetBlockType(voxelPos);

        for (int face = 0; face < 6; ++face) {

            ushort otherBlockType = GetBlockType(voxelPos + VoxelData.faceChecks[face]);
            if (!World.instance.blockTypes[otherBlockType].isTransparent || thisBlockType == otherBlockType) continue;

            vertices.Add(voxelPos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 0]]);
            vertices.Add(voxelPos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 1]]);
            vertices.Add(voxelPos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 2]]);
            vertices.Add(voxelPos + VoxelData.voxelVertices[VoxelData.voxelTriangles[face, 3]]);

            triangles.Add(currentVertex + 0);
            triangles.Add(currentVertex + 1);
            triangles.Add(currentVertex + 2);
            triangles.Add(currentVertex + 2);
            triangles.Add(currentVertex + 1);
            triangles.Add(currentVertex + 3);

            AddTexturedUVs(World.instance.blockTypes[thisBlockType].faces[face]);

            currentVertex += 4;
        }
    }

    // AddTexturedUVs() Adds the texture UVs for the specified textureID
    private void AddTexturedUVs(int textureID) {
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

    // DrawChunk() generates and draws the chunk's mesh
    public void DrawChunk() {
        chunkObject.SetActive(true);
        GenerateChunkMesh();
        DrawChunkMesh();
    }

    // DrawChunkMesh() sets the voxel
    void DrawChunkMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        meshFilter.mesh = mesh;

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        currentVertex = 0;
    }

    // UndrawChunk() resets the activeness and meshFilter of the chunk
    public void UndrawChunk() {
        chunkObject.SetActive(false);
        meshFilter.mesh = null;
    }

    // UnloadChunk() deletes the gameObject associated with the chunk
    public void DeleteChunk() {
        Object.Destroy(chunkObject);
    }

    // ===================================================================== //
    //                           UTILITY FUNCTIONS                           //
    // ===================================================================== //

    // SetVoxelData() returns block type and state into a compressed data format
    ushort SetVoxelData(ushort blockType, byte blockState) {
        return (ushort)((blockType << 6) + blockState);
    }

    // GetBlockType() returns the blockType of the given block if it's in this chunk,
    // or searches worldwide if it's not in this chunk
    public ushort GetBlockType(Vector3Int voxelPos) {
        if (VoxelNotInChunk(voxelPos))
            return World.instance.GetBlockType(voxelPos + position);
        return (ushort)(voxelData[voxelPos.x, voxelPos.y, voxelPos.z] >> blockTypeBitShift);
    }

    // GetBlockType() returns the blockState of the given block if it's in this chunk,
    // or searches worldwide if it's not in this chunk
    ushort GetBlockState(Vector3Int voxelPos) {
        if (VoxelNotInChunk(voxelPos)) return 0;
        return (ushort)(voxelData[voxelPos.x, voxelPos.y, voxelPos.z] & blockStateBitMask);
    }

    // VoxelNotINChunk() returns whether the given voxel is not in the current chunk
    bool VoxelNotInChunk(Vector3Int voxelPos) {
        return voxelPos.x < 0 || VoxelData.chunkSize - 1 < voxelPos.x ||
               voxelPos.y < 0 || VoxelData.chunkSize - 1 < voxelPos.y ||
               voxelPos.z < 0 || VoxelData.chunkSize - 1 < voxelPos.z;
    }
}


