using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using SDCards.Models;
using SDCards.Services;
using MongoDB.Driver;

namespace SDCards.Controllers
{
    public class SwipeHub : Hub
    {
        private readonly MongoDbService _mongo;

        public SwipeHub(MongoDbService mongo)
        {
            _mongo = mongo;
        }

        public async Task JoinRoom(string roomId, string username)
        {
            var room = await _mongo.DecisionRooms.Find(r => r.Id == roomId).FirstOrDefaultAsync();
            if (room == null) return;

            var user = new User
            {
                Username = username,
                ConnectionId = Context.ConnectionId
            };

            var isNewUser = false;

            // Check if this is the creator joining
            if (username == room.CreatorUsername)
            {
                room.CreatorConnectionId = Context.ConnectionId;
            }

            // Avoid duplicate usernames
            if (!room.Participants.Any(p => p.Username == username))
            {
                room.Participants.Add(user);
                isNewUser = true; // Only true if it was really new
            }

            await _mongo.DecisionRooms.ReplaceOneAsync(r => r.Id == roomId, room);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            await Clients.Caller.SendAsync("RoomInfo", new {
                CreatorUsername = room.CreatorUsername,
                CreatorConnectionId = room.CreatorConnectionId,
                Participants = room.Participants.Select(p => new { p.Username })
            });

            // ONLY broadcast if it's a **true** new user
            if (isNewUser)
            {
                await Clients.Group(roomId).SendAsync("UserJoined", username);
            }
        }
        
        public async Task SendCardSwipe(string roomId, string cardId, bool isRightSwipe)
        {
            // 1) Log the raw inputs
            Console.WriteLine($"[SwipeHub] 🔔 Called with roomId='{roomId}', cardId='{cardId}', right={isRightSwipe}");

            // 2) Try an explicit ObjectId filter
            var filter = Builders<DecisionRoom>.Filter.Eq("_id", new ObjectId(roomId));
            var swipe = new Swipe
            {
                Id           = ObjectId.GenerateNewId().ToString(),
                CardId       = cardId, 
                IsRightSwipe = isRightSwipe
            };
            var update = Builders<DecisionRoom>.Update.Push(r => r.Swipes, swipe);

            // 3) Capture and log the update result
            var result = await _mongo.DecisionRooms.UpdateOneAsync(filter, update);
            Console.WriteLine($"[SwipeHub] ✅ Matched={result.MatchedCount}, Modified={result.ModifiedCount}");
        }
        
        public async Task AddAnswer(string roomId, string answerText)
        {
            // assign a real ObjectId for this new card
            var card = new Card
            {
                Id       = ObjectId.GenerateNewId().ToString(),
                Title    = answerText,
                ImageUrl = ""
            };

            var update = Builders<DecisionRoom>
                .Update.Push(r => r.Cards, card);

            await _mongo.DecisionRooms.UpdateOneAsync(r => r.Id == roomId, update);

            // let everyone know a new card was added, including its real Id
            await Clients.Group(roomId)
                .SendAsync("AnswerAdded", new {
                    card.Id,
                    card.Title,
                    card.ImageUrl
                });
        }

        public async Task StartActivity(string roomId)
        {
            var room = await _mongo.DecisionRooms.Find(r => r.Id == roomId).FirstOrDefaultAsync();
            if (room == null) return;

            if (room.CreatorConnectionId != Context.ConnectionId)
                return;

            room.IsActive = true;
            room.Cards = room.Cards.Where(c => !string.IsNullOrWhiteSpace(c.Title)).ToList(); // ✅ Remove blanks

            await _mongo.DecisionRooms.ReplaceOneAsync(r => r.Id == roomId, room);

            await Clients.Group(roomId).SendAsync("ActivityStarted", roomId);
        }
        
        public async Task FinishSwiping(string roomId)
        {
            var room = await _mongo.DecisionRooms.Find(r => r.Id == roomId).FirstOrDefaultAsync();
            if (room == null) return;

            room.FinishedParticipants.Add(Context.ConnectionId); // Track that this user finished
            await _mongo.DecisionRooms.ReplaceOneAsync(r => r.Id == roomId, room);

            // If everyone finished
            if (room.Participants.Count == room.FinishedParticipants.Count)
            {
                await Clients.Group(roomId).SendAsync("ResultsReady", roomId);
            }
        }

    }
}
