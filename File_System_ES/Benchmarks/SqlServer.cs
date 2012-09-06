using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class SqlServer : Benchmark
    {
        public override void Run(int count, int? batch)
        {
            using (var con = new System.Data.SqlClient.SqlConnection(@"Data Source=(localdb)\Projects;Initial Catalog=Test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False"))
            {
                con.Open();

                for (int i = 0; i < count; i++)
                {
                    var trans = con.BeginTransaction();
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = @"INSERT INTO dbo.TABLE_Insert VALUES(@id, @value)";
                        var par = cmd.CreateParameter();
                        par.Value = i;
                        par.ParameterName = "id";
                        cmd.Parameters.Add(par);

                        par = cmd.CreateParameter();
                        par.Value = "value " + i;
                        par.ParameterName = "value";
                        cmd.Parameters.Add(par);

                        cmd.ExecuteNonQuery();
                    }
                    trans.Rollback();
                }
            }
        }
    }
}
