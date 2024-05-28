using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Minecraft/BiomeData")]
public class BiomeData : ScriptableObject {
    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale = 1.3f;
    [Range(0f, 1f)] public float treeZoneThreshold = 0.6f;
    public float treePlacementScale = 15f;
    [Range(0f, 1f)] public float treePlacementThreshold = 0.8f;
    public int maxTreeSize = 7;
    public int minTreeSize = 4;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode {
    public string nodeName;

    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}