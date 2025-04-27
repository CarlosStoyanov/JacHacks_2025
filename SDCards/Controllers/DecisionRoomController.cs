using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SDCards.Services;
using SDCards.Models;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Managers;
using OpenAI;

namespace SDCards.Controllers
{
    public class DecisionRoomController : Controller
    {
        private readonly MongoDbService _mongo;
        private readonly IConfiguration _configuration;

        public DecisionRoomController(MongoDbService mongo, IConfiguration configuration)
        {
            _mongo = mongo;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult CreateRoom()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(string username, string question, int? timeLimitSeconds,
            int maxAnswersPerPerson)
        {
            HttpContext.Session.SetString("Username", username);

            var creatorUser = new User
            {
                Username = username,
                ConnectionId = "" // Will be updated when SignalR connects
            };

            var room = new DecisionRoom
            {
                RoomCode = GenerateRoomCode(),
                Question = question,
                CreatorUsername = username,
                CreatorConnectionId = "",
                IsActive = true,
                Cards = new List<Card>(),
                Participants = new List<User> { creatorUser },
                RoomCreatedAtUtc = DateTime.UtcNow,
                TimeLimitSeconds = timeLimitSeconds,
                MaxAnswersPerPerson = maxAnswersPerPerson,
            };

            await _mongo.DecisionRooms.InsertOneAsync(room);

            return RedirectToAction("Lobby", new { roomId = room.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Lobby(string roomId)
        {
            var room = await _mongo.DecisionRooms.Find(r => r.Id == roomId).FirstOrDefaultAsync();
            if (room == null)
                return NotFound();

            return View(room);
        }

        [HttpGet]
        public async Task<IActionResult> Swipe(string roomId)
        {
            var room = await _mongo.DecisionRooms
                .Find(r => r.Id == roomId)
                .FirstOrDefaultAsync();
            if (room == null) return NotFound();

            // ——— DEBUG START ———
            System.Diagnostics.Debug.WriteLine($"Swipe view for room {roomId}, {room.Cards.Count} total cards:");
            foreach (var c in room.Cards)
            {
                System.Diagnostics.Debug.WriteLine($"    Id:'{c.Id}'   Title:'{c.Title}'");
            }
            // ——— DEBUG END ———

            return View(room);
        }

        // Example quick method to generate a room code (6 random uppercase letters)
        private string GenerateRoomCode()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpGet]
        public async Task<IActionResult> Results(string roomId)
        {
            var room = await _mongo.DecisionRooms
                .Find(r => r.Id == roomId)
                .FirstOrDefaultAsync();
            if (room == null) return NotFound();

            var cardVotes = new List<CardResult>();

            foreach (var card in room.Cards)
            {
                var swipes = room.Swipes.Where(s => s.CardId == card.Id).ToList();
                int rightSwipes = swipes.Count(s => s.IsRightSwipe);
                int leftSwipes = swipes.Count(s => !s.IsRightSwipe);

                cardVotes.Add(new CardResult
                {
                    CardTitle = card.Title,
                    RightSwipes = rightSwipes,
                    LeftSwipes = leftSwipes
                });
            }

            // Generate AI Summary
            string aiSummary = await GenerateSummary(room.Question, cardVotes);

            ViewBag.AISummary = aiSummary;

            return View(cardVotes);
        }

        private async Task<string> GenerateSummary(string question, List<CardResult> cardVotes)
        {
            var apiKey = Environment.GetEnvironmentVariable("API_KEY");
            var service = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            var answersFormatted = string.Join("\n", cardVotes.Select(cv =>
                $"Answer: {cv.CardTitle}, Right Swipes: {cv.RightSwipes}, Left Swipes: {cv.LeftSwipes}"));

            var prompt = $"Given the following question: '{question}' and these answers with their swipe results:\n{answersFormatted}\n\nPropose a recommendation to the user on what to pick or how to decide, in a helpful but concise paragraph.";

            var completionResult = await service.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are an assistant who helps summarize and recommend choices."),
                    ChatMessage.FromUser(prompt)
                },
                Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo // <== fixed here
            });

            Console.WriteLine($"Completion successful? {completionResult.Successful}");
            Console.WriteLine($"Completion error: {completionResult.Error?.Message}");
            Console.WriteLine($"Completion choices: {completionResult.Choices?.Count}");

            if (completionResult.Successful)
            {
                return completionResult.Choices.First().Message.Content;
            }
            else
            {
                return "Could not generate summary.";
            }
        }

        [HttpGet]
        public async Task<IActionResult> Join(string roomId)
        {
            var room = await _mongo.DecisionRooms.Find(r => r.Id == roomId).FirstOrDefaultAsync();
            if (room == null)
                return NotFound();

            ViewBag.RoomId = roomId; // pass it to the view
            return View(room); // You already have Join.cshtml
        }

        [HttpPost]
        public IActionResult Join(string roomId, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return RedirectToAction("Join", new { roomId = roomId });
            }

            HttpContext.Session.SetString("Username", username);
            return RedirectToAction("Lobby", new { roomId = roomId });
        }


        public class CardResult
        {
            public string CardTitle { get; set; }
            public int RightSwipes { get; set; }
            public int LeftSwipes { get; set; }
        }
    }
}