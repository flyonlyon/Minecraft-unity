using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour {

    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public TextMeshProUGUI slotAmount;

    private WorldData world;

    private void Awake() {
        world = GameObject.Find("World").GetComponent<WorldData>();
    }

    public bool HasItem {
        get {
            if (itemSlot == null) return false;
            else return itemSlot.HasItem;
        }
    }

    public void Link(ItemSlot _itemSlot) {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkItemSlotUI(this);
        UpdateSlot();
    }

    public void Unlink() {
        itemSlot = null;
        isLinked = false;
        itemSlot.UnlinkItemSlotUI();
        UpdateSlot();
    }

    public void UpdateSlot() {

        if (itemSlot != null && itemSlot.HasItem) {

            slotIcon.sprite = world.blockTypes[itemSlot.stack.id].blockIcon;
            slotAmount.text = itemSlot.stack.amount.ToString();
            slotIcon.enabled = true;
            slotAmount.enabled = true;

        } else {
            Clear();
        }
    }

    public void Clear() {
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy() {
        if (itemSlot != null) itemSlot.UnlinkItemSlotUI();
    }

}

[System.Serializable]
public class ItemSlot
{
    public ItemStack stack = null;
    private ItemSlotUI itemSlotUI = null;
    public bool isCreative;

    public ItemSlot(ItemSlotUI _itemSlotUI) {
        stack = null;
        itemSlotUI = _itemSlotUI;
        itemSlotUI.Link(this);
    }

    public ItemSlot(ItemSlotUI _itemSlotUI, ItemStack _stack)
    {
        stack = _stack;
        itemSlotUI = _itemSlotUI;
        itemSlotUI.Link(this);
    }

    public void LinkItemSlotUI(ItemSlotUI _itemSlotUI) {
        itemSlotUI = _itemSlotUI;
    }

    public void UnlinkItemSlotUI() {
        itemSlotUI = null;
    }

    public void EmptySlot() {
        stack = null;
        if (itemSlotUI != null) itemSlotUI.UpdateSlot();
    }

    public int Take(int amount) {

        if (amount < stack.amount) {
            stack.amount -= amount;
            itemSlotUI.UpdateSlot();
            return amount;
        }

        if (amount == stack.amount) {
            EmptySlot();
            return amount;
        }

        int _amount = stack.amount;
        EmptySlot();
        return _amount;
    }

    public ItemStack TakeAll() {
        ItemStack handOver = new ItemStack(stack.id, stack.amount);
        EmptySlot();
        return handOver;
    }

    public void InsertStack(ItemStack _stack) {
        stack = _stack;
        itemSlotUI.UpdateSlot();
    }

    public bool HasItem {
        get { return (stack != null); }
    }
}
