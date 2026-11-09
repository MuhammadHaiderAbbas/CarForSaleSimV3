using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// OwnedCars is the SOURCE OF TRUTH (and what gets saved). Car GameObjects are only visuals.
    /// A CarDisplay holds a reference to the SAME CarInstance object, so edits are shared.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public List<CarInstance> OwnedCars { get; private set; } = new List<CarInstance>();
        public event Action OnInventoryChanged;

        void Awake() => CarSalesServices.Inventory = this;

        public void Add(CarInstance car) { OwnedCars.Add(car); NotifyChanged(); }
        public void Remove(string id) { OwnedCars.RemoveAll(c => c.instanceId == id); NotifyChanged(); }
        public CarInstance Get(string id) => OwnedCars.Find(c => c.instanceId == id);
        public void SetCars(List<CarInstance> cars) { OwnedCars = cars ?? new List<CarInstance>(); NotifyChanged(); }
        public void NotifyChanged() => OnInventoryChanged?.Invoke();
    }
}
