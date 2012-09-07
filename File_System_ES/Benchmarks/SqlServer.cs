using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class SqlServer : Benchmark, IDisposable
    {
        SqlConnection con;
        public SqlServer()
        { 
            con = new SqlConnection(@"Data Source=(localdb)\Projects;Initial Catalog=Test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False");
            con.Open();

            using(var cmd = con.CreateCommand())
            {
                cmd.CommandText = "TRUNCATE TABLE Test.dbo.Table_Insert";
                cmd.ExecuteNonQuery();
            }
        }
        public override void Run(int count, int? batch)
        {
            batch = batch.GetValueOrDefault(1);

            for (int i = 0; i < count; i++)
            {
                var trans = con.BeginTransaction();
                for (int j = 0; j < batch; j+= batch.Value)
                {
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = @"INSERT INTO dbo.TABLE_Insert VALUES(@id, @value)";
                        var par = cmd.CreateParameter();
                        par.Value = i;
                        par.ParameterName = "id";
                        cmd.Parameters.Add(par);

                        par = cmd.CreateParameter();
                        par.Value = "value " + i + " / "+j;
                        par.ParameterName = "value";
                        cmd.Parameters.Add(par);

                        cmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
