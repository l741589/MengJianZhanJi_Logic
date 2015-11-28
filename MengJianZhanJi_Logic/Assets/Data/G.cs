using Assets.GameLogic;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Data {
    public static class G {
        private static Dictionary<int, CardInfo> cards;
            
        public static Dictionary<int, CardInfo> Cards {
            get {
                if (cards != null) return cards;
                CardInfo[] cs = IOUtils.ReadCsv<CardInfo>("CardInfo.csv");
                cards = new Dictionary<int, CardInfo>();
                foreach (var c in cs) { Cards.Add(c.Id, c); }
                return cards;
            }
        }
    }
}
