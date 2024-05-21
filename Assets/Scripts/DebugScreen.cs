using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugScreen : MonoBehaviour {

    WorldData worldData;
    TextMeshProUGUI text;

    float framerate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    private void Start() {
        worldData = GameObject.Find("World").GetComponent<WorldData>();
        text = GetComponent<TextMeshProUGUI>();

        halfWorldSizeInVoxels = VoxelData.worldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.worldSizeInChunks / 2;
    }

    private void Update() {
        if (timer > 1f){
            framerate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0f;
        } else timer += Time.deltaTime;

       text.text = "Minecraft 2 | Created by Flynn Lyon \n" +
                   "\n" +
                   "Framerate: " + framerate + " fps\n" +
                   "Coordinates: " + (Mathf.Floor(worldData.player.transform.position.x) - halfWorldSizeInVoxels) + ", " + (Mathf.Floor(worldData.player.transform.position.y) - halfWorldSizeInVoxels) + ", " + (Mathf.Floor(worldData.player.transform.position.z) - halfWorldSizeInVoxels) + "\n" +
                   "Chunk: " + (worldData.playerChunkCoord.x - halfWorldSizeInChunks) + ", " + (worldData.playerChunkCoord.z - halfWorldSizeInChunks) + "\n";
    }
}
