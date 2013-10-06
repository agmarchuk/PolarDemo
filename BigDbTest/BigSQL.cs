using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigDbTest
{
    public class BigSQL
    {
        System.Random rnd = new Random();
        DbConnection connection = null;
        public BigSQL(string connectionstring)
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
            comm.CommandText = "DROP TABLE Tab1; CREATE TABLE Tab1 (randcol INT NOT NULL);";
            string message = null;
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            connection.Close();
            if (message != null) Console.WriteLine(message);
        }

        public void Index()
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "CREATE INDEX ent_int ON Tab1(randcol);";
            comm.CommandTimeout = 20000; // более 5 часов?
            comm.ExecuteNonQuery();
            connection.Close();
        }
        public void Load(int numb)
        {
            DbCommand runcomm = RunStart();
            int portion = 200;
            //connection.Open();
            for (int i = 0; i < numb / portion; i++)
            {
                if (i % 10000 == 0) Console.WriteLine("{0}%", (double)i * 100.0 / (double)numb * (double)portion);
                string aaa = Enumerable.Range(0, portion).Select(n => "(" + rnd.Next() + ")")
                    .Aggregate((sum, s) => sum + "," + s);
                runcomm.CommandText = "INSERT INTO Tab1 VALUES " + aaa + ";";
                runcomm.ExecuteNonQuery();
            }
            runcomm.CommandText = "INSERT INTO Tab1 VALUES (777777777);"; // Последним, чтобы не натыкаться на него в самом начале
            runcomm.ExecuteNonQuery();
            RunStop(runcomm);
        }
        public void Test2(string condition)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT * FROM Tab1 WHERE "+condition+";";
            var reader = comm.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine("Result={0}", reader.GetValue(0));
            }
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
