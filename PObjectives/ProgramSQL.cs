using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PObjectives
{
    class ProgramSQL
    {
        public static void Main7(string[] args)
        {
            Console.WriteLine("ProgramSQL starts.");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            string connectionstring = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=C:\Users\Александр\Documents\test20140823.mdf;Integrated Security=True;Connect Timeout=30";
            ProgramSQL pSQL = new ProgramSQL(connectionstring);

            bool toload = true;
            if (toload)
            {
                sw.Restart();
                pSQL.PrepareToLoad();
                string database_path = @"C:\home\FactographDatabases\PolarDemo\perpho.xml";
                pSQL.LoadTables(XElement.Load(database_path), "person");
                pSQL.LoadTables(XElement.Load(database_path), "photo-doc");
                pSQL.LoadTables(XElement.Load(database_path), "reflection");
                sw.Stop();
                // Загрузка 17 сек.
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds);
            }

            sw.Restart();
            pSQL.Count("person");
            sw.Stop();
            Console.WriteLine("Count ok. Duration={0}", sw.ElapsedMilliseconds);

            sw.Restart();
            pSQL.SelectById(2870, "person");
            //pSQL.SelectById(1309, "person");
            sw.Stop();
            Console.WriteLine("SelectById ok. Duration={0}", sw.ElapsedMilliseconds); // 226, 2, 

            sw.Restart();
            pSQL.SearchByName("Марчук", "person");
            sw.Stop();
            Console.WriteLine("SearchByName ok. Duration={0}", sw.ElapsedMilliseconds); // 122, 49

            sw.Restart();
            pSQL.GetRelationByPerson(2870);
            //pSQL.GetRelation(1309, "reflected");
            sw.Stop();
            Console.WriteLine("GetRelation ok. Duration={0}", sw.ElapsedMilliseconds); // 310, 18, ..., 31, 9, 1 на моем портрете и если повторно

            XElement tracer = XElement.Load(@"C:\home\FactographDatabases\PolarDemo\tracer.xml");
            sw.Restart(); int cnt = 0, sum = 0;
            foreach (XElement portrait in tracer.Elements("portrait").Where(po => po.Attribute("type").Value == "person"))
            {
                int id = Int32.Parse(portrait.Attribute("id").Value);
                sum += pSQL.GetRelationByPerson(id);
                cnt++;
            }
            sw.Stop();
            Console.WriteLine("Tracer test ok. Duration={0} cnt={1} sum={2}", sw.ElapsedMilliseconds, cnt, sum); // 1720 мс. - 993, 2779

        }
        DbConnection connection = null;
        public ProgramSQL(string connectionstring)
        {
            string dataprovider = "System.Data.SqlClient";
            DbProviderFactory factory = DbProviderFactories.GetFactory(dataprovider);
            connection = factory.CreateConnection();
            connection.ConnectionString = connectionstring;
        }
        public void PrepareToLoad() 
        {
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "DROP TABLE person; DROP TABLE photo_doc; DROP TABLE reflection;";
            string message = null;
            try { comm.ExecuteNonQuery(); } catch (Exception ex) { message = ex.Message; }
            comm.CommandText =
@"CREATE TABLE person (id INT NOT NULL, name NVARCHAR(400), from_date NVARCHAR(100), description NVARCHAR(4000), to_date NVARCHAR(100), sex NVARCHAR(6), PRIMARY KEY(id));
CREATE TABLE photo_doc (id INT NOT NULL, name NVARCHAR(400), from_date NVARCHAR(100), description NVARCHAR(4000), PRIMARY KEY(id));
CREATE TABLE reflection (id INT NOT NULL, ground NVARCHAR(20), reflected INT NOT NULL, in_doc INT NOT NULL);";
            try { comm.ExecuteNonQuery(); }
            catch (Exception ex) { message = ex.Message; }
            connection.Close();
            if (message != null) Console.WriteLine(message);

            connection.Open();
            comm = connection.CreateCommand();
            comm.CommandTimeout = 2000;
            comm.CommandText =
@"CREATE INDEX person_name ON person(name);
CREATE INDEX reflection_reflected ON reflection(reflected);
CREATE INDEX reflection_indoc ON reflection(in_doc);";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            connection.Close();
        }

        public void LoadTables(XElement xdb, string table)
        {
            string tab = table.Replace('-', '_');
            DbCommand runcomm = RunStart();
            int portion = 200; int i = 1;
            foreach (XElement element in xdb.Elements(table))
            {
                if (i % 1000 == 0) Console.WriteLine("{0}", i); 
                i++;
                string aaa = null;
                if (table == "person")
                    aaa = "(" + element.Attribute("id").Value + ", " +
                        "N'" + element.Element("name").Value.Replace('\'', '"') + "', " +
                        "'" + element.Element("from-date").Value + "', " +
                        "N'" + element.Element("description").Value + "', " +
                        "'" + element.Element("to-date").Value + "', " +
                        "'" + element.Element("sex").Value + "'" +
                        ")";
                else if (table == "photo-doc")
                    aaa = "(" + element.Attribute("id").Value + ", " +
                        "N'" + element.Element("name").Value.Replace('\'', '"') + "', " +
                        "'" + element.Element("from-date").Value + "', " +
                        "N'" + element.Element("description").Value.Replace('\'', '"') + "'" +
                        ")";
                else if (table == "reflection")
                    aaa = "(" + element.Attribute("id").Value + ", " +
                        "'" + element.Element("ground").Value.Replace('\'', '"') + "', " +
                        "" + element.Element("reflected").Attribute("ref").Value + ", " +
                        "" + element.Element("in-doc").Attribute("ref").Value + "" +
                        ")";
                runcomm.CommandText = "INSERT INTO " + tab + " VALUES " + aaa + ";";
                runcomm.ExecuteNonQuery();
            }
            RunStop(runcomm);
        }

        public void Count(string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandTimeout = 1000;
            comm.CommandText = "SELECT COUNT(*) FROM " + table +";";
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Count()={0}", obj);
            connection.Close();
        }
        public void SelectById(int id, string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT * FROM " + table + " WHERE id=" + id + ";";
            var reader = comm.ExecuteReader();
            while (reader.Read())
            {
                var oname = reader.GetValue(1);
                string name = reader.GetString(1);
                Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            }
            connection.Close();
        }
        public void SearchByName(string searchstring, string table)
        { 
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE N'" + searchstring + "%'";
            var reader = comm.ExecuteReader();
            while (reader.Read())
            {
                var oname = reader.GetValue(1);
                string name = reader.GetString(1);
                Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            }
            connection.Close();
        }
        public int GetRelationByPerson(int id)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT photo_doc.id,reflection.ground,photo_doc.name FROM reflection INNER JOIN photo_doc ON reflection.in_doc=photo_doc.id WHERE reflection.reflected=" + id + ";";
            var reader = comm.ExecuteReader();
            int cnt = 0;
            while (reader.Read())
            {
                //Console.WriteLine("v0={0} v1={1} v2={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
                cnt++;
            }
            connection.Close();
            //Console.WriteLine("cnt={0}", cnt);
            return cnt;
        }


        // Начальная и конечная "скобки" транзакции. В серединке должны использоваться SQL-команды ТОЛЬКО на основе команды runcommand
        private DbCommand RunStart()
        {
            if (connection.State == System.Data.ConnectionState.Open) connection.Close();
            connection.Open();
            DbCommand runcommand = connection.CreateCommand();
            runcommand.CommandType = System.Data.CommandType.Text;
            DbTransaction transaction = connection.BeginTransaction();
            runcommand.Transaction = transaction;
            runcommand.CommandTimeout = 10000;
            return runcommand;
        }
        private void RunStop(DbCommand runcommand)
        {
            runcommand.Transaction.Commit();
            connection.Close();
        }
    }
}
