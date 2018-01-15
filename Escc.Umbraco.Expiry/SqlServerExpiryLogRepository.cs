using System.Collections.Generic;
using System.Linq;
using PetaPoco;
using System.Data.SqlClient;
using System.Configuration;
using System;

namespace Escc.Umbraco.Expiry
{
    public class SqlServerExpiryLogRepository : IExpiryLogRepository
    {
        private readonly Database _db;

        public SqlServerExpiryLogRepository()
        {
            _db = new Database("DefaultConnection");
        }

        /// <summary>
        /// Run SP to input the expiry email details into the database
        /// </summary>
        /// <param name="model">ExpiryLogModel - </param>
        public void SetExpiryLogDetails(ExpiryLogEntry model)
        {
            _db.Execute("EXEC SetExpiryLogDetails @EmailAddress, @DateAdded, @EmailSuccess, @Pages", new { model.EmailAddress, model.DateAdded, model.EmailSuccess, model.Pages });
        }

        /// <summary>
        /// Run query to get all expiry logs
        /// </summary>
        /// <param name="model">ExpiryLogModel - </param>
        public List<ExpiryLogEntry> GetExpiryLogs()
        {
            List<ExpiryLogEntry> expiryEmails = new List<ExpiryLogEntry>();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString()))
            {
                cn.Open();
                var sql = string.Format("SELECT * FROM ExpiryEmails");
                SqlCommand sqlCommand = new SqlCommand(sql, cn);
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    ExpiryLogEntry model = new ExpiryLogEntry(0, null, DateTime.Now, false, null);
                    model.Pages = reader["Pages"].ToString();
                    model.DateAdded = (DateTime)reader["DateAdded"];
                    model.EmailAddress = reader["EmailAddress"].ToString();
                    model.EmailSuccess = (bool)reader["EmailSuccess"];
                    model.Id = (int)reader["ID"];
                    expiryEmails.Add(model);
                }
                cn.Close();
            }

            return expiryEmails;
        }

        /// <summary>
        /// Run query to get successful expiry logs
        /// </summary>
        /// <param name="model">ExpiryLogModel - </param>
        public List<ExpiryLogEntry> GetExpiryLogSuccessDetails()
        {
            List<ExpiryLogEntry> expiryEmails = new List<ExpiryLogEntry>();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString()))
            {
                cn.Open();
                var sql = string.Format("SELECT * FROM ExpiryEmails WHERE [EmailSuccess] = {0}", 1);
                SqlCommand sqlCommand = new SqlCommand(sql, cn);
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    ExpiryLogEntry model = new ExpiryLogEntry(0, null, DateTime.Now, false, null);
                    model.Pages = reader["Pages"].ToString();
                    model.DateAdded = (DateTime)reader["DateAdded"];
                    model.EmailAddress = reader["EmailAddress"].ToString();
                    model.EmailSuccess = (bool)reader["EmailSuccess"];
                    model.Id = (int)reader["ID"];
                    expiryEmails.Add(model);
                }
                cn.Close();
            }

            return expiryEmails;
        }

        /// <summary>
        /// Run query to get failed expiry logs
        /// </summary>
        /// <param name="model">ExpiryLogModel - </param>
        public List<ExpiryLogEntry> GetExpiryLogFailureDetails()
        {
            List<ExpiryLogEntry> expiryEmails = new List<ExpiryLogEntry>();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString()))
            {
                cn.Open();
                var sql = string.Format("SELECT * FROM ExpiryEmails WHERE [EmailSuccess] = {0}", 0);
                SqlCommand sqlCommand = new SqlCommand(sql, cn);
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    ExpiryLogEntry model = new ExpiryLogEntry(0, null, DateTime.Now, false, null);
                    model.Pages = reader["Pages"].ToString();
                    model.DateAdded = (DateTime)reader["DateAdded"];
                    model.EmailAddress = reader["EmailAddress"].ToString();
                    model.EmailSuccess = (bool)reader["EmailSuccess"];
                    model.Id = (int)reader["ID"];
                    expiryEmails.Add(model);
                }
                cn.Close();
            }

            return expiryEmails;
        }

        /// <summary>
        /// Run query to get a single log entry
        /// </summary>
        /// <param name="model">ExpiryLogModel - </param>
        public ExpiryLogEntry GetExpiryLogById(int id)
        {
            ExpiryLogEntry expiryEmail = new ExpiryLogEntry();
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString()))
            {
                cn.Open();
                var sql = string.Format("SELECT * FROM ExpiryEmails WHERE [ID] = {0}", id);
                SqlCommand sqlCommand = new SqlCommand(sql, cn);
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    ExpiryLogEntry model = new ExpiryLogEntry(0, null, DateTime.Now, false, null);
                    model.Pages = reader["Pages"].ToString();
                    model.DateAdded = (DateTime)reader["DateAdded"];
                    model.EmailAddress = reader["EmailAddress"].ToString();
                    model.EmailSuccess = (bool)reader["EmailSuccess"];
                    model.Id = (int)reader["ID"];
                    expiryEmail = model;
                }
                cn.Close();
            }

            return expiryEmail;
        }
    }
}