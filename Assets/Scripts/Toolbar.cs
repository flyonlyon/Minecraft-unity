using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour {

    public Player player;
    public ItemSlotUI[] slots;
    public RectTransform selected;
    public int slotIndex;

    private void Start() {

        byte index = 1;
        foreach (ItemSlotUI slot in slots) {
            ItemStack stack = new ItemStack(index, Random.Range(1, 65));
            ItemSlot currSlot = new ItemSlot(slots[index - 1], stack);
            ++index;
        }
        
    }

    private void Update() {

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0) {
            if (scroll > 0) --slotIndex;
            else ++slotIndex;

            if (slotIndex > slots.Length - 1) slotIndex = 0;
            if (slotIndex < 0) slotIndex = slots.Length - 1;

            selected.position = slots[slotIndex].slotIcon.transform.position;

        }

        CheckNumberInputs();
    }

    private void CheckNumberInputs() {
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            slotIndex = 0;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            slotIndex = 1;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            slotIndex = 2;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha4)) {
            slotIndex = 3;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha5)) {
            slotIndex = 4;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha6)) {
            slotIndex = 5;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha7)) {
            slotIndex = 6;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha8)) {
            slotIndex = 7;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        } else if (Input.GetKeyDown(KeyCode.Alpha9)) {
            slotIndex = 8;
            selected.position = slots[slotIndex].slotIcon.transform.position;
        }
    }

}



