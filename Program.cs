using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            String databaseFilePath = Directory.GetCurrentDirectory() + "\\chatbot.db";

            DatabaseManager dbManager = new DatabaseManager(databaseFilePath);
            Chatbot chatbot = new Chatbot(dbManager);

            DateTime DATA = DateTime.Now;

            string HORARIO = DateTime.Now.ToLongTimeString();

            Console.WriteLine("Olá! Eu sou um ChatBot. Digite 'limpar' para limpar a tela do nosso chat, 'sair' para sair ou se preferir pesquisar por algo escreva 'pesquisar' antes do que deseja.");

            while (true)
            {
                Console.Write("Você: ");
                string userInput = Console.ReadLine();
                string cleanedUserInput = RemoveAccentsAndPunctuation(userInput.ToLower());

                if (Regex.IsMatch(cleanedUserInput, @"\b(pesquisa|pesquisar|procurar|achar)\b"))
                {
                    string searchTerm = cleanedUserInput.Trim();
                    string encodedSearchTerm = Uri.EscapeDataString(searchTerm);
                    Console.WriteLine("ChatBot: Sim, efetuando a pesquisa agora !");
                    string googleSearchUrl = $"https://www.google.com/search?q={encodedSearchTerm}";
                    Process.Start(googleSearchUrl);
                }
                if (Regex.IsMatch(cleanedUserInput, @"\b(limpar|limpar tela|limpar chat|limpar chatbot)\b"))
                {
                    Console.Clear();
                    Console.WriteLine("Olá! Eu sou um ChatBot. Digite 'limpar' para limpar a tela do nosso chat ou 'sair' para sair.");
                }
                if (cleanedUserInput == "sair")
                {
                    break;
                }

                string response = chatbot.GetResponse(cleanedUserInput);

                if (response == null)
                {
                    Console.WriteLine("ChatBot: Desculpe, não entendi a pergunta.");
                }
                else
                {
                    Console.WriteLine("ChatBot: " + response);
                }
                if (response == "Agora é")
                {
                    Console.WriteLine("ChatBot: " + HORARIO);
                }
                if (response == "Hoje é")
                {
                    Console.WriteLine("ChatBot: " + DATA);
                }
                if (response == "Amanha é")
                {
                    DateTime dataAmanha = DATA.AddDays(1);
                    string dataAmanhaString = dataAmanha.ToLongDateString();
                    Console.WriteLine("ChatBot: " + dataAmanhaString);
                }
                if (response == "Ontem foi")
                {
                    DateTime dataOntem = DATA.AddDays(-1);
                    string dataOntemString = dataOntem.ToLongDateString();
                    Console.WriteLine("ChatBot: " + dataOntemString);
                }
                if (response == "Vou te encaminhar para o Google para melhor entendimento do assunto !")
                {
                    string searchTerm = userInput.Trim();
                    string encodedSearchTerm = Uri.EscapeDataString(searchTerm);
                    Console.WriteLine("ChatBot: Sim, efetuando a pesquisa agora !");
                    string googleSearchUrl = $"https://www.google.com/search?q={encodedSearchTerm}";
                    Process.Start(googleSearchUrl);
                }
            }
        }

        static string RemoveAccentsAndPunctuation(string text)
        {
            string normalizedText = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && !char.IsPunctuation(c))
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }
    }

    class Chatbot
    {
        private DatabaseManager dbManager;
        private Dictionary<string, string> knowledgeBase;

        public Chatbot(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.knowledgeBase = new Dictionary<string, string>();

            LoadResponses();
        }

        private void LoadResponses()
        {
            List<ChatbotResponse> responses = dbManager.LoadResponses();
            foreach (var response in responses)
            {
                knowledgeBase[response.Question] = response.Answer;
            }
        }

        public string GetResponse(string question)
        {
            string response = null;

            foreach (var key in knowledgeBase.Keys)
            {
                if (question.Contains(key))
                {
                    response = knowledgeBase[key];
                    break;
                }
            }

            if (response == null)
            {
                Console.WriteLine("ChatBot:: Desculpe, não sei a resposta. Pode me ensinar?");
                Console.Write("Resposta: ");
                response = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(response))
                {
                    ChatbotResponse newResponse = new ChatbotResponse
                    {
                        Question = question,
                        Answer = response
                    };

                    knowledgeBase[question] = response;
                    dbManager.SaveResponse(newResponse);
                    Console.WriteLine("ChatBot: Obrigado por me ensinar!");
                }

            }

            return response;
        }
    }

    public class ChatbotResponse
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class DatabaseManager
    {
        private string databasePath;

        public DatabaseManager(string databasePath)
        {
            this.databasePath = databasePath;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(databasePath))
            {
                SQLiteConnection.CreateFile(databasePath);
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
                {
                    connection.Open();
                    string createTableQuery = "CREATE TABLE IF NOT EXISTS ChatbotResponses (Question TEXT PRIMARY KEY, Answer TEXT)";
                    using (SQLiteCommand command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<ChatbotResponse> LoadResponses()
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                string selectQuery = "SELECT Question, Answer FROM ChatbotResponses";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        List<ChatbotResponse> responses = new List<ChatbotResponse>();
                        while (reader.Read())
                        {
                            responses.Add(new ChatbotResponse
                            {
                                Question = reader["Question"].ToString(),
                                Answer = reader["Answer"].ToString()
                            });
                        }
                        return responses;
                    }
                }
            }
        }

        public void SaveResponse(ChatbotResponse response)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                string insertQuery = "INSERT OR REPLACE INTO ChatbotResponses (Question, Answer) VALUES (@question, @answer)";
                using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@question", response.Question);
                    command.Parameters.AddWithValue("@answer", response.Answer);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
