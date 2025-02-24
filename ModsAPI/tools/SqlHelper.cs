using Microsoft.Data.SqlClient;
using System.Data;

namespace ModsAPI.tools
{
    public class SqlHelper
    {
        public string _strcon = string.Empty;
        private SqlConnection con = null;

        public SqlHelper(string strcon)
        {
            _strcon = strcon;
        }

        private SqlConnection OpenOrCreateCon()
        {
            con = new SqlConnection(_strcon);
            if (con != null && con.State == ConnectionState.Closed)
            { con.Open(); }
            return con;
        }
        private void ClosedCon()
        {
            if (con != null && con.State == ConnectionState.Open)
            { con.Close(); }

        }
        public int GetByScalar(string sql)//查询
        {
            OpenOrCreateCon();
            SqlCommand cmd = new SqlCommand(sql, con);
            int i = Convert.ToInt32(cmd.ExecuteScalar());
            ClosedCon();
            return i;
        }
        public int GetByNonQuery(string sql)//增删改
        {
            OpenOrCreateCon();
            SqlCommand cmd = new SqlCommand(sql, con);
            int i = Convert.ToInt32(cmd.ExecuteNonQuery());
            ClosedCon();
            return i;
        }
        public int GetByNonQuery(string sql, SqlParameter[] para)
        {
            OpenOrCreateCon();
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddRange(para);
            int i = Convert.ToInt32(cmd.ExecuteNonQuery());
            ClosedCon();
            return i;
        }
        public SqlDataReader GetByReader(string sql)
        {
            OpenOrCreateCon();
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataReader i = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return i;
        }
        public DataSet GetBySet(string sql)
        {
            OpenOrCreateCon();
            SqlDataAdapter sda = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();
            sda.Fill(ds);
            return ds;
        }
        public DataTable GetByTable(string sql)
        {
            OpenOrCreateCon();
            SqlDataAdapter sda = new SqlDataAdapter(sql, con);
            DataTable dt = new DataTable();
            sda.Fill(dt);
            return dt;
        }
        public DataView GetByView(string sql)
        {
            OpenOrCreateCon();
            SqlDataAdapter sda = new SqlDataAdapter(sql, con);
            DataTable dt = new DataTable();
            sda.Fill(dt);
            DataView dv = new DataView();
            dv.Table = dt;
            return dv;
        }
    }
}
