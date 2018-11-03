using System;
using System.Collections.Generic;
using StarlightEngine.Math;

namespace StarlightGame.GameCore.Field.Galaxy
{
    public static class SystemNames
    {
        private static List<string> m_namePool = new List<string>(new[] {
            // Special names
            "Sol",
            "Alpha Centauri",
            "Wolf 359",

            // Names from "Bulletin of the IAU Working Group on Star Names, No. 1"
            "Alpheratz",
            "Caph",
            "Algenib",
            "Ankaa",
            "Mirach",
            "Titawin",
            "Achernar",
            "Sheratan",
            "Almach",
            "Hamal",
            "Mira",
            "Polaris",
            "Acamar",
            "Menkar",
            "Algol",
            "Mirfak",
            "Ran",
            "Maia",
            "Merope",
            "Alcyone",
            "Pleione",
            "Zaurak",
            "Ain",
            "Aldebaran",
            "Cursa",
            "Rigel",
            "Capella",
            "Bellatrix",
            "Elnath",
            "Nihal",
            "Mintaka",
            "Arneb",
            "Meissa",
            "Alnilam",
            "Phact",
            "Alnitak",
            "Saiph",
            "Wazn",
            "Betelgeuse",
            "Menkalinan",
            "Propus",
            "Furud",
            "Mirzam",
            "Canopus",
            "Car",
            "Alhena",
            "Gem",
            "Sirius",
            "Wezen",
            "Aludra",
            "Gomeisa",
            "Castor",
            "Procyon",
            "Pollux",
            "Avior",
            "Muscida",
            "Copernicus",
            "Acubens",
            "Talitha",
            "Miaplacidus",
            "Aspidiske",
            "Alphard",
            "Intercrus",
            "Regulus",
            "Adhafera",
            "Tania Borealis",
            "Algieba",
            "Tania Australis",
            "Chalawan",
            "Merak",
            "Dubhe",
            "Zosma",
            "Chertan",
            "Alula Australis",
            "Alula Borealis",
            "Denebola",
            "Phecda",
            "Tonatiuh",
            "Megrez",
            "Acrux",
            "Algorab",
            "Gacrux",
            "Chara",
            "Porrima",
            "Mimosa",
            "Alioth",
            "Cor Caroli",
            "Lich",
            "Vindemiatrix",
            "Mizar",
            "Spica",
            "Alcor",
            "Alkaid",
            "Thuban",
            "Arcturus",
            "Kochab",
            "Edasich",
            "Alphecca",
            "Antares",
            "Ogma",
            "Atria",
            "Rasalgethi",
            "Shaula",
            "Rasalhague",
            "Cervantes",
            "Kaus Media",
            "Kaus Australis",
            "Fafnir",
            "Kaus Borealis",
            "Vega",
            "Rukbat",
            "Albireo",
            "Altair",
            "Libertas",
            "Peacock",
            "Deneb",
            "Musica",
            "Alderamin",
            "Enif",
            "Alnair",
            "Helvetios",
            "Fomalhaut",
            "Scheat",
            "Markab",
            "Veritate",
        });

        // static constructor
        static SystemNames()
        {
            // randomize list of names
            Random rng = RNG.GetRNG();
            m_namePool.Sort((x, y) => (int)Math.Round(rng.NextDouble()));
        }

        /// <summary>
        /// Returns a random name for a system, and removes it from the list of available names
        /// All names from this method will be unique
        /// </summary>
        public static string GetSystemName()
        {
            string name = m_namePool[0];
            m_namePool.RemoveAt(0);
            return name;
        }
    }
}