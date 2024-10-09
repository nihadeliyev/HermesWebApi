using HermesWebApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi
{
    public class Db
    {
        public static ResultCode BeginTransaction(ref SqlConnection connection)
        {
            int affRows = 0;
            return ExecuteWithConnection(ref connection, "BEGIN TRAN", ref affRows);
        }
        public static ResultCode CommitTransaction(ref SqlConnection connection)
        {
            int affRows = 0;
            return ExecuteWithConnection(ref connection, "COMMIT", ref affRows);
        }
        public static ResultCode RollbackTransaction(ref SqlConnection connection)
        {
            int affRows = 0;
            return ExecuteWithConnection(ref connection, "ROLLBACK", ref affRows);
        }
        public static ResultCode ExecuteWithConnection(ref SqlConnection connection, string commandText, ref int rowsAffected, params SqlParameter[] parameters)
        {
            rowsAffected = 0;
            try
            {
                //if (SiteSession.UserID.Equals("0"))
                //    return ResultCodes.sessionTimeoutError;
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 0;
                cmd.Connection = connection;
                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                foreach (SqlParameter param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
                rowsAffected = cmd.ExecuteNonQuery();
                return ResultCodes.noError;
            }
            catch (Exception ExcpNo)
            {
                // new UserLogger().LogException(ExcpNo.Message + "----Query : " + commandText.Replace("'", "''"));
                return ResultCodes.dbError;
            }

        }
        public static ResultCode ExecuteWithConnection(ref SqlConnection connection, string commandText, ref int rowsAffected, ref object insertedID, params SqlParameter[] parameters)
        {
            //if (SiteSession.UserID.Equals("0"))
            //    return ResultCodes.sessionTimeoutError;
            rowsAffected = 0;
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 0;
                cmd.Connection = connection;
                cmd.CommandText = commandText.Replace("VALUES", string.Format("OUTPUT INSERTED.{0} VALUES", insertedID));
                cmd.CommandType = CommandType.Text;
                foreach (SqlParameter param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
                insertedID = cmd.ExecuteScalar();
                return ResultCodes.noError;
            }
            catch (Exception ExcpNo)
            {
                //new UserLogger().LogException(ExcpNo.Message + "----Query : " + commandText.Replace("'", "''"));
                return ResultCodes.dbError;
            }

        }
        public static ResultCode GetDbDataWithConnection(ref SqlConnection connection, string commandText, ref DataSet ds, params SqlParameter[] parameters)
        {
            //if (SiteSession.UserID.Equals("0"))
            //    return ResultCodes.sessionTimeoutError;
#pragma warning disable CS0219 // The variable 'result' is assigned but its value is never used
            bool result;
#pragma warning restore CS0219 // The variable 'result' is assigned but its value is never used
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 0;
                cmd.Connection = connection;
                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                foreach (SqlParameter param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds);
                cmd.Parameters.Clear();
                result = true;
                return ResultCodes.noError;
            }
            catch (Exception ExcpNo)
            {
                //new UserLogger().LogException(ExcpNo.Message + "----Query : " + commandText.Replace("'", "''"));
                result = false;
                ResultCodes.dbError.ErrorMessageEn = ExcpNo.Message;
                return ResultCodes.dbError;
            }
        }
        public static ResultCode GetDbDataWithConnectionSessionless(ref SqlConnection connection, string commandText, ref DataSet ds, params SqlParameter[] parameters)
        {
#pragma warning disable CS0219 // The variable 'result' is assigned but its value is never used
            bool result;
#pragma warning restore CS0219 // The variable 'result' is assigned but its value is never used
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 0;
                cmd.Connection = connection;
                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                foreach (SqlParameter param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds);
                cmd.Parameters.Clear();
                result = true;
                return ResultCodes.noError;
            }
            catch (Exception ExcpNo)
            {
                //new UserLogger().LogException(ExcpNo.Message + "----Query : " + commandText.Replace("'", "''"));
                result = false;
                return ResultCodes.dbError;
            }
        }
    }
}

