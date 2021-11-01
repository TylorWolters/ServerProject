using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
namespace ConsoleDB
{
    class Program
    {
        public static int Login(string GivenUsername, string GivenPassword, string ConnectionString)
        {
            int Token = -1; //-1 default signifies a failed sign in attempt
            
            ////If username and password provided are found in our table of verified users, give corresponding token that allows access to other commands.
            string QueryString = $@"SELECT UserId 
                                    FROM Users
                                    WHERE Username = '{GivenUsername}' 
                                    AND Password = '{GivenPassword}'
                                    ";
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                SqlCommand Command = new SqlCommand(QueryString, Connection);
                Connection.Open();
                using (SqlDataReader Reader = Command.ExecuteReader())
                {
                    if (Reader.Read())//will be false if no data match exists.
                    {
                            Token = (int)Reader[0]; //reader[0] holds the corresponding token.
                    }
                }
                Connection.Close();
            }
            ////
            
            ////If we found a token, use that token to update the last login time for that corresponding user.
            if (Token != -1)
            {
                string QueryStringUpdate = $@"UPDATE Users
                                              SET Last_login_timestamp = GETDATE()
                                              WHERE UserId = '{Token}'
                                              ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                    Connection.Open();
                    Command.ExecuteReader();
                    Connection.Close();
                }
            }
            ////
            return Token;
        }     
        public static int Logout(int Token)
        {
            Token = -1; //"expires" the token,  would make more sense with real web functionality, or a logout timestamp.
            return Token;                                   
        }
        public static void NewNote(int NoteId, string NoteText, string Project = null, string[] Attribute = null)
        {
            string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ServerProjectDB.mdf"";Integrated Security = True; Connect Timeout = 30";
            string QueryString = null;
            //Main logic split comes from wheter or not Attributes are provided, as project is simply another value in the table but attributes require multiple tables to be accessed to be added properly.
            if (Attribute == null)
            {
                if (Project == null)
                {
                    QueryString = $@"INSERT INTO Notes (NoteId, Creation_timestamp, Note_text)
                                     VALUES ('{NoteId}',GETDATE(),'{NoteText}')
                                     ";
                }
                else
                {
                    QueryString = $@"INSERT INTO Notes (NoteId, Creation_timestamp, Note_text, Project)
                                     VALUES ('{NoteId}',GETDATE(),'{NoteText}','{Project}')
                                     BEGIN
                                     IF NOT EXISTS (SELECT * FROM Projects 
                                                    WHERE Name = '{Project}')
                                     INSERT INTO Projects (Name)
                                     VALUES ('{Project}')
                                     END
                                     ";
                }
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryString, Connection);
                    Connection.Open();
                    Command.ExecuteReader();
                    Connection.Close();
                }
            }
            else
            {
                if (Project == null)
                {
                    QueryString = $@"INSERT INTO dbo.Notes (NoteId, Creation_timestamp, Note_text)
                                     VALUES ('{NoteId}',GETDATE(),'{NoteText}')
                                     ";
                }
                else
                {
                    QueryString = $@"INSERT INTO Notes (NoteId, Creation_timestamp, Note_text, Project)
                                     VALUES ('{NoteId}',GETDATE(),'{NoteText}','{Project}')
                                     BEGIN
                                     IF NOT EXISTS (SELECT * FROM Projects 
                                       WHERE Name = '{Project}')
                                     INSERT INTO Projects (Name)
                                       VALUES ('{Project}')
                                     END
                                     ";
                }
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryString, Connection);
                    Connection.Open();
                    Command.ExecuteReader();
                    Connection.Close();
                }
                foreach (string Name in Attribute)
                {
                    QueryString = $@"INSERT INTO dbo.Attributes (Name)
                                  VALUES ('{Name}')
                                  DECLARE @AttributeIdGenerated AS INT
                                  SELECT @AttributeIdGenerated = AttributeId
                                  FROM Attributes 
                                  WHERE Name = '{Name}'
                                  INSERT INTO dbo.NoteAttributes (NoteId, AttributeId)
                                  VALUES ({NoteId},@AttributeIdGenerated)
                                  ";


                    using (SqlConnection Connection = new SqlConnection(ConnectionString))
                    {
                        SqlCommand Command = new SqlCommand(QueryString, Connection);
                        Connection.Open();
                        Command.ExecuteReader();
                        Connection.Close();
                    }
                    }
            }
        }
        public static void UpdateNote(int token, int NoteId, string NoteText)
        {
            string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ServerProjectDB.mdf"";Integrated Security = True; Connect Timeout = 30";
            if (token != -1)
            {
                string QueryStringUpdate = $@"UPDATE dbo.Notes
                                              SET Note_text = '{NoteText}'
                                              WHERE NoteId = '{NoteId}'
                                              ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                    Connection.Open();
                    Command.ExecuteReader();
                    Connection.Close();
                }
            }
        }
        public static void DeleteNote(int token, int NoteId) {
            string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ServerProjectDB.mdf"";Integrated Security = True; Connect Timeout = 30";
            if (token != -1)
            {
                string QueryStringUpdate = $@"DELETE FROM NoteAttributes
                                                     FROM Notes 
                                              WHERE Notes.NoteId = {NoteId}
                                              OR NoteAttributes.NoteId = {NoteId}
                                              ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                    Connection.Open();
                    Command.ExecuteReader();
                    Connection.Close();
                }
                QueryStringUpdate = $@"DELETE FROM Attributes 
                                       WHERE Attributes.AttributeId NOT IN (SELECT AttributeId FROM NoteAttributes)
                                              ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                    Connection.Open();
                    Command.ExecuteReader();
                    Connection.Close();
                }
            }
        }
        public static List<string> GetNotes(int token, int ProjectId = -1, int[] AttributeId = null) {
            List<string> NoteList = new List<string>();
            string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ServerProjectDB.mdf"";Integrated Security = True; Connect Timeout = 30";

            //Specifying a project will return notes for that project only.
            if (ProjectId != -1) 
            {
                string QueryString = $@"DECLARE @ProjectNameFromId AS NVARCHAR(MAX)
                                  SELECT @ProjectNameFromId = Name
                                  FROM Projects
                                  WHERE ProjectId = '{ProjectId}'
                                  SELECT Note_text
                                  FROM Notes
                                  WHERE Project = @ProjectNameFromId
                                  ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryString, Connection);
                    Connection.Open();
                    using (SqlDataReader reader = Command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NoteList.Add(reader.GetString(0));
                        }
                    }
                    Connection.Close();
                }
            }

            //Not specifying a project will return notes for all projects.
            if (ProjectId == -1)
            {
                string QueryString = $@"SELECT Note_text
                                        FROM Notes
                                        WHERE Project IS NOT NULL
                                        ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryString, Connection);
                    Connection.Open();
                    using (SqlDataReader reader = Command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NoteList.Add(reader.GetString(0));
                        }
                    }
                    Connection.Close();
                }
            }

            //Specifying attributeIds will return only notes that have those attributes.  
            if (AttributeId != null)
            {
                foreach (int AttId in AttributeId)
                {
                    string QueryString = $@"DECLARE @NoteIdFromAttributeId AS INT
                                  SELECT @NoteIdFromAttributeId = NoteID
                                  FROM NoteAttributes
                                  WHERE AttributeId = '{AttId}'
                                  SELECT Note_text
                                  FROM Notes
                                  WHERE NoteId = @ProjectNameFromId
                                  ";
                    using (SqlConnection Connection = new SqlConnection(ConnectionString))
                    {
                        SqlCommand Command = new SqlCommand(QueryString, Connection);
                        Connection.Open();
                        using (SqlDataReader reader = Command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                NoteList.Add(reader.GetString(0));
                            }
                        }
                        Connection.Close();
                    }
                }
            }

            //Not specifying attributeIds will return all notes regardless of attributes.
            if (AttributeId == null)
            {
                string QueryString = $@"SELECT Note_text
                                        FROM Notes
                                        ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryString, Connection);
                    Connection.Open();
                    using (SqlDataReader reader = Command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NoteList.Add(reader.GetString(0));
                        }
                    }
                    Connection.Close();
                }
            }
            return NoteList;
        }
        public static Dictionary<string, int> GetProjectNoteCounts(int token)
        {
            string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ServerProjectDB.mdf"";Integrated Security = True; Connect Timeout = 30";
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            int NullCount = 0;
            if (token != -1)
            {
                string QueryStringUpdate = $@"SELECT COUNT(*), 'null_tally' AS Project
                                              FROM Notes
                                              WHERE Project IS NULL
                                             ";

                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                    Connection.Open();
                    using (SqlDataReader reader = Command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NullCount += 1;
                        }
                    }
                    Connection.Close();
                }
                dictionary.Add("null", NullCount);
                QueryStringUpdate = $@"SELECT Project, count(Project) 
                                       FROM Notes
                                       WHERE Project IS NOT NULL
                                       GROUP by Project
                                       ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                {
                    SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                    Connection.Open();
                    using (SqlDataReader reader = Command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                dictionary.Add(reader.GetString(0), reader.GetInt32(1));
                            }
                        }
                    }
                    Connection.Close();
                }   
            }
            return dictionary;
        }
        public static Dictionary<string,int> GetAttributeNoteCounts(int token)
            {
                string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ServerProjectDB.mdf"";Integrated Security = True; Connect Timeout = 30";
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                int NullCount = 0;
                if (token != -1)
            {
                string QueryStringUpdate = $@"SELECT COUNT(*), 'null_tally' AS NoteId
                                                  FROM Notes
                                                  WHERE NoteId NOT IN
                                                  (Select NoteId
                                                  FROM NoteAttributes)
                                                  ";

                    using (SqlConnection Connection = new SqlConnection(ConnectionString))
                    {
                        SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                        Connection.Open();
                        using (SqlDataReader reader = Command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                NullCount += 1;
                            }
                        }
                        Connection.Close();
                    }
                    dictionary.Add("null", NullCount);

                QueryStringUpdate = $@"SELECT AttributeId, count(AttributeId) 
                                       FROM NoteAttributes
                                       GROUP by NoteId
                                       ";
                using (SqlConnection Connection = new SqlConnection(ConnectionString))
                    {
                        SqlCommand Command = new SqlCommand(QueryStringUpdate, Connection);
                        Connection.Open();
                        using (SqlDataReader reader = Command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    dictionary.Add(reader.GetString(0), reader.GetInt32(1));
                                }
                            }
                        }
                        Connection.Close();
                    }
                }
                return dictionary;
            }
            static void Main(string[] args)
        {
            DeleteNote(100, 10);
        }
    }
}
