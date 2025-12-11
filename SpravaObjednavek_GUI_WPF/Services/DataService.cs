using Oracle.ManagedDataAccess.Client;
using SpravaObjednavek_GUI_WPF.Model;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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

        public List<PolozkaAlergen> NacistAlergeny()
        {
            var seznam = new List<PolozkaAlergen>();

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    // SQL dotaz na tvůj view
                    string sql = "SELECT NAZEV_POLOZKY, NAZEV_ALERGENU FROM V_POLOZKY_ALERGENY ORDER BY NAZEV_POLOZKY";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            seznam.Add(new PolozkaAlergen
                            {
                                NazevPolozky = reader["NAZEV_POLOZKY"].ToString(),
                                NazevAlergenu = reader["NAZEV_ALERGENU"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chyba načítání alergenů: " + ex.Message);
                }
            }
            return seznam;
        }

        public int? OveritUzivatele(string jmeno, string heslo)
        {
            int? userId = null;
            string hashHesla = VytvoritMD5(heslo); // Zašifrujeme zadané heslo

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    // 1. Zjistíme, zda existuje uživatel se zadaným jménem a heslem
                    string sql = "SELECT USER_ID FROM LOGIN_CREDS WHERE USER_NAME = :name AND USER_PWD = :pwd";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("name", jmeno));
                        cmd.Parameters.Add(new OracleParameter("pwd", hashHesla));

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            // Uživatel nalezen
                            userId = Convert.ToInt32(result);

                            // 2. Nastavíme příznak logged_in = 1 v tabulce USER
                            NastavitPrihlaseni(userId.Value, 1, conn);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ideálně logovat chybu
                    System.Diagnostics.Debug.WriteLine("Chyba DB: " + ex.Message);
                    throw; // Pošleme chybu dál, ať ji vidíme v okně
                }
            }
            return userId;
        }

        private void NastavitPrihlaseni(int userId, int stav, OracleConnection conn)
        {
            // Pozor: "USER" je klíčové slovo, musí být v uvozovkách
            string updateSql = "UPDATE \"USER\" SET LOGGED_IN = :stav WHERE USER_ID = :id";

            using (OracleCommand cmd = new OracleCommand(updateSql, conn))
            {
                cmd.Parameters.Add(new OracleParameter("stav", stav));
                cmd.Parameters.Add(new OracleParameter("id", userId));
                cmd.ExecuteNonQuery();
            }
        }

        // Metoda pro odhlášení uživatele při ukončení aplikace
        public void OdhlasitUzivatele(int userId)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    // SQL příkaz: Nastaví LOGGED_IN na 0 pro dané ID
                    // Pozor na uvozovky u "USER", protože je to klíčové slovo
                    string sql = "UPDATE \"USER\" SET LOGGED_IN = 0 WHERE USER_ID = :id";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("id", userId));
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // Zde chybu jen vypíšeme do Outputu, protože při vypínání aplikace 
                    // už uživatele nechceme otravovat vyskakovacím oknem
                    System.Diagnostics.Debug.WriteLine("Chyba při odhlašování: " + ex.Message);
                }
            }
        }

        public DetailUzivatele ZiskatDetailUzivatele(int userId)
        {
            var detail = new DetailUzivatele();

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    // Spojíme tabulky, abychom získali Jméno i Typ
                    // Předpokládám názvy tabulek "USER" a LOGIN_CREDS a sloupce USER_TYPE, USER_NAME
                    string sql = @"
                SELECT c.USER_NAME, u.USER_TYPE 
                FROM ""USER"" u 
                JOIN LOGIN_CREDS c ON u.USER_ID = c.USER_ID 
                WHERE u.USER_ID = :id";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("id", userId));

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                detail.Jmeno = reader["USER_NAME"].ToString();
                                detail.Role = reader["USER_TYPE"].ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chyba načítání detailu uživatele: " + ex.Message);
                    detail.Jmeno = "Neznámý";
                    detail.Role = "Chyba";
                }
            }
            return detail;
        }

        private string VytvoritMD5(string vstup)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(vstup);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}