using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure {
    public static void MakeTree(Vector3 position, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight) {
        int height = (int)(Noise.Get2DPerlin(new Vector2(position.x, position.z), 250, 3)
                     * (maxTrunkHeight - minTrunkHeight) + minTrunkHeight);

        for (int i = 1; i < height; ++i) {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 9)); // Wood
        }

        for (int x = -2; x < 3; ++x) {
            for (int y = 0; y < 6; ++y) {
                for (int z = -2; z < 3; ++z) {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height+ y, position.z + z), 10)); // Leaves
                }
            }
        }
    }
}
