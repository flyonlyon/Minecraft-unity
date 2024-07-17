using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    ushort[,,] voxelData = new ushort[VoxelData.chunkSize, VoxelData.chunkSize, VoxelData.chunkSize];
    const ushort blockTypeBitShift = 6;
    const ushort blockStateBitMask = 63;

    World world;
    GameObject chunkObject;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    int currentVertex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();


    public Chunk(Vector3Int chunkCoord, World _world) {
        world = _world;

        chunkObject = new GameObject();
        chunkObject.name = "Chunk " + chunkCoord.x + " " + chunkCoord.y + " " + chunkCoord.z;
        chunkObject.transform.SetParent(world.gameObject.transform);
        chunkObject.transform.localPosition = chunkCoord * VoxelData.chunkSize;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = world.worldMaterial;

        PopulateVoxelData();
    }

    public void DrawChunk() {
        GenerateChunkMesh();
        DrawChunkMesh();
    }

    // PopulateVoxelData() fills the voxelData array
    void PopulateVoxelData() {
        for (byte x = 0; x < VoxelData.chunkSize; ++x)
            for (byte y = 0; y < VoxelData.chunkSize; ++y)
                for (byte z = 0; z < VoxelData.chunkSize; ++z)
                    voxelData[x, y, z] = SetVoxelData(1, 0);   
    }

    // GenerateChunkMesh() builds the entire chunk's mesh
    void GenerateChunkMesh() {
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
            if (!world.blockTypes[otherBlockType].isTransparent || thisBlockType == otherBlockType) continue;

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

            AddTexturedUVs(world.blockTypes[thisBlockType].faces[face]);

            currentVertex += 4;
        }
    }

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
    }

    ushort SetVoxelData(ushort blockType, byte blockState) {
        return (ushort)((blockType << 6) + blockState);
    }

    public ushort GetBlockType(Vector3Int voxelPos) {
        if (VoxelNotInChunk(voxelPos))
            return world.GetBlockType(voxelPos + Vector3Int.FloorToInt(chunkObject.transform.localPosition));
        return (ushort)(voxelData[voxelPos.x, voxelPos.y, voxelPos.z] >> blockTypeBitShift);
    }

    ushort GetBlockState(Vector3Int voxelPos) {
        if (VoxelNotInChunk(voxelPos)) return 0;
        return (ushort)(voxelData[voxelPos.x, voxelPos.y, voxelPos.z] & blockStateBitMask);
    }

    bool VoxelNotInChunk(Vector3Int voxelPos) {
        return voxelPos.x < 0 || VoxelData.chunkSize - 1 < voxelPos.x ||
               voxelPos.y < 0 || VoxelData.chunkSize - 1 < voxelPos.y ||
               voxelPos.z < 0 || VoxelData.chunkSize - 1 < voxelPos.z;
    }

}


