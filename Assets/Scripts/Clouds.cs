using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour {

    private int cloudHeight = VoxelData.chunkHeight - 10;
    private int cloudScale = 2;

    [SerializeField] private Texture2D cloudPattern;
    [SerializeField] private Material cloudMaterial;
    [SerializeField] private WorldData world;
    bool[,] cloudData;

    int cloudTextureWidth;

    int cloudTileSize;
    Vector3Int offset;

    Dictionary<Vector2Int, GameObject> clouds = new Dictionary<Vector2Int, GameObject>();

    void Start() {

        cloudTextureWidth = cloudPattern.width;
        cloudTileSize = VoxelData.chunkWidth;
        offset = new Vector3Int(-(cloudTextureWidth / 2), 0, -(cloudTextureWidth / 2));
        
        transform.position = new Vector3(VoxelData.worldCenter, cloudHeight, VoxelData.worldCenter);

        LoadCloudData();
        CreateClouds();
        UpdateClouds(); // Gets called twice? Issue with order things start in

    }

    private void LoadCloudData() {

        cloudData = new bool[cloudTextureWidth, cloudTextureWidth];
        Color[] cloudTexture = cloudPattern.GetPixels();

        for (int x = 0; x < cloudTextureWidth; ++x) {
            for (int y = 0; y < cloudTextureWidth; ++y) {
                cloudData[x, y] = cloudTexture[y * cloudTextureWidth + x].a > 0;
            }
        }
    }

    private void CreateClouds() {

        if (world.settings.clouds == CloudStyle.Off) return;

        for (int x = 0; x < cloudTextureWidth; x += cloudTileSize) {
            for (int y = 0; y < cloudTextureWidth; y += cloudTileSize) {

                Mesh cloudMesh;

                if (world.settings.clouds == CloudStyle.Fast)
                    cloudMesh = CreateFastCloudMesh(x, y);
                else
                    cloudMesh = CreateFancyCloudMesh(x, y);

                Vector3 position = new Vector3(x, cloudHeight, y);
                clouds.Add(CloudTilePosFromVector3(position), CreateCloudTile(cloudMesh, position));
                

            }
        }

    }

    public void UpdateClouds() {

        if (world.settings.clouds == CloudStyle.Off) return;

        for (int x = 0; x < cloudTextureWidth; x += cloudTileSize) {
            for (int y = 0; y < cloudTextureWidth; y += cloudTileSize) {

                Vector3 position = world.player.position + offset + (new Vector3(x, 0, y));
                position = new Vector3(RoundToCloud(position.x), cloudHeight, RoundToCloud(position.z));
                Vector2Int cloudPosition = CloudTilePosFromVector3(position);

                clouds[cloudPosition].transform.position = position;

            }
        }
    }

    public int RoundToCloud(float value) {
        return Mathf.FloorToInt(value / cloudTileSize) * cloudTileSize;
    }

    private Mesh CreateFastCloudMesh(int x, int z) {

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; ++xIncrement) {
            for (int zIncrement = 0; zIncrement < cloudTileSize; ++zIncrement) {

                int xVal = x + xIncrement;
                int zVal = z + zIncrement;
                if (!cloudData[xVal, zVal]) continue;

                vertices.Add(new Vector3(xIncrement, 0, zIncrement));
                vertices.Add(new Vector3(xIncrement, 0, zIncrement + 1));
                vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement + 1));
                vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement));

                for (int idx = 0; idx < 4; ++idx)
                    normals.Add(Vector3.down);

                triangles.Add(vertCount + 1);
                triangles.Add(vertCount);
                triangles.Add(vertCount + 2);

                triangles.Add(vertCount + 2);
                triangles.Add(vertCount);
                triangles.Add(vertCount + 3);

                vertCount += 4;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;

    }

    private Mesh CreateFancyCloudMesh(int x, int z) {

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; ++xIncrement) {
            for (int zIncrement = 0; zIncrement < cloudTileSize; ++zIncrement) {

                int xVal = x + xIncrement;
                int zVal = z + zIncrement;
                if (!cloudData[xVal, zVal]) continue;

                for (int face = 0; face < 6; ++face) {

                    if (CheckCloudData(new Vector3Int(xVal, 0, zVal) + VoxelData.faceChecks[face]))
                        continue;

                    for (int idx = 0; idx < 4; ++idx) {

                        Vector3 vertex = new Vector3Int(xIncrement, 0, zIncrement);
                        vertex += VoxelData.VoxelVertices[VoxelData.VoxelTriangles[face, idx]];
                        vertex.y *= cloudScale;
                        vertices.Add(vertex);

                    }

                    for (int idx = 0; idx < 4; ++idx)
                        normals.Add(VoxelData.faceChecks[face]);

                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 1);
                    triangles.Add(vertCount + 2);
                    triangles.Add(vertCount + 2);
                    triangles.Add(vertCount + 1);
                    triangles.Add(vertCount + 3);

                    vertCount += 4;

                }

            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;
    }

    private bool CheckCloudData(Vector3Int point) {

        if (point.y != 0) return false;

        int x = point.x;
        if (x < 0) x = cloudTextureWidth - 1;
        if (x > cloudTextureWidth - 1) x = 0;

        int z = point.z;
        if (z < 0) z = cloudTextureWidth - 1;
        if (z > cloudTextureWidth - 1) z = 0;

        return cloudData[x, z];

    }

    private GameObject CreateCloudTile(Mesh mesh, Vector3 position) {

        GameObject newCloudTile = new GameObject();
        newCloudTile.transform.position = position;
        newCloudTile.transform.parent = transform;
        newCloudTile.name = "Cloud (" + position.x + ", " + position.z + ")";

        MeshRenderer mr = newCloudTile.AddComponent<MeshRenderer>();
        MeshFilter mf = newCloudTile.AddComponent<MeshFilter>();

        mr.material = cloudMaterial;
        mf.mesh = mesh;

        return newCloudTile;

    }

    private Vector2Int CloudTilePosFromVector3(Vector3 position) {
        return new Vector2Int(CloudTileCoordFromFloat(position.x), CloudTileCoordFromFloat(position.z));
    }

    private int CloudTileCoordFromFloat(float value) {

        float position = value / (float)cloudTextureWidth;
        position -= Mathf.FloorToInt(position);
        int modPosition = Mathf.FloorToInt((float)cloudTextureWidth * position);

        return modPosition;

    }

}

public enum CloudStyle { Off, Fast, Fancy }