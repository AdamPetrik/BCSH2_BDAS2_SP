using Oracle.ManagedDataAccess.Client;
using SpravaObjednavek_GUI_WPF.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace SpravaObjednavek_GUI_WPF.Services
{
    public class DataService
    {
        // Metoda pro načtení položek menu
        public List<PolozkaMenu> NacistMenuZDatabaze()
        {
            var list = new List<PolozkaMenu>();

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                // ZMĚNA SQL: Přidali jsme volání funkce FN_GET_ITEM_LABEL
                string sql = @"
            SELECT 
                ITEM_ID, 
                NAME, 
                PRICE, 
                FN_GET_ITEM_LABEL(ITEM_ID) AS MARKETING_LABEL 
            FROM ITEM 
            ORDER BY NAME";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PolozkaMenu
                            {
                                Id = Convert.ToInt32(reader["ITEM_ID"]),
                                Nazev = reader["NAME"].ToString(),
                                Cena = Convert.ToDecimal(reader["PRICE"]),

                                // Načtení výsledku funkce do vlastnosti Stitek
                                Stitek = reader["MARKETING_LABEL"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
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

        // Nezapomeňte nahoře v souboru mít: using System.Linq; 

        public void VytvoritObjednavku(int userId, decimal celkovaCena, string typPlatby, IEnumerable<PolozkaKosiku> polozky)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                using (OracleTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. VLOŽENÍ HLAVIČKY - OPRAVA PARAMETRŮ
                        // Přejmenoval jsem :uid na :p_userId, :price na :p_price atd., aby nedocházelo ke kolizi s klíčovými slovy Oracle.
                        string sqlOrder = @"
                    INSERT INTO ""ORDER"" (order_id, user_id, created_at, type, price, method) 
                    VALUES (ORDER_ID_SEQ.NEXTVAL, :p_userId, SYSDATE, 'REGULAR', :p_price, :p_method)
                    RETURNING order_id INTO :p_newId";

                        int newOrderId;

                        using (OracleCommand cmd = new OracleCommand(sqlOrder, conn))
                        {
                            cmd.Transaction = transaction;

                            // !!! DŮLEŽITÉ: Zapneme vázání podle jména, jinak se Oracle ztratí v pořadí parametrů !!!
                            cmd.BindByName = true;

                            // Používáme nové, bezpečné názvy parametrů
                            cmd.Parameters.Add(new OracleParameter("p_userId", userId));
                            cmd.Parameters.Add(new OracleParameter("p_price", celkovaCena));
                            cmd.Parameters.Add(new OracleParameter("p_method", typPlatby));

                            // Výstupní parametr
                            OracleParameter outId = new OracleParameter("p_newId", OracleDbType.Int32);
                            outId.Direction = System.Data.ParameterDirection.Output;
                            cmd.Parameters.Add(outId);

                            cmd.ExecuteNonQuery();

                            // Získání ID
                            string hodnotaId = outId.Value.ToString();
                            newOrderId = int.Parse(hodnotaId);
                        }

                        // 2. VLOŽENÍ POLOŽEK
                        string sqlItem = "INSERT INTO ORDER_ITEM (order_id, item_id, quantity) VALUES (:p_oid, :p_iid, :p_qty)";

                        using (OracleCommand cmdItem = new OracleCommand(sqlItem, conn))
                        {
                            cmdItem.Transaction = transaction;
                            cmdItem.BindByName = true; // I tady pro jistotu

                            foreach (var polozka in polozky)
                            {
                                cmdItem.Parameters.Clear();
                                cmdItem.Parameters.Add(new OracleParameter("p_oid", newOrderId));
                                cmdItem.Parameters.Add(new OracleParameter("p_iid", polozka.Id));
                                cmdItem.Parameters.Add(new OracleParameter("p_qty", polozka.Pocet));

                                cmdItem.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ObjednavkaZaznam> NacistObjednavkyPodleData(int mesic, int rok)
        {
            var seznam = new List<ObjednavkaZaznam>();

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    using (OracleCommand cmd = new OracleCommand("GET_ORDERS_BY_MONTH", conn))
                    {
                        // Řekneme, že voláme proceduru, ne SQL text
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Přidáme parametry
                        cmd.Parameters.Add("p_month", OracleDbType.Int32).Value = mesic;
                        cmd.Parameters.Add("p_year", OracleDbType.Int32).Value = rok;

                        // Výstupní parametr (Kurzor)
                        cmd.Parameters.Add("p_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                seznam.Add(new ObjednavkaZaznam
                                {
                                    Id = Convert.ToInt32(reader["ORDER_ID"]),
                                    Datum = Convert.ToDateTime(reader["CREATED_AT"]),
                                    Cena = Convert.ToDecimal(reader["PRICE"]),
                                    ZpusobPlatby = reader["METHOD"].ToString(),
                                    Obsluha = reader["USER_NAME"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chyba načítání objednávek: " + ex.Message);
                }
            }
            return seznam;
        }

        public List<StatistikaItem> ZiskatStatistiku(int minId, int maxId)
        {
            var seznam = new List<StatistikaItem>();

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand("GET_STATS_BY_RANGE", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.Add("p_min_id", OracleDbType.Int32).Value = minId;
                        cmd.Parameters.Add("p_max_id", OracleDbType.Int32).Value = maxId;
                        cmd.Parameters.Add("p_results", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                seznam.Add(new StatistikaItem
                                {
                                    Nazev = reader["NAME"].ToString(),
                                    Pocet = Convert.ToInt32(reader["POCET"])
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Chyba statistiky: " + ex.Message);
                }
            }
            return seznam;
        }

        public void RegistrovatUzivatele(string jmeno, string heslo, string role, int licenseId, int addressId, string poznamka)
        {
            string hashHesla = VytvoritMD5(heslo);

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("REGISTER_USER", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // Parametry musí sedět s názvy v proceduře
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = jmeno;
                    cmd.Parameters.Add("p_password_hash", OracleDbType.Varchar2).Value = hashHesla;
                    cmd.Parameters.Add("p_role", OracleDbType.Varchar2).Value = role;

                    // Nové parametry
                    cmd.Parameters.Add("p_license_id", OracleDbType.Int32).Value = licenseId;
                    cmd.Parameters.Add("p_address_id", OracleDbType.Int32).Value = addressId;
                    cmd.Parameters.Add("p_note", OracleDbType.Varchar2).Value = poznamka;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int NahratObrazek(string cestaKSouboru, string autor)
        {
            // 1. Načtení souboru z disku do byte[]
            byte[] dataObrazku = File.ReadAllBytes(cestaKSouboru);

            // Získání informací o souboru
            FileInfo fi = new FileInfo(cestaKSouboru);
            string filename = fi.Name;
            string extension = fi.Extension.Replace(".", ""); // např. "jpg"
            string mimetype = "image/" + extension; // Zjednodušeně

            int newImageId = 0;

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("UPLOAD_IMAGE", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_filename", OracleDbType.Varchar2).Value = filename;
                    cmd.Parameters.Add("p_extension", OracleDbType.Varchar2).Value = extension;
                    cmd.Parameters.Add("p_mimetype", OracleDbType.Varchar2).Value = mimetype;

                    // Posílání BLOBu
                    cmd.Parameters.Add("p_content", OracleDbType.Blob).Value = dataObrazku;

                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = autor;

                    // Výstupní parametr
                    OracleParameter outId = new OracleParameter("p_new_id", OracleDbType.Int32);
                    outId.Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(outId);

                    cmd.ExecuteNonQuery();

                    // Získání ID
                    newImageId = Convert.ToInt32(outId.Value.ToString());
                }
            }
            return newImageId;
        }

        public void VytvoritPolozkuMenu(string nazev, decimal cena, int? imageId)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("CREATE_ITEM", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = nazev;
                    cmd.Parameters.Add("p_price", OracleDbType.Int32).Value = cena; // Cena je v DB INTEGER

                    // Ošetření NULL hodnoty pro obrázek
                    if (imageId.HasValue)
                    {
                        cmd.Parameters.Add("p_image_id", OracleDbType.Int32).Value = imageId.Value;
                    }
                    else
                    {
                        cmd.Parameters.Add("p_image_id", OracleDbType.Int32).Value = DBNull.Value;
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SmazatObjednavku(int orderId)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("DELETE_ORDER", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_order_id", OracleDbType.Int32).Value = orderId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpravitPolozku(int id, string nazev, decimal cena)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("UPDATE_ITEM", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = nazev;
                    cmd.Parameters.Add("p_price", OracleDbType.Int32).Value = cena;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SmazatPolozku(int id)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("DELETE_ITEM", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<DenniTrzba> NacistDenniTrzby()
        {
            var list = new List<DenniTrzba>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                // Jednoduchý select z View
                using (OracleCommand cmd = new OracleCommand("SELECT * FROM V_DENNI_TRZBY", conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DenniTrzba
                            {
                                Den = Convert.ToDateTime(reader["DEN"]),
                                PocetObjednavek = Convert.ToInt32(reader["POCET_OBJEDNAVEK"]),
                                CelkovaTrzba = Convert.ToDecimal(reader["CELKOVA_TRZBA"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<VykonObsluhy> NacistVykonObsluhy()
        {
            var list = new List<VykonObsluhy>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("SELECT * FROM V_VYKON_OBSLUHY", conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new VykonObsluhy
                            {
                                Obsluha = reader["OBSLUHA"].ToString(),
                                PocetObjednavek = Convert.ToInt32(reader["POCET_OBJEDNAVEK"]),
                                CelkovaTrzba = Convert.ToDecimal(reader["CELKOVA_TRZBA"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<PolozkaGalerie> NacistGalerii()
        {
            var list = new List<PolozkaGalerie>();

            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    // Stahujeme jen data, žádné vytváření BitmapImage zde!
                    string sql = @"
                SELECT i.NAME, im.CONTENT, im.FILENAME, im.EXTENSION
                FROM ITEM i
                LEFT JOIN IMAGE im ON i.IMAGE_ID = im.IMAGE_ID
                ORDER BY i.NAME";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var polozka = new PolozkaGalerie
                                {
                                    NazevJidla = reader["NAME"].ToString()
                                };

                                if (reader["CONTENT"] != DBNull.Value)
                                {
                                    // Jen uložíme surová data (byte[])
                                    // Zbytek udělá ViewModel na hlavním vlákně
                                    polozka.ObrazekData = (byte[])reader["CONTENT"];
                                    polozka.NazevSouboru = reader["FILENAME"].ToString();
                                    polozka.Pripona = reader["EXTENSION"].ToString();
                                }

                                list.Add(polozka);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Logovat chybu, ale nevyhazovat, ať vidíme aspoň zbytek
                    System.Diagnostics.Debug.WriteLine("Chyba DB: " + ex.Message);
                }
            }
            return list;
        }

        public void ZdrazitLevnaJidla(int procento)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("PLOSNE_ZDRAZENI_LEVNYCH_JIDEL", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_procento", OracleDbType.Int32).Value = procento;
                    cmd.ExecuteNonQuery();
                }
            }
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