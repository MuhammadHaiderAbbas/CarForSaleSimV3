using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Foundation;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// The rental &amp; hire business (phase 14, "Final Map"): any of your forecourt cars can go out on hire —
    /// a daily rate, a refundable deposit, damage and miles applied on return, late fees kept from the deposit.
    /// The dealer computer has a Rentals app; the world has a competitor (RentGo, next to Luxury Lane) running the same game.
    /// </summary>
    public class RentalManager : MonoBehaviour
    {
        readonly List<RentalContract> contracts = new List<RentalContract>();
        public IReadOnlyList<RentalContract> Contracts => contracts;
        public event Action OnRentalsChanged;

        void Awake() => CarSalesServices.Rentals = this;
        void OnEnable() => EventBus.Subscribe<DayStarted>(OnDayStarted);
        void OnDisable() => EventBus.Unsubscribe<DayStarted>(OnDayStarted);

        public static RentalQuote Quote(CarInstance car, int days)
        {
            int value = CarValueCalculator.MarketValue(car);
            return RentalRules.Quote(Mathf.Max(1, value), days);
        }

        /// <summary>Can this car go out on hire?</summary>
        public static bool CanRent(CarInstance car, out string reason)
        {
            reason = null;
            if (car == null) { reason = "no car"; return false; }
            if (car.personal) { reason = "your personal car is not for hire"; return false; }
            if (CarSalesServices.Inventory == null || !CarSalesServices.Inventory.OwnedCars.Contains(car)) { reason = "not your car"; return false; }
            if (car.state == CarState.InTransit) { reason = "the car is still in transit"; return false; }
            var svc = CarSalesServices.Rentals;
            if (svc != null && svc.IsRentedOut(car.instanceId)) { reason = "already out on hire"; return false; }
            return true;
        }

        public bool IsRentedOut(string carInstanceId) =>
            contracts.Any(c => c != null && c.active && c.carInstanceId == carInstanceId);

        /// <summary>Active contracts, oldest first.</summary>
        public List<RentalContract> Active() => contracts.Where(c => c != null && c.active).OrderBy(c => c.dueDay).ToList();

        /// <summary>Total active hire income per day (what the fleet earns while it is out).</summary>
        public int DailyHireIncome => Active().Sum(c => c.dailyRate);

        /// <summary>Rent a car out for `days` days. Returns false + reason when it can't.</summary>
        public bool RentOut(CarInstance car, string renterName, int days, out string error)
        {
            error = null;
            if (!CanRent(car, out error)) return false;
            var day = CarSalesServices.DayCycle;
            int today = day ? day.CurrentDay : CarRecords.Today;
            days = RentalRules.ClampDays(days);
            var quote = Quote(car, days);
            var dealershipId = string.IsNullOrEmpty(car.dealershipId) ? (DealershipManager.Main ? DealershipManager.Main.dealershipId : "autoworld") : car.dealershipId;

            var c = new RentalContract
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 12),
                carInstanceId = car.instanceId,
                carName = car.Name,
                dealershipId = dealershipId,
                renterName = string.IsNullOrWhiteSpace(renterName) ? "A renter" : renterName.Trim(),
                startDay = today,
                dueDay = today + days,
                days = days,
                dailyRate = quote.DailyRate,
                deposit = quote.Deposit,
                active = true,
                lateNotifiedFor = 0,
            };

            // The car leaves the forecourt (the HUD shows it as gone, spots free up).
            if (CarSalesServices.Vehicles) CarSalesServices.Vehicles.Despawn(car.instanceId);

            // Take the hire fee and the deposit; both are reconciled on return.
            if (CarSalesServices.Economy)
            {
                CarSalesServices.Economy.AddMoney(quote.Total, TxCategory.Rental, $"Hire fee ({days}d) - {c.carName}", c.id);
                CarSalesServices.Economy.AddMoney(quote.Deposit, TxCategory.Deposits, $"Rental deposit held - {c.carName}", c.id);
            }
            contracts.Add(c);
            if (CarSalesServices.Messages)
                CarSalesServices.Messages.Add(MessageType.Info, "Car rented out", $"{c.carName} went out to {c.renterName} for {days} day(s). Due back Day {c.dueDay}. Deposit £{quote.Deposit:N0} held.");
            Changed();
            return true;
        }

        /// <summary>Bring a car back early or late. Applies mileage, wear, the deposit refund and the late fees kept.</summary>
        public bool Return(string contractId, out string message)
        {
            message = null;
            var c = contracts.Find(x => x != null && x.id == contractId);
            if (c == null) { message = "No such contract."; return false; }
            if (!c.active) { message = "That one is already back."; return false; }
            var day = CarSalesServices.DayCycle;
            int today = day ? day.CurrentDay : CarRecords.Today;
            int daysLate = RentalRules.DaysLate(c.dueDay, today);
            int feeKept = RentalRules.LateFeeKept(c.deposit, daysLate);
            int refund = RentalRules.RefundOnReturn(c.deposit, daysLate);
            int daysOut = Mathf.Max(1, today - c.startDay);

            // Damage and miles on the physical car.
            var car = CarSalesServices.Inventory ? CarSalesServices.Inventory.Get(c.carInstanceId) : null;
            if (car != null)
            {
                car.mileage += RentalRules.MilesPerDay * daysOut;
                car.condition = Mathf.Clamp(car.condition - RentalRules.WearPerDay * daysOut, 1f, 100f);
                car.fuel = Mathf.Clamp(car.fuel - 22f * daysOut, 5f, 100f);
                if (car.state == CarState.Listed) car.state = CarValueCalculator.DefaultState(car);   // it won't be on the forecourt anymore
                car.location = CarLocation.Spot;
                car.dealershipId = string.IsNullOrEmpty(c.dealershipId) ? "autoworld" : c.dealershipId;
                car.parkingSpotIndex = -1;   // VehicleRegistry finds it a free spot
            }

            if (CarSalesServices.Economy)
                CarSalesServices.Economy.ForceSpend(refund, TxCategory.Deposits, $"Deposit refunded{(feeKept > 0 ? $" (kept £{feeKept:N0} late fees)" : " in full")} - {c.carName}", c.id);
            if (CarSalesServices.Progress?.Stats != null)
            {
                CarSalesServices.Progress.Stats.rentalsCompleted++;
                CarSalesServices.Progress.Stats.rentalEarnings += c.dailyRate * c.days + feeKept;
            }

            c.active = false;
            if (car != null && CarSalesServices.Vehicles) CarSalesServices.Vehicles.Spawn(car);   // back on the forecourt

            message = feeKept > 0
                ? $"Back {daysLate} day(s) late. Kept £{feeKept:N0} of the deposit as late fees, refunded £{refund:N0}."
                : $"Back on time. Deposit £{c.deposit:N0} refunded in full.";
            if (CarSalesServices.Messages)
                CarSalesServices.Messages.Add(MessageType.Info, "Car returned", $"{c.carName} is back from {c.renterName}. {message}");
            Changed();
            return true;
        }

        void OnDayStarted(DayStarted e)
        {
            foreach (var c in contracts.Where(x => x != null && x.active))
            {
                int late = RentalRules.DaysLate(c.dueDay, e.Day);
                if (late <= 0) continue;
                if (c.lateNotifiedFor == 0 || e.Day - c.lateNotifiedFor >= 3)
                {
                    c.lateNotifiedFor = e.Day;
                    if (CarSalesServices.Messages)
                        CarSalesServices.Messages.Add(MessageType.Info, "Rental overdue", $"{c.carName} (out to {c.renterName}) is {late} day(s) late. Return it from the Rentals app — late fees come out of the £{c.deposit:N0} deposit.");
                }
            }
            Changed();
        }

        public void SetState(List<RentalContract> list)
        {
            contracts.Clear();
            if (list != null) foreach (var c in list) if (c != null) contracts.Add(c);
            Changed();
        }

        public List<RentalContract> GetState() => contracts.Select(c => c).ToList();

        void Changed() => OnRentalsChanged?.Invoke();
    }
}
