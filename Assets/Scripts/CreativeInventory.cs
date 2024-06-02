using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreativeInventory : MonoBehaviour {

    public GameObject slotPrefab;
    WorldData world;

    List<ItemSlot> slots = new List<ItemSlot>();

    void Start() {

        world = GameObject.Find("World").GetComponent<WorldData>();

        for (int i = 1; i < world.blockTypes.Length; ++i) {
            GameObject slot = Instantiate(slotPrefab, transform);

            ItemStack itemStack = new ItemStack((byte)i, world.blockTypes[i].maxStackSize);
            ItemSlot itemSlot = new ItemSlot(slot.GetComponent<ItemSlotUI>(), itemStack);
            itemSlot.isCreative = true;
        }

    }
}
