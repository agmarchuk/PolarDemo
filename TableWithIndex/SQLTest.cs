using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TableWithIndex
{
    public class SQLTest
    {
        System.Random rnd = new Random();
        DbConnection connection = null;
        public SQLTest(string connectionstring)
        {
            string dataprovider = "System.Data.SqlClient";
            DbProviderFactory factory = DbProviderFactories.GetFactory(dataprovider);
            connection = factory.CreateConnection();
            connection.ConnectionString = connectionstring;
        }
        public void PrepareToLoad()
        {
            connection.Open();
            var comm = connection.CreateCommand();

            try
            {
                DbCommand sqlcommand = connection.CreateCommand();
                sqlcommand.CommandText = "DROP TABLE items";
                sqlcommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem to DROP TABLE: " + ex.Message);
            }
            try
            {
                DbCommand sqlcommand = connection.CreateCommand();
                sqlcommand.CommandText =
                    "CREATE TABLE items (id NVARCHAR(400) NOT NULL, name NVARCHAR(400) NOT NULL, fd NVARCHAR(MAX), PRIMARY KEY(id));";
                sqlcommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err in CREATE TABLE: " + ex.Message);
            }

            connection.Close();
        }


        public void Load(XElement db)
        {
            DbCommand runcomm = RunStart();
            foreach (XElement element in db.Elements())
            {
                var id_att = element.Attribute(ONames.rdfabout);
                var tname = element.Name;
                if (id_att == null) continue;
                //if (!(tname == ONames.tag_person)) continue;
                var name_el = element.Element(ONames.tag_name);
                if (name_el == null) continue;
                var fd_el = element.Element(ONames.tag_fromdate);

                string id = id_att.Value;
                string name = name_el.Value;
                string fd = fd_el == null ? null : fd_el.Value;

                runcomm.CommandText = "INSERT INTO items VALUES ('" + id + "',N'" + name.Replace('\'', '\"') + "'," +
                    (fd == null ? "NULL" : "'" + fd.Replace('\'', '\"') + "'") + ");";
                runcomm.ExecuteNonQuery();
            }
            RunStop(runcomm);
        }

        public void Index1()
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "CREATE INDEX id_index ON items(id);";
            comm.CommandTimeout = 20000; // более 5 часов?
            comm.ExecuteNonQuery();
            connection.Close();
        }
        public void Index2()
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "CREATE INDEX name_index ON items(name);";
            comm.CommandTimeout = 20000; // более 5 часов?
            comm.ExecuteNonQuery();
            connection.Close();
        }
        public void SelectById(string id)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT * FROM items WHERE id='" + id + "';";
            var reader = comm.ExecuteReader();
            while (reader.Read())
            {
                var oname = reader.GetValue(1);
                string name = reader.GetString(1);
                Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            }
            connection.Close();
        }
        public void Count()
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandTimeout = 1000;
            comm.CommandText = "SELECT COUNT(*) FROM items;";
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Count()={0}", obj);
            connection.Close();
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
