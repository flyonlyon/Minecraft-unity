using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class AtlasPacker : EditorWindow {

    int blockSizeInPixels = 16;
    int atlasSizeInBlocks = 16;
    int atlasSizeInPixels;

    Object[] rawTextures = new Object[256];
    List<Texture2D> sortedTextures = new List<Texture2D>();
    Texture2D atlas;

    [MenuItem("Minecraft/Atlas Packer")]
    public static void ShowWindow() {

        EditorWindow.GetWindow(typeof(AtlasPacker));

    }

    private void OnGUI() {

        atlasSizeInPixels = blockSizeInPixels * atlasSizeInBlocks;

        GUILayout.Label("Minecraft Texture Atlas Packer", EditorStyles.boldLabel);

        blockSizeInPixels = EditorGUILayout.IntField("Block Size (In Pixels)", blockSizeInPixels);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas Size (In Blocks)", atlasSizeInBlocks);

        if (GUILayout.Button("Load Textures")) {
            LoadTextures();
            PackAtlas();
        }

        if (GUILayout.Button("Clear Textures")) {
            atlas = new Texture2D(atlasSizeInPixels, atlasSizeInPixels);
            Debug.Log("AtlasPacker: Textures Cleared");
        }

        if (GUILayout.Button("Save Atlas")) {
            byte[] bytes = atlas.EncodeToPNG();

            try {  File.WriteAllBytes(Application.dataPath + "/Textures/PackedAtlas.png", bytes); }
            catch { Debug.Log("AtlasPacker: Error writing to filepath. Atlas not saved."); }
        }

        GUILayout.Label(atlas);
    }

    private void LoadTextures() {

        sortedTextures.Clear();
        rawTextures = Resources.LoadAll("AtlasPacker", typeof(Texture2D));

        int index = 0;
        foreach (Object tex in rawTextures) {
            Texture2D texture = (Texture2D)tex;
            if (texture.width == blockSizeInPixels && texture.height == blockSizeInPixels)
                sortedTextures.Add(texture);
            else Debug.Log("AtlasPacker: Texture \"" + texture.name + "\" has an invalid size. Texture not loaded") ;
            ++index;
        }

        Debug.Log("AtlasPacker: " + sortedTextures.Count + " textures successfully loaded");

    }

    private void PackAtlas() {

        atlas = new Texture2D(atlasSizeInPixels, atlasSizeInPixels);
        Color[] pixels = new Color[atlasSizeInPixels * atlasSizeInPixels];

        for (int x = 0; x < atlasSizeInPixels; ++x) {
            for (int y = 0; y < atlasSizeInPixels; ++y) {

                int currBlockX = x / blockSizeInPixels;
                int currBlockY = y / blockSizeInPixels;

                int index = currBlockY * blockSizeInPixels + currBlockX;

                if (index < sortedTextures.Count)
                    pixels[(atlasSizeInPixels - y - 1) * atlasSizeInPixels + x] =
                        sortedTextures[index].GetPixel(x, blockSizeInPixels - y - 1);
                else
                    pixels[(atlasSizeInPixels - y - 1) * atlasSizeInPixels + x] =
                        new Color(0f, 0f, 0f, 0f);

            }
        }

        atlas.SetPixels(pixels);
        atlas.Apply();

    }
}
