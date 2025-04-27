using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SDCards.Models
{
    public class DecisionRoom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string RoomCode { get; set; }
        public string Question { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsStarted { get; set; } = false;
        public string CreatorConnectionId { get; set; }
        public string CreatorUsername { get; set; } 
        public DateTime? RoomCreatedAtUtc { get; set; }
        public int? TimeLimitSeconds { get; set; }
        public int? MaxAnswersPerPerson { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();
        public List<User> Participants { get; set; } = new List<User>();        
        public List<String> FinishedParticipants { get; set; } = new List<String>();
        public List<Swipe> Swipes { get; set; } = new List<Swipe>();
    }
}