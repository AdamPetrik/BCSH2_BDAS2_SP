using Oracle.ManagedDataAccess.Client;
using SpravaObjednavek_GUI_WPF.Model;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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

                // ZMĚNA SQL: přidání volání funkce FN_GET_ITEM_LABEL
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
                    // SQL dotaz na view
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
                    System.Diagnostics.Debug.WriteLine("Chyba DB: " + ex.Message);
                    throw; // Pošleme chybu dál, ať ji vidíme v okně
                }
            }
            return userId;
        }

        private void NastavitPrihlaseni(int userId, int stav, OracleConnection conn)
        {
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
                        string sqlOrder = @"
                    INSERT INTO ""ORDER"" (order_id, user_id, created_at, type, price, method) 
                    VALUES (ORDER_ID_SEQ.NEXTVAL, :p_userId, SYSDATE, 'REGULAR', :p_price, :p_method)
                    RETURNING order_id INTO :p_newId";

                        int newOrderId;

                        using (OracleCommand cmd = new OracleCommand(sqlOrder, conn))
                        {
                            cmd.Transaction = transaction;

                            cmd.BindByName = true;

                            // bezpečné názvy parametrů, zabránění kolize s systémovými slovy
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
                    // Stahujeme jen data
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

        public List<PolozkaDetailu> NacistDetailObjednavky(int orderId)
        {
            var list = new List<PolozkaDetailu>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("GET_ORDER_DETAIL", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_order_id", OracleDbType.Int32).Value = orderId;
                    cmd.Parameters.Add("p_results", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PolozkaDetailu
                            {
                                Nazev = reader["NAME"].ToString(),
                                Kusy = Convert.ToInt32(reader["QUANTITY"]),
                                CenaKus = Convert.ToDecimal(reader["PRICE"]),
                                Mezisoucet = Convert.ToDecimal(reader["SUBTOTAL"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<PolozkaMenu> VyhledatVMenu(string text, decimal? maxCena)
        {
            var list = new List<PolozkaMenu>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("SEARCH_MENU", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_query", OracleDbType.Varchar2).Value = (object)text ?? DBNull.Value;
                    cmd.Parameters.Add("p_max_price", OracleDbType.Int32).Value = (object)maxCena ?? DBNull.Value;
                    cmd.Parameters.Add("p_results", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PolozkaMenu
                            {
                                Id = Convert.ToInt32(reader["ITEM_ID"]),
                                Nazev = reader["NAME"].ToString(),
                                Cena = Convert.ToDecimal(reader["PRICE"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 1. Načtení všech adres (SELECT)
        public List<Adresa> NacistAdresy()
        {
            var list = new List<Adresa>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM ADDRESS ORDER BY CITY, STREET";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Adresa
                            {
                                Id = Convert.ToInt32(reader["ADDRESS_ID"]),
                                Ulice = reader["STREET"].ToString(),
                                CisloPopisne = Convert.ToInt32(reader["BUILDING_NUMBER"]),
                                Kraj = reader["PROVINCE"].ToString(),
                                Mesto = reader["CITY"].ToString(),
                                PSC = Convert.ToInt32(reader["POSTAL_CODE"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. Vytvoření
        public void VytvoritAdresu(Adresa a)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("CREATE_ADDRESS", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_street", OracleDbType.Varchar2).Value = a.Ulice;
                    cmd.Parameters.Add("p_building_number", OracleDbType.Int32).Value = a.CisloPopisne;
                    cmd.Parameters.Add("p_province", OracleDbType.Varchar2).Value = a.Kraj;
                    cmd.Parameters.Add("p_city", OracleDbType.Varchar2).Value = a.Mesto;
                    cmd.Parameters.Add("p_postal_code", OracleDbType.Int32).Value = a.PSC;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 3. Úprava
        public void UpravitAdresu(Adresa a)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("UPDATE_ADDRESS", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = a.Id;
                    cmd.Parameters.Add("p_street", OracleDbType.Varchar2).Value = a.Ulice;
                    cmd.Parameters.Add("p_building_number", OracleDbType.Int32).Value = a.CisloPopisne;
                    cmd.Parameters.Add("p_province", OracleDbType.Varchar2).Value = a.Kraj;
                    cmd.Parameters.Add("p_city", OracleDbType.Varchar2).Value = a.Mesto;
                    cmd.Parameters.Add("p_postal_code", OracleDbType.Int32).Value = a.PSC;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Smazání
        public void SmazatAdresu(int id)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("DELETE_ADDRESS", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 1. Načíst všechny licence
        public List<Licence> NacistLicence()
        {
            var list = new List<Licence>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                // Řadíme od nejnovějších
                string sql = "SELECT * FROM LICENSE ORDER BY VALID_FROM DESC";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Licence
                            {
                                Id = Convert.ToInt32(reader["LICENSE_ID"]),
                                TypId = Convert.ToInt32(reader["LICENSE_TYPE_ID"]),
                                PlatnostOd = Convert.ToDateTime(reader["VALID_FROM"]),
                                PlatnostDo = Convert.ToDateTime(reader["VALID_TILL"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. Vytvořit
        public void VytvoritLicenci(int typId, DateTime od, DateTime doData)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("CREATE_LICENSE", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_type_id", OracleDbType.Int32).Value = typId;
                    cmd.Parameters.Add("p_valid_from", OracleDbType.Date).Value = od;
                    cmd.Parameters.Add("p_valid_till", OracleDbType.Date).Value = doData;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 3. Upravit
        public void UpravitLicenci(int id, int typId, DateTime od, DateTime doData)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("UPDATE_LICENSE", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.Parameters.Add("p_type_id", OracleDbType.Int32).Value = typId;
                    cmd.Parameters.Add("p_valid_from", OracleDbType.Date).Value = od;
                    cmd.Parameters.Add("p_valid_till", OracleDbType.Date).Value = doData;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Smazat
        public void SmazatLicenci(int id)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("DELETE_LICENSE", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 1. Načíst všechny alergeny
        public List<Alergen> NacistVsechnyAlergeny()
        {
            var list = new List<Alergen>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM ALLERGEN ORDER BY ALLERGEN_ID";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Alergen
                            {
                                Id = Convert.ToInt32(reader["ALLERGEN_ID"]),
                                Nazev = reader["NAME"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. Vytvořit
        public void VytvoritAlergen(string nazev)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("CREATE_ALLERGEN", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = nazev;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 3. Upravit
        public void UpravitAlergen(int id, string nazev)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("UPDATE_ALLERGEN", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = nazev;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Smazat
        public void SmazatAlergen(int id)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("DELETE_ALLERGEN", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 1. Načíst seznam (jen metadata, žádný BLOB = rychlé)
        public List<ObrazekMeta> NacistSeznamObrazku()
        {
            var list = new List<ObrazekMeta>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT IMAGE_ID, FILENAME, EXTENSION, UPLOADED_BY, UPLOADED_AT FROM IMAGE ORDER BY UPLOADED_AT DESC";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ObrazekMeta
                            {
                                Id = Convert.ToInt32(reader["IMAGE_ID"]),
                                NazevSouboru = reader["FILENAME"].ToString(),
                                Pripona = reader["EXTENSION"].ToString(),
                                Autor = reader["UPLOADED_BY"].ToString(),
                                NahranoKdy = Convert.ToDateTime(reader["UPLOADED_AT"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. Načíst BLOB pro jeden obrázek (pro náhled)
        public byte[] NacistDataObrazku(int imageId)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT CONTENT FROM IMAGE WHERE IMAGE_ID = :id";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = imageId;
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return (byte[])result;
                    }
                }
            }
            return null;
        }

        // 3. Upravit název
        public void UpravitObrazek(int id, string novyNazev)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("UPDATE_IMAGE_METADATA", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.Parameters.Add("p_filename", OracleDbType.Varchar2).Value = novyNazev;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 4. Smazat
        public void SmazatObrazek(int id)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("DELETE_IMAGE", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<HierarchiePolozka> NacistHierarchii()
        {
            var list = new List<HierarchiePolozka>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("SELECT * FROM V_ORG_STRUKTURA", conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new HierarchiePolozka
                            {
                                Uroven = Convert.ToInt32(reader["UROVEN"]),
                                Jmeno = reader["JMENO"].ToString(),
                                Role = reader["ROLE"].ToString(),
                                StromZobrazeni = reader["STROM"].ToString(), // Tady už jsou mezery z DB
                                Cesta = reader["CESTA"].ToString(),
                                Sef = reader["SEF"] == DBNull.Value ? "-" : reader["SEF"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 1. Načíst všechny uživatele (pro tabulku)
        public List<UzivatelPrehled> NacistVsechnyUzivatele()
        {
            var list = new List<UzivatelPrehled>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                // Spojíme USER a LOGIN_CREDS, abychom měli jména
                string sql = @"
            SELECT u.USER_ID, lc.USER_NAME, u.USER_TYPE 
            FROM ""USER"" u
            JOIN LOGIN_CREDS lc ON u.USER_ID = lc.USER_ID
            ORDER BY u.USER_ID";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new UzivatelPrehled
                            {
                                Id = Convert.ToInt32(reader["USER_ID"]),
                                Jmeno = reader["USER_NAME"].ToString(),
                                Role = reader["USER_TYPE"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 2. Vynutit přihlášení (Emulace)
        public void EmulovatUzivatele(int userId)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("FORCE_LOGIN", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_user_id", OracleDbType.Int32).Value = userId;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<DatabazovyObjekt> NacistSchéma()
        {
            var list = new List<DatabazovyObjekt>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("GET_SCHEMA_OBJECTS", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_results", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DatabazovyObjekt
                            {
                                Typ = reader["TYP"].ToString(),
                                Jmeno = reader["JMENO"].ToString(),
                                // Ošetření NULL hodnoty u sekvencí
                                DatumVytvoreni = reader["DATUM_VYTVORENI"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DATUM_VYTVORENI"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public string ZiskatRoliUzivatele(int userId)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT USER_TYPE FROM \"USER\" WHERE USER_ID = :id";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = userId;
                    object result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : "USER";
                }
            }
        }

        // 1. Odeslání zprávy
        public void OdeslatZpravu(int senderId, int receiverId, string content)
        {
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("SEND_MESSAGE", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_sender_id", senderId);
                    cmd.Parameters.Add("p_receiver_id", receiverId);
                    cmd.Parameters.Add("p_content", content);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // 2. Načtení historie
        public List<Zprava> NacistHistoriiChatu(int myId, int otherId)
        {
            var list = new List<Zprava>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand("GET_CHAT_HISTORY", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_user1_id", myId);
                    cmd.Parameters.Add("p_user2_id", otherId);
                    cmd.Parameters.Add("p_results", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Zprava
                            {
                                MessageId = Convert.ToInt32(reader["MESSAGE_ID"]),
                                SentAt = Convert.ToDateTime(reader["SENT_AT"]),
                                SenderId = Convert.ToInt32(reader["SENDER_ID"]),
                                ReceiverId = Convert.ToInt32(reader["RECEIVER_ID"]),
                                Content = reader["CONTENT"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // 3. Seznam uživatelů pro chat (Všichni kromě mě)
        public List<UzivatelPrehled> NacistUzivateleProChat(int myId)
        {
            var list = new List<UzivatelPrehled>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                // Načteme ID a Jméno všech ostatních
                string sql = "SELECT USER_ID, USER_NAME FROM \"USER\" JOIN LOGIN_CREDS USING(USER_ID) WHERE USER_ID != :myId";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("myId", myId);
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new UzivatelPrehled
                            {
                                Id = Convert.ToInt32(reader["USER_ID"]),
                                Jmeno = reader["USER_NAME"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<LicenceV2> NacistVsechnyLicence()
        {
            var list = new List<LicenceV2>();
            using (OracleConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                // Spojíme tabulku LICENSE a LICENSE_TYPE, abychom viděli název typu
                string sql = @"
            SELECT l.license_id, lt.type, l.valid_till 
            FROM LICENSE l 
            JOIN LICENSE_TYPE lt ON l.license_type_id = lt.license_type_id
            ORDER BY l.valid_till DESC"; // Seřazeno podle platnosti

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new LicenceV2
                            {
                                Id = Convert.ToInt32(reader["license_id"]),
                                TypLicence = reader["type"].ToString(),
                                PlatnostDo = Convert.ToDateTime(reader["valid_till"])
                            });
                        }
                    }
                }
            }
            return list;
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