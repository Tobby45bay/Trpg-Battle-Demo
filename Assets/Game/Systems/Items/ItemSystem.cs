using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.Effect;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using Game.Systems.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static Game.Core.Game.GameManager;
using static Game.Core.Game.GameManager.GameConst;

namespace Game.Systems.Item
{
    public enum ItemType
    {
        Weapon,
        Consumable
    }

    public enum WeaponType
    {
        Sword,
        Axe,
        Lance,
        Staff,
        Bow,
        None
    }

    [Serializable]
    public class ItemSave
    {
        public int ItemID;
        public string Name;
        public int uses;
        public bool isEquipped;
    }

    public class ItemInstance
    {
        public int instanceId;
        public int itemId;

        public int currentUses;
        public bool isEquipped;

        public ItemSO Data;
    }

    public class UnitInventory
    {
        public int ownerUnitId;
        public List<ItemInstance> inventory = new();
        public ItemInstance equippedWeapon { get; private set; }

        public UnitInventory(int unitId)
        {
            ownerUnitId = unitId;
        }

        public void Add(ItemInstance itemInstance)
        {
            if (inventory.Count >= GameConst.Inventory.MaxItemsPerUnit)
                return;

            inventory.Add(itemInstance);
        }

        public void Remove(ItemInstance itemInstance)
        {
            if(equippedWeapon == itemInstance)
                UnquipWeapon();
            inventory.Remove(itemInstance);
        }

        public bool EquipWeapon(ItemInstance itemInstance)
        {
            UnquipWeapon();

            equippedWeapon = itemInstance;
            equippedWeapon.isEquipped = true;

            TriggerEquipAction(equippedWeapon, true);
            Debug.Log($"{equippedWeapon.Data.name} was eqipped");
            return true;
        }

        public void UnquipWeapon()
        {
            if (!IsEquipped()) return;
            TriggerEquipAction(equippedWeapon, false);
            equippedWeapon.isEquipped = false;
            equippedWeapon = null;
        }

        public bool IsEquipped() => equippedWeapon != null;

        public bool IsItemPresent(ItemInstance itemInstance) => inventory.Contains(itemInstance);
        public bool IsInventoryFull() => inventory.Count >= GameConst.Inventory.MaxItemsPerUnit;


        private void TriggerEquipAction(ItemInstance weapon, bool onEquip)
        {
            if (weapon?.Data is not WeaponSO weaponData)
                return;

            var context = new EffectContext
            {
                battleSystems = BattleManager.Instance.battleSystems,
                SourceUnitId = ownerUnitId,
                TargetUnitId = ownerUnitId,

                SourceCode = SourceType.Weapon,
                SourceId = weapon.instanceId,

                SourceRefCode = SourceType.Item,
                SourceRefId = weapon.itemId
            };

            var triggerGroup = onEquip
                ? weaponData.onEquip
                : weaponData.onRemove;

            if (triggerGroup == null)
                return;

            ActionHelper.ExecuteActions(triggerGroup.Actions, context);
        }

    }

    public static class ItemFactory
    {
        public static class ItemIdGenerator
        {
            public static int _nextId = GameConst.Id.ItemStart;

            public static int GenNextId() { return _nextId++; }

            public static void Reset()
            {
                _nextId = GameConst.Id.ItemStart;
            }
        }

        private static ItemDatabase itemDb;

        public static void Initialize(ItemDatabase itemDatabase)
        {
            itemDb = itemDatabase;
        }

        public static ItemInstance CreateItem(int itemId)
        {
            if (itemDb == null)
                throw new Exception("ItemFactory not initialized");

            var itemData = itemDb.GetItem(itemId);
            if (itemData == null)
                throw new Exception($"Item {itemId} not found");

            var instanceId = ItemIdGenerator.GenNextId();

            var item = new ItemInstance
            {
                instanceId = instanceId,
                itemId = itemId,
                Data = itemData,
                isEquipped = false,
                currentUses = itemData.maxUse < 0 ? -1 : itemData.maxUse
            };

            Debug.Log($"{item.Data.name}.{item.Data.type} : was created ");
            return item;
        }

        public static ItemInstance CreateItem(ItemSave itemSave)
        {
            var item = CreateItem(itemSave.ItemID);

            item.isEquipped = itemSave.isEquipped;
            item.currentUses = itemSave.uses;


            return item;
        }

    }

    public class InventoryListener : TriggerListener
    {
        private InventoryManager inventoryManager;

        public InventoryListener(InventoryManager inventoryManager)
        {
            this.priority = 4;
            this.inventoryManager = inventoryManager;
        }

        public override void Trigger(TriggerContext context)
        {
            //
        }
    }

    public class InventoryManager
    {
        private Dictionary<int, UnitInventory> inventoryRegistry = new();
        public ItemDatabase itemDb;
        public void InitializeSystem(GameManager gm)
        {
            itemDb = gm.ItemDb;
            gm.triggerDispatcher.Register(new InventoryListener(this));
            ItemFactory.Initialize(itemDb);
        }

        public void RegisterUnit(int unitId, List<ItemSave> itemSaves)
        {
            if (inventoryRegistry.ContainsKey(unitId)) return;

            var inventory = new UnitInventory(unitId);
            inventoryRegistry[unitId] = InventoryHelper.LoadInventory(itemSaves, inventory);
        }

        public void AddItem(int unitId, ItemInstance itemInstance)
        {
            if (!inventoryRegistry.ContainsKey(unitId)) return;

            var inventory = inventoryRegistry[unitId];

            if (inventory.IsItemPresent(itemInstance)) return;

            inventory.Add(itemInstance);
        }

        public void RemoveItem(int unitId, ItemInstance itemInstance) 
        {
            if (!inventoryRegistry.ContainsKey(unitId)) return;

            var inventory = inventoryRegistry[unitId];

            if (!inventory.IsItemPresent(itemInstance)) return;

            inventory.Remove(itemInstance);
        }

        public void EquipWeapon(int unitId,ItemInstance itemInstance)
        {
            if (!inventoryRegistry.ContainsKey(unitId)) return;

            if (itemInstance.Data is not WeaponSO wd) return;   
            
            var inventory = inventoryRegistry[unitId];

            if (!inventory.IsItemPresent(itemInstance)) return;

            inventory.EquipWeapon(itemInstance);
            BattleManager.Instance.battleSystems.AttackLoadOutManager.SetDefaultAttack(unitId, wd.DefaultAttack);
        }

        public void UnequipWeapon(int unitId, ItemInstance itemInstance)
        {
            if (!inventoryRegistry.ContainsKey(unitId)) return;

            if (itemInstance.Data.type != ItemType.Weapon) return;

            var inventory = inventoryRegistry[unitId];

            if (!inventory.IsItemPresent(itemInstance)) return;

            inventory.UnquipWeapon();
            BattleManager.Instance.battleSystems.AttackLoadOutManager.SetDefaultAttack(unitId, -1);
        }

        public ItemInstance GetEquippedWeapon(int unitId)
        {
            if (!inventoryRegistry.ContainsKey(unitId)) return null;
            var inventory = inventoryRegistry[unitId];
            return inventory.equippedWeapon;
        }

        public void TradeItem(int tradingUnitId,int partnerUnitId,ItemInstance tradingItemInstance)
        {
            if (!inventoryRegistry.ContainsKey(tradingUnitId) ||
                !inventoryRegistry.ContainsKey(partnerUnitId)) return;


            
            var tardingInventory = inventoryRegistry[tradingUnitId];
            var partnerInventory = inventoryRegistry[partnerUnitId];

            if (!tardingInventory.IsItemPresent(tradingItemInstance)) return;

            tardingInventory.Remove(tradingItemInstance);
            partnerInventory.Add(tradingItemInstance);
        }

        public void UseItem(int unitId, ItemInstance itemInstance)
        {

        }
    }

    public static class InventoryHelper
    {
        public static UnitInventory LoadInventory(List<ItemSave> items,UnitInventory unitInventory)
        {
            unitInventory.inventory.Clear();
            unitInventory.UnquipWeapon();

            foreach (var item in items)
            {
                var ItemInstance = ItemFactory.CreateItem(item);
                unitInventory.Add(ItemInstance);
                if(ItemInstance.isEquipped) unitInventory.EquipWeapon(ItemInstance);
            }
            return unitInventory;
        }
    }
}
