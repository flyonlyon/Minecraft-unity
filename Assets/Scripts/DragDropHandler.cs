using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropHandler : MonoBehaviour
{
    [SerializeField] private ItemSlotUI cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster mouse_raycaster = null;
    private PointerEventData mouse_eventData;
    [SerializeField] private EventSystem mouse_eventSystem = null;

    WorldData world;


    void Start() {
        world = GameObject.Find("World").GetComponent<WorldData>();
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    void Update() {

        if (!world.inUI) return;

        cursorSlot.transform.position = Input.mousePosition;
        if (Input.GetMouseButtonDown(0)) HandleSlotClick(CheckForSlot());

    }

    private void HandleSlotClick(ItemSlotUI clickedSlot) {
        if (clickedSlot == null) return;

        if (!cursorItemSlot.HasItem && !clickedSlot.HasItem)
            return;

        else if (clickedSlot.itemSlot.isCreative) {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
            cursorSlot.UpdateSlot();
            return;
        }

        else if (!cursorItemSlot.HasItem && clickedSlot.HasItem) {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            cursorSlot.UpdateSlot();
            return;
        }

        else if (cursorItemSlot.HasItem && !clickedSlot.HasItem) {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            clickedSlot.UpdateSlot();
            return;
        }

        else if (cursorItemSlot.HasItem && clickedSlot.HasItem) {

            if (cursorItemSlot.stack.id != clickedSlot.itemSlot.stack.id) {

                ItemStack oldCursorStack = cursorItemSlot.TakeAll();

                cursorSlot.itemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
                clickedSlot.itemSlot.InsertStack(oldCursorStack);
                
            } else {

                int toTake = world.blockTypes[clickedSlot.itemSlot.stack.id].maxStackSize
                             - clickedSlot.itemSlot.stack.amount;
                toTake = Mathf.Min(toTake, cursorItemSlot.stack.amount);

                cursorSlot.itemSlot.stack.amount -= toTake;
                if (cursorSlot.itemSlot.stack.amount == 0) cursorSlot.itemSlot.EmptySlot();
                else cursorSlot.UpdateSlot();

                clickedSlot.itemSlot.stack.amount += toTake;
                if (clickedSlot.itemSlot.stack.amount == 0) clickedSlot.itemSlot.EmptySlot();
                else clickedSlot.UpdateSlot();
            }
        }
    }

    private ItemSlotUI CheckForSlot() {
        mouse_eventData = new PointerEventData(mouse_eventSystem);
        mouse_eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        mouse_raycaster.Raycast(mouse_eventData, results);

        foreach (RaycastResult result in results)
            if (result.gameObject.tag == "ItemSlotUI")
                return result.gameObject.GetComponent<ItemSlotUI>();

        return null;
    }

}
