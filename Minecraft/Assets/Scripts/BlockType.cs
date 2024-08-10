using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


[System.Serializable]
public class BlockType {
    public string name;
    public bool isTransparent;
    public short[] faces = new short[6];

    public BlockType(string _name, bool _isTransparent, short[] _faces) {
        name = _name;
        isTransparent = _isTransparent;
        faces = _faces;
    }
}

public class Loader {
    [System.Serializable] private class BlockTypeWrapper { public BlockType[] blockTypes; }

    public static BlockType[] LoadBlockTypes() {
        string loadPath = Application.dataPath + "/Resources/BlockTypes.json";
        if (!File.Exists(loadPath)) {
            Debug.Log("BlockTypes.json does not exist");
            return new BlockType[] { };
        }

        string jsonText = File.ReadAllText(loadPath);
        BlockTypeWrapper jsonWrapper = JsonUtility.FromJson<BlockTypeWrapper>(jsonText);

        List<BlockType> blockTypes = new List<BlockType>();
        foreach (BlockType blockType in jsonWrapper.blockTypes)
            blockTypes.Add(blockType);

        return blockTypes.ToArray();
    }
}
