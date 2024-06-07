using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour {
    private int cloudHeight = VoxelData.chunkHeight - 10;

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

        for (int x = 0; x < cloudTextureWidth; x += cloudTileSize) {
            for (int y = 0; y < cloudTextureWidth; y += cloudTileSize) {

                Vector3 position = new Vector3(x, cloudHeight, y);
                clouds.Add(CloudTilePosFromVector3(position), CreateCloudTile(CreateCloudMesh(x, y), position));
                

            }
        }

    }

    public void UpdateClouds() {

        for (int x = 0; x < cloudTextureWidth; x += cloudTileSize) {
            for (int y = 0; y < cloudTextureWidth; y += cloudTileSize) {

                Vector3 position = world.player.position + new Vector3(x, 0, y) + offset;
                position = new Vector3(RoundToCloud(position.x), cloudHeight, RoundToCloud(position.z));
                Vector2Int cloudPosition = CloudTilePosFromVector3(position);

                clouds[cloudPosition].transform.position = position;

            }
        }
    }

    public int RoundToCloud(float value) {
        return Mathf.FloorToInt(value / cloudTileSize) * cloudTileSize;
    }

    private Mesh CreateCloudMesh(int x, int z) {

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
