using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class APILogService : IAPILogService
    {
        private readonly IConfiguration _IConfiguration;

        public APILogService(IConfiguration iConfiguration)
        {
            _IConfiguration = iConfiguration;
        }

        public async Task WriteLogAsync(string API, string UserId, string IP)
        {
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
            using SqlConnection sqlConnection = new SqlConnection(_IConfiguration["WriteConnectionString"]);
            await sqlConnection.OpenAsync();
            var strsql = "insert into APILog(Id,API,UserId,IP,CreatedAt) values(@Id, @API, @UserId, @IP, @CreatedAt)";
            SqlParameter[] sqlParameters = new SqlParameter[5];
            sqlParameters[0] = new SqlParameter("@Id", Guid.NewGuid());
            sqlParameters[1] = new SqlParameter("@API", API);
            sqlParameters[2] = new SqlParameter("@UserId", UserId);
            sqlParameters[3] = new SqlParameter("@IP", IP);
            sqlParameters[4] = new SqlParameter("@CreatedAt", DateTime.Now);
            await ExecuteNonQueryAsync(sqlConnection, CommandType.Text, strsql, sqlParameters);
            //watch.Stop();
            //var timeSpan = watch.Elapsed.TotalMilliseconds;
        }

        private static async Task<int> ExecuteNonQueryAsync(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            SqlCommand sqlCommand = new SqlCommand();
            bool mustCloseConnection = false;
            PrepareCommand(sqlCommand, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);
            int result = await sqlCommand.ExecuteNonQueryAsync();
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                await connection.CloseAsync();
            }

            return result;
        }

        private static int ExecuteNonQuery(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            SqlCommand sqlCommand = new SqlCommand();
            bool mustCloseConnection = false;
            PrepareCommand(sqlCommand, connection, null, commandType, commandText, commandParameters, out mustCloseConnection);
            int result = sqlCommand.ExecuteNonQuery();
            sqlCommand.Parameters.Clear();
            if (mustCloseConnection)
            {
                connection.Close();
            }

            return result;
        }

        private static void PrepareCommand(SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, out bool mustCloseConnection)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (commandText == null || commandText.Length == 0)
            {
                throw new ArgumentNullException("commandText");
            }

            if (connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                connection.Open();
            }
            else
            {
                mustCloseConnection = false;
            }

            command.Connection = connection;
            command.CommandText = commandText;
            if (transaction != null)
            {
                if (transaction.Connection == null)
                {
                    throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", "transaction");
                }

                command.Transaction = transaction;
            }

            command.CommandType = commandType;
            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
        }

        private static void AttachParameters(SqlCommand command, SqlParameter[] commandParameters)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (commandParameters == null)
            {
                return;
            }

            foreach (SqlParameter sqlParameter in commandParameters)
            {
                if (sqlParameter != null)
                {
                    if ((sqlParameter.Direction == ParameterDirection.InputOutput || sqlParameter.Direction == ParameterDirection.Input) && sqlParameter.Value == null)
                    {
                        sqlParameter.Value = DBNull.Value;
                    }

                    command.Parameters.Add(sqlParameter);
                }
            }
        }

    }
}
