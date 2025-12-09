using Oracle.ManagedDataAccess.Client;
using SpravaObjednavek_GUI_WPF.Model;
using System.Collections.Generic;
using System;

namespace SpravaObjednavek_GUI_WPF.Services
{
    public class DataService
    {
        // Metoda pro načtení položek menu
        public List<PolozkaMenu> NacistMenuZDatabaze()
        {
            var polozky = new List<PolozkaMenu>();

            // Získáme connection (předpokládám, že tvůj DatabaseConnection.GetConnection() vrací správný string)
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT ITEM_ID, NAME, PRICE FROM ITEM ORDER BY NAME";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            polozky.Add(new PolozkaMenu
                            {
                                // Pozor na přetypování, Oracle vrací specifické typy
                                Id = Convert.ToInt32(reader["ITEM_ID"]),
                                Nazev = reader["NAME"].ToString(),
                                Cena = Convert.ToDecimal(reader["PRICE"])
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Zde by bylo dobré logovat chybu nebo vyhodit výjimku
                    System.Diagnostics.Debug.WriteLine("Chyba DB: " + ex.Message);
                }
            }

            return polozky;
        }
    }
}