using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>Inbox page. Private-seller offers can be bought straight from here.</summary>
    public class MessagesUI : ComputerPage
    {
        RectTransform list;
        TextMeshProUGUI header;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 12f);
            var top = UIFactory.HRow(Area, 44, 10, "Top", false);
            header = UIFactory.Text(top, "Inbox", 24, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(header, flexW: 1);
            var markRead = UIFactory.Btn(top, "Mark all read", UIFactory.Neutral, () => { CarSalesServices.Messages.MarkAllRead(); Refresh(); }, 40f, 16f);
            UIFactory.LE(markRead, h: 40, w: 160, flexW: 0);
            list = UIFactory.ScrollList(Area, 10f);
        }

        public override void Refresh()
        {
            var mm = CarSalesServices.Messages;
            UIFactory.ClearChildren(list);
            if (!mm) return;
            header.text = $"Inbox  <size=70%><color={UIFactory.Hex(UIFactory.TextDim)}>{mm.Messages.Count} messages, {mm.UnreadCount} unread</color></size>";
            foreach (var m in mm.Messages) Row(m);
            if (mm.Messages.Count == 0)
            {
                var t = UIFactory.Text(list, "No messages yet.", 20, UIFactory.TextDim, TextAlignmentOptions.Center);
                UIFactory.LE(t, h: 60);
            }
            // Messages count as read once you've seen the inbox.
            foreach (var m in mm.Messages) m.read = true;
        }

        void Row(GameMessage m)
        {
            Color color; string glyph;
            switch (m.type)
            {
                case MessageType.SellerOffer: color = UIFactory.Good; glyph = "$"; break;
                case MessageType.Bank: color = UIFactory.Warn; glyph = "!"; break;
                case MessageType.Goal: color = UIFactory.Gold; glyph = "*"; break;
                default: color = UIFactory.Accent; glyph = "i"; break;
            }
            var card = UIFactory.Panel(list, "Message", m.read ? UIFactory.CardBg : UIFactory.CardLight, 1.4f);
            var h = UIFactory.Horizontal(card, 14, 14f, TextAnchor.UpperLeft);
            h.childForceExpandHeight = false;
            UIFactory.Icon(card.transform, color, glyph, 44f);

            var col = UIFactory.VCol(card.transform, 4f);
            UIFactory.Text(col, (m.read ? "" : $"<color={UIFactory.Hex(UIFactory.Gold)}>NEW  </color>") + m.title, 20, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.Text(col, $"Day {m.day}", 13, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.Text(col, m.body, 17, UIFactory.TextDim, TextAlignmentOptions.Left);

            if (m.type == MessageType.SellerOffer && !m.resolved)
            {
                var buttons = UIFactory.HRow(col, 44, 10, "Actions", false);
                var buy = UIFactory.Btn(buttons, $"Buy for {UIFactory.Money(m.offerPrice)}", UIFactory.Good, () => Accept(m), 42f, 17f);
                UIFactory.LE(buy, h: 42, w: 220, flexW: 0);
                var decline = UIFactory.Btn(buttons, "Decline", UIFactory.Neutral, () => { CarSalesServices.Messages.Decline(m); Refresh(); }, 42f, 17f);
                UIFactory.LE(decline, h: 42, w: 130, flexW: 0);
                var value = UIFactory.Text(buttons, $"Market value ~{UIFactory.Money(CarValueCalculator.MarketValue(m.offerCar))}", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
                UIFactory.LE(value, flexW: 1);
            }
        }

        void Accept(GameMessage m)
        {
            int price = m.offerPrice;
            if (CarSalesServices.Messages.AcceptOffer(m, out string msg))
                CarSalesServices.UI.purchaseConfirm.Show(m.offerCar, price, m.offerCar.parkingSpotIndex, msg);
            else
            {
                CarSalesServices.UI.Error(msg);
                Refresh();
            }
        }
    }
}
