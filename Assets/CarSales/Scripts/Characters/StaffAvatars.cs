using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using CarSales.Studio;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 11: your staff are people you can see. During working hours each member of staff who isn't off sick stands
    /// at their post at your main dealership and service centre: the receptionist types at the desk, salespeople talk
    /// on the showroom floor, the manager makes calls, mechanics kneel at the bays while a job is on, detailers clean
    /// the dirtiest car on the lot, drivers wait by the lot entrance. Added by CarSalesBootstrap.
    /// </summary>
    public class StaffAvatars : MonoBehaviour
    {
        readonly Dictionary<string, (GameObject root, CharacterAnimator body, StaffRole role)> shown = new Dictionary<string, (GameObject, CharacterAnimator, StaffRole)>();
        float timer;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 2f;
            Refresh();
        }

        void OnDisable() { foreach (var s in shown.Values) if (s.root) Destroy(s.root); shown.Clear(); }

        void Refresh()
        {
            var staff = CarSalesServices.Staff;
            var dealer = DealershipManager.Main;
            var day = CarSalesServices.DayCycle;
            bool hours = day && day.HourOfDay >= BoardLogic.WorkStart && day.HourOfDay < BoardLogic.WorkEnd;
            var working = staff && dealer && hours
                ? staff.Employees.Where(e => e.sickDay != CarRecords.Today).ToList()
                : new List<Employee>();

            foreach (var id in shown.Keys.ToList())
                if (!working.Exists(e => e.id == id)) { if (shown[id].root) Destroy(shown[id].root); shown.Remove(id); }

            var byRole = new Dictionary<StaffRole, int>();
            foreach (var e in working)
            {
                byRole.TryGetValue(e.Role, out int n);
                byRole[e.Role] = n + 1;
                if (!shown.TryGetValue(e.id, out var s) || !s.root)
                {
                    var root = new GameObject("Staff_" + e.name);
                    root.transform.SetParent(transform, false);
                    var body = CharacterSpawn.Attach(root.transform, CharacterUse.Staff);
                    if (!body) { Destroy(root); continue; }   // no characters set up: staff stay invisible, as before
                    s = (root, body, e.Role);
                    shown[e.id] = s;
                    Place(s, dealer, n);
                }
                Act(s, dealer, n);
            }
        }

        static Vector3 Local(DealershipManager d, float x, float z) => d.transform.TransformPoint(new Vector3(x, 0f, z));

        void Place((GameObject root, CharacterAnimator body, StaffRole role) s, DealershipManager d, int n)
        {
            Vector3 pos; Quaternion rot = d.transform.rotation;
            switch (s.role)
            {
                case StaffRole.Receptionist: pos = Local(d, -8.2f + n * 1.2f, 23.2f); rot = d.transform.rotation * Quaternion.Euler(0f, 180f, 0f); break;
                case StaffRole.Salesperson: pos = Local(d, n % 2 == 0 ? -3f : 3f, 13f + (n / 2) * 2.5f); rot = d.transform.rotation * Quaternion.Euler(0f, 180f, 0f); break;
                case StaffRole.Manager: pos = Local(d, 9f, 20f); rot = d.transform.rotation * Quaternion.Euler(0f, 220f, 0f); break;
                case StaffRole.Driver: pos = Local(d, 16f + n, -19f); rot = d.transform.rotation * Quaternion.Euler(0f, 180f, 0f); break;
                case StaffRole.Mechanic:
                {
                    var bays = WorkStation.All.Where(w => w).ToList();
                    if (bays.Count > 0)
                    {
                        var w = bays[n % bays.Count];
                        pos = w.transform.position + w.transform.forward * 1.2f + w.transform.right * 1.5f;
                        rot = Quaternion.LookRotation(-w.transform.right);
                    }
                    else pos = Local(d, 14f, 20f);
                    break;
                }
                default: pos = Local(d, -14f + n * 2f, -2f); break;   // detailer: moves to a car in Act
            }
            s.root.transform.SetPositionAndRotation(pos, rot);
        }

        void Act((GameObject root, CharacterAnimator body, StaffRole role) s, DealershipManager d, int n)
        {
            var b = s.body;
            if (!b) return;
            switch (s.role)
            {
                case StaffRole.Receptionist: b.Loop(CharacterAnim.Typing); break;
                case StaffRole.Manager: b.Loop(Random.value < 0.5f ? CharacterAnim.PhoneCall : CharacterAnim.Thinking); break;
                case StaffRole.Salesperson:
                {
                    // Talks to a nearby customer, otherwise waits; turns to face you when you're close.
                    bool customerNear = Physics.OverlapSphere(s.root.transform.position, 4f).Any(c => c.GetComponentInParent<CustomerAI>());
                    if (customerNear) b.Loop(CharacterAnim.Talking); else b.StopLoop();
                    FacePlayer(s.root.transform, 6f);
                    break;
                }
                case StaffRole.Mechanic:
                {
                    var ws = CarSalesServices.Workshop;
                    bool busy = ws && ws.Jobs.Count > 0;
                    if (busy) b.Loop(CharacterAnim.Kneel); else b.StopLoop();
                    break;
                }
                case StaffRole.Detailer:
                {
                    var dirtiest = d.spots.Where(p => p && p.Occupant && p.Occupant.Data != null && p.Occupant.Data.cleanliness < 85f)
                                          .OrderBy(p => p.Occupant.Data.cleanliness).Skip(n).FirstOrDefault();
                    if (dirtiest)
                    {
                        var car = dirtiest.Occupant.transform;
                        var pos = car.position + car.right * 1.4f;
                        s.root.transform.SetPositionAndRotation(new Vector3(pos.x, d.transform.position.y, pos.z), Quaternion.LookRotation(-car.right));
                        b.Loop(CharacterAnim.Kneel);
                    }
                    else b.StopLoop();
                    break;
                }
                default: b.StopLoop(); break;
            }
        }

        static void FacePlayer(Transform t, float range)
        {
            var cam = Camera.main;
            if (!cam) return;
            var dir = cam.transform.position - t.position; dir.y = 0f;
            if (dir.sqrMagnitude < range * range && dir.sqrMagnitude > 0.2f) t.rotation = Quaternion.LookRotation(dir);
        }
    }
}
