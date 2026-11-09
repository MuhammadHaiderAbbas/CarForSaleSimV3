using System.Collections.Generic;
using CarSales.Studio;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 11: the sale-hall room. During auction hours (and while you're near) bidders sit on the benches and stand
    /// along the walls; the auctioneer talks while a lot is up and points at the room for bids; standing bidders raise
    /// their hands for room bids and clap when a lot sells. Needs characters from Character Setup (or the Asset Studio).
    /// </summary>
    public class AuctionCrowd : MonoBehaviour
    {
        const float NearDistance = 70f;
        readonly List<CharacterAnimator> seated = new List<CharacterAnimator>(), standing = new List<CharacterAnimator>();
        readonly List<GameObject> spawned = new List<GameObject>();
        CharacterAnimator auctioneer;
        bool present;
        float checkTimer;

        void OnEnable() => AuctionManager.OnHallEvent += React;
        void OnDisable() { AuctionManager.OnHallEvent -= React; Clear(); }

        void Update()
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 1f;
            var cam = Camera.main;
            float hour = CarSalesServices.DayCycle ? CarSalesServices.DayCycle.HourOfDay : 12f;
            bool want = cam && (cam.transform.position - transform.position).sqrMagnitude < NearDistance * NearDistance && hour >= 9.5f && hour < 17.5f;
            if (want && !present) Fill();
            else if (!want && present) Clear();
            if (auctioneer)
            {
                bool live = CarSalesServices.Auction && CarSalesServices.Auction.LiveLot != null;
                if (live) auctioneer.Loop(CharacterAnim.Talking); else auctioneer.StopLoop();
            }
        }

        void Fill()
        {
            present = true;
            // Seated bidders: two to three per bench, facing the stage.
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name != "Bench" || t.lossyScale.x < 4f) continue;
                int n = Random.Range(1, 4);
                for (int i = 0; i < n; i++)
                {
                    float along = Random.Range(-0.42f, 0.42f) * t.lossyScale.x;
                    var pos = t.position + t.right * along + transform.forward * 0.2f;
                    pos.y = transform.position.y;
                    var body = Spawn(pos, Quaternion.LookRotation(transform.forward), CharacterUse.Auction);
                    if (body) { body.Loop(CharacterAnim.SitIdle); seated.Add(body); }
                }
            }
            // Standing bidders along the side walls.
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var local = new Vector3(side * Random.Range(10.5f, 11.8f), 0f, Random.Range(2.5f, 10f));
                var pos = transform.TransformPoint(local);
                var face = transform.TransformDirection(new Vector3(-side * 0.6f, 0f, 1f));
                var body = Spawn(pos, Quaternion.LookRotation(face), CharacterUse.Auction);
                if (body) standing.Add(body);
            }
            // The auctioneer behind the rostrum (the old capsule is hidden).
            var cap = transform.Find("Auctioneer");
            if (cap)
            {
                var pos = cap.position; pos.y = transform.position.y;
                var body = Spawn(pos, cap.rotation, CharacterUse.Auction | CharacterUse.Staff);
                if (body) { auctioneer = body; foreach (var r in cap.GetComponentsInChildren<Renderer>()) r.enabled = false; }
            }
        }

        CharacterAnimator Spawn(Vector3 pos, Quaternion rot, CharacterUse role)
        {
            var root = new GameObject("Bidder");
            root.transform.SetPositionAndRotation(pos, rot);
            root.transform.SetParent(transform, true);
            var body = CharacterSpawn.Attach(root.transform, role);
            if (!body) { Destroy(root); return null; }
            spawned.Add(root);
            return body;
        }

        void Clear()
        {
            present = false;
            foreach (var g in spawned) if (g) Destroy(g);
            spawned.Clear(); seated.Clear(); standing.Clear();
            auctioneer = null;
            var cap = transform.Find("Auctioneer");
            if (cap) foreach (var r in cap.GetComponentsInChildren<Renderer>()) r.enabled = true;
        }

        void React(AuctionManager.HallEvent e)
        {
            if (!present) return;
            switch (e)
            {
                case AuctionManager.HallEvent.RoomBid:
                    if (standing.Count > 0) { var b = standing[Random.Range(0, standing.Count)]; if (b) b.Play(CharacterAnim.RaiseHand); }
                    if (auctioneer) auctioneer.Play(CharacterAnim.Point);
                    break;
                case AuctionManager.HallEvent.PlayerBid:
                    if (auctioneer) auctioneer.Play(CharacterAnim.Point);
                    break;
                case AuctionManager.HallEvent.Sold:
                    foreach (var b in standing) if (b && Random.value < 0.6f) b.Play(CharacterAnim.Clap);
                    break;
                case AuctionManager.HallEvent.NewLot:
                    foreach (var b in standing) if (b && Random.value < 0.3f) b.Play(CharacterAnim.Thinking);
                    break;
            }
        }
    }
}
